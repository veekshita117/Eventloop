using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Eventloop.Core.Entities;
using Eventloop.Core.Interfaces;
using Eventloop.Core.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Eventloop.Infrastructure.Services;

public class AzureQueueRegistrationEventPublisher : IRegistrationEventPublisher
{
    private readonly QueueClient? _queueClient;
    private readonly ILogger<AzureQueueRegistrationEventPublisher> _logger;
    private bool _queueInitialized = false;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public AzureQueueRegistrationEventPublisher(
        IConfiguration configuration,
        ILogger<AzureQueueRegistrationEventPublisher> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("AzureQueueStorage")
            ?? configuration["AzureWebJobsStorage"]
            ?? "UseDevelopmentStorage=true";

        try
        {
            _queueClient = new QueueClient(connectionString, RegistrationEventMessage.QueueName, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure QueueClient with provided connection string: {Message}", ex.Message);
        }
    }

    public async Task PublishTicketIssuedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        var message = new RegistrationEventMessage(
            RegistrationEventTypes.TicketIssued,
            registration.Id,
            registration.UserId,
            eventEntity.Id);

        await SendMessageSafelyAsync(message, cancellationToken);
    }

    public async Task PublishWaitlistPromotedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        var message = new RegistrationEventMessage(
            RegistrationEventTypes.WaitlistPromoted,
            registration.Id,
            registration.UserId,
            eventEntity.Id);

        await SendMessageSafelyAsync(message, cancellationToken);
    }

    public async Task PublishRegistrationCancelledAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default)
    {
        var message = new RegistrationEventMessage(
            RegistrationEventTypes.RegistrationCancelled,
            registration.Id,
            registration.UserId,
            eventEntity.Id);

        await SendMessageSafelyAsync(message, cancellationToken);
    }

    private async Task SendMessageSafelyAsync(RegistrationEventMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (_queueClient == null)
            {
                _logger.LogWarning("QueueClient is not initialized. Skipping enqueue for {EventType}.", message.EventType);
                return;
            }

            if (!_queueInitialized)
            {
                await _initLock.WaitAsync(cancellationToken);
                try
                {
                    if (!_queueInitialized)
                    {
                        await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                        _queueInitialized = true;
                    }
                }
                finally
                {
                    _initLock.Release();
                }
            }

            var json = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(json, cancellationToken);

            _logger.LogInformation(
                "Enqueued {EventType} message for RegistrationId={RegistrationId} to queue '{QueueName}'",
                message.EventType,
                message.RegistrationId,
                RegistrationEventMessage.QueueName);
        }
        catch (Exception ex)
        {
            // Failure to enqueue must not roll back or fail the calling registration operation
            _logger.LogError(
                ex,
                "Non-fatal error: Failed to enqueue {EventType} message for RegistrationId={RegistrationId}. Error: {Message}",
                message.EventType,
                message.RegistrationId,
                ex.Message);
        }
    }
}
