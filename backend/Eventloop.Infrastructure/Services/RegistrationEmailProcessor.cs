using Eventloop.Core.Entities;
using Eventloop.Core.Enums;
using Eventloop.Core.Interfaces;
using Eventloop.Core.Messages;
using Eventloop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Services;

public class RegistrationEmailProcessor : IRegistrationEmailProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegistrationEmailProcessor> _logger;

    public RegistrationEmailProcessor(
        ApplicationDbContext context,
        IEmailSender emailSender,
        ILogger<RegistrationEmailProcessor> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task ProcessEventMessageAsync(RegistrationEventMessage message, CancellationToken cancellationToken = default)
    {
        var registration = await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == message.RegistrationId, cancellationToken);

        if (registration == null)
        {
            _logger.LogWarning("Registration {RegistrationId} not found. Skipping email processing.", message.RegistrationId);
            return;
        }

        switch (message.EventType)
        {
            case RegistrationEventTypes.TicketIssued:
                await HandleTicketIssuedAsync(registration, cancellationToken);
                break;

            case RegistrationEventTypes.WaitlistPromoted:
                await HandleWaitlistPromotedAsync(registration, cancellationToken);
                break;

            case RegistrationEventTypes.RegistrationCancelled:
                await HandleRegistrationCancelledAsync(registration, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown event type '{EventType}' for Registration {RegistrationId}.", message.EventType, message.RegistrationId);
                break;
        }
    }

    public async Task<int> SendDailyRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var next24Hours = now.AddHours(24);

        var upcomingRegistrations = await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Event)
            .Where(r => r.Status == RegistrationStatus.Confirmed
                     && r.ReminderSentAt == null
                     && r.Event.Status == EventStatus.Published
                     && r.Event.StartTime > now
                     && r.Event.StartTime <= next24Hours)
            .ToListAsync(cancellationToken);

        if (upcomingRegistrations.Count == 0)
        {
            return 0;
        }

        foreach (var reg in upcomingRegistrations)
        {
            var subject = $"Event Reminder: '{reg.Event.Title}' is coming up!";
            var html = BuildReminderHtml(reg.Event, reg);

            await _emailSender.SendEmailAsync(reg.User.Email, subject, html, cancellationToken);
            reg.ReminderSentAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Sent {Count} event reminder emails.", upcomingRegistrations.Count);

        return upcomingRegistrations.Count;
    }

    private async Task HandleTicketIssuedAsync(Registration reg, CancellationToken cancellationToken)
    {
        // Idempotency check: don't send confirmation if ticket is no longer confirmed
        if (reg.Status != RegistrationStatus.Confirmed && reg.Status != RegistrationStatus.CheckedIn)
        {
            _logger.LogInformation("Registration {RegistrationId} status is '{Status}', skipping confirmation email.", reg.Id, reg.Status);
            return;
        }

        var subject = $"Your Ticket: {reg.Event.Title}";
        var html = BuildConfirmationHtml(reg.Event, reg, isPromoted: false);

        await _emailSender.SendEmailAsync(reg.User.Email, subject, html, cancellationToken);
    }

    private async Task HandleWaitlistPromotedAsync(Registration reg, CancellationToken cancellationToken)
    {
        // Idempotency check: ensure user is indeed confirmed
        if (reg.Status != RegistrationStatus.Confirmed && reg.Status != RegistrationStatus.CheckedIn)
        {
            _logger.LogInformation("Registration {RegistrationId} status is '{Status}', skipping promotion email.", reg.Id, reg.Status);
            return;
        }

        var subject = $"Good news! A spot opened for '{reg.Event.Title}'";
        var html = BuildConfirmationHtml(reg.Event, reg, isPromoted: true);

        await _emailSender.SendEmailAsync(reg.User.Email, subject, html, cancellationToken);
    }

    private async Task HandleRegistrationCancelledAsync(Registration reg, CancellationToken cancellationToken)
    {
        // Idempotency check: ensure registration was actually cancelled
        if (reg.Status != RegistrationStatus.Cancelled)
        {
            _logger.LogInformation("Registration {RegistrationId} status is '{Status}', skipping cancellation email.", reg.Id, reg.Status);
            return;
        }

        var subject = $"Registration Cancelled: {reg.Event.Title}";
        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: sans-serif; color: #333; line-height: 1.6;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;"">
        <h2 style=""color: #dc2626;"">Registration Cancelled</h2>
        <p>Hi {reg.User.Name},</p>
        <p>Your registration for <strong>{reg.Event.Title}</strong> has been successfully cancelled.</p>
        <p>If this was a mistake, you can visit the Eventloop portal to register again if spots are available.</p>
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;"" />
        <p style=""font-size: 12px; color: #6b7280;"">Eventloop - College Event Registration System</p>
    </div>
</body>
</html>";

        await _emailSender.SendEmailAsync(reg.User.Email, subject, html, cancellationToken);
    }

    private static string BuildConfirmationHtml(Event ev, Registration reg, bool isPromoted)
    {
        var headerText = isPromoted ? "You're off the waitlist!" : "Your Registration is Confirmed!";
        var introText = isPromoted
            ? $"A spot has opened up, and your registration for <strong>{ev.Title}</strong> has been confirmed!"
            : $"Thank you for registering for <strong>{ev.Title}</strong>. We look forward to seeing you!";

        return $@"
<!DOCTYPE html>
<html>
<body style=""font-family: sans-serif; color: #333; line-height: 1.6;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;"">
        <h2 style=""color: #2563eb;"">{headerText}</h2>
        <p>Hi {reg.User.Name},</p>
        <p>{introText}</p>
        
        <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 6px; margin: 20px 0;"">
            <h3 style=""margin-top: 0;"">Event Details</h3>
            <p style=""margin: 5px 0;""><strong>Event:</strong> {ev.Title}</p>
            <p style=""margin: 5px 0;""><strong>Venue:</strong> {ev.Venue}</p>
            <p style=""margin: 5px 0;""><strong>Date & Time:</strong> {ev.StartTime:f} UTC</p>
            <p style=""margin: 5px 0;""><strong>Ticket Code:</strong> <span style=""font-family: monospace; font-size: 16px; background: #e5e7eb; padding: 2px 6px; border-radius: 4px;"">{reg.TicketCode}</span></p>
        </div>

        <p>Present your QR ticket at the door for fast check-in.</p>
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;"" />
        <p style=""font-size: 12px; color: #6b7280;"">Eventloop - College Event Registration System</p>
    </div>
</body>
</html>";
    }

    private static string BuildReminderHtml(Event ev, Registration reg)
    {
        return $@"
<!DOCTYPE html>
<html>
<body style=""font-family: sans-serif; color: #333; line-height: 1.6;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;"">
        <h2 style=""color: #059669;"">Event Reminder</h2>
        <p>Hi {reg.User.Name},</p>
        <p>This is a quick reminder that <strong>{ev.Title}</strong> takes place in less than 24 hours!</p>
        
        <div style=""background-color: #f3f4f6; padding: 15px; border-radius: 6px; margin: 20px 0;"">
            <p style=""margin: 5px 0;""><strong>Venue:</strong> {ev.Venue}</p>
            <p style=""margin: 5px 0;""><strong>Start Time:</strong> {ev.StartTime:f} UTC</p>
            <p style=""margin: 5px 0;""><strong>Ticket Code:</strong> <span style=""font-family: monospace; font-size: 16px; background: #e5e7eb; padding: 2px 6px; border-radius: 4px;"">{reg.TicketCode}</span></p>
        </div>

        <p>Have your QR code ready on your mobile device at the entrance.</p>
        <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;"" />
        <p style=""font-size: 12px; color: #6b7280;"">Eventloop - College Event Registration System</p>
    </div>
</body>
</html>";
    }
}
