using Eventloop.Core.Entities;
using Eventloop.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Services;

public class LoggingRegistrationEventPublisher : IRegistrationEventPublisher
{
    private readonly ILogger<LoggingRegistrationEventPublisher> _logger;

    public LoggingRegistrationEventPublisher(ILogger<LoggingRegistrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishTicketIssuedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Ticket issued: TicketCode={TicketCode}, EventId={EventId}, EventTitle='{EventTitle}', UserId={UserId}",
            registration.TicketCode,
            eventEntity.Id,
            eventEntity.Title,
            registration.UserId);

        return Task.CompletedTask;
    }

    public Task PublishWaitlistPromotedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Waitlist promoted to Confirmed: TicketCode={TicketCode}, EventId={EventId}, EventTitle='{EventTitle}', UserId={UserId}",
            registration.TicketCode,
            eventEntity.Id,
            eventEntity.Title,
            registration.UserId);

        return Task.CompletedTask;
    }

    public Task PublishRegistrationCancelledAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Registration cancelled: TicketCode={TicketCode}, EventId={EventId}, EventTitle='{EventTitle}', UserId={UserId}",
            registration.TicketCode,
            eventEntity.Id,
            eventEntity.Title,
            registration.UserId);

        return Task.CompletedTask;
    }
}
