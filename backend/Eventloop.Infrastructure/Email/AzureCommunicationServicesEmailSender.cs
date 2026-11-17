using Azure;
using Azure.Communication.Email;
using Eventloop.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eventloop.Infrastructure.Email;

public class AzureCommunicationServicesEmailOptions
{
    public const string SectionName = "Email:AzureCommunicationServices";
    public string ConnectionString { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = "donotreply@eventloop.college.edu";
}

public class AzureCommunicationServicesEmailSender : IEmailSender
{
    private readonly AzureCommunicationServicesEmailOptions _options;
    private readonly ILogger<AzureCommunicationServicesEmailSender> _logger;

    public AzureCommunicationServicesEmailSender(
        IOptions<AzureCommunicationServicesEmailOptions> options,
        ILogger<AzureCommunicationServicesEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new InvalidOperationException("Azure Communication Services connection string is not configured.");
            }

            var emailClient = new EmailClient(_options.ConnectionString);

            var emailMessage = new EmailMessage(
                senderAddress: _options.SenderAddress,
                recipientAddress: toEmail,
                content: new EmailContent(subject)
                {
                    Html = htmlBody
                });

            var operation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken);
            _logger.LogInformation("Sent email to {ToEmail} via Azure Communication Services. Status: {Status}", toEmail, operation.Value.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via Azure Communication Services: {Message}", toEmail, ex.Message);
            throw;
        }
    }
}
