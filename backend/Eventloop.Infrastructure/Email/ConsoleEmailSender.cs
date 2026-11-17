using Eventloop.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Email;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL] To: {ToEmail} | Subject: '{Subject}'\nContent:\n{HtmlBody}\n",
            toEmail,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}
