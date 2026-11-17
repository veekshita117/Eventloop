using System.Text.Json;
using Eventloop.Core.Interfaces;
using Eventloop.Core.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Eventloop.Functions;

public class ProcessRegistrationEventFunction
{
    private readonly IRegistrationEmailProcessor _emailProcessor;
    private readonly ILogger<ProcessRegistrationEventFunction> _logger;

    public ProcessRegistrationEventFunction(
        IRegistrationEmailProcessor emailProcessor,
        ILogger<ProcessRegistrationEventFunction> logger)
    {
        _emailProcessor = emailProcessor;
        _logger = logger;
    }

    [Function("ProcessRegistrationEvent")]
    public async Task Run(
        [QueueTrigger(RegistrationEventMessage.QueueName, Connection = "AzureWebJobsStorage")] string messageJson,
        FunctionContext context)
    {
        _logger.LogInformation("Processing registration event queue message: {MessageJson}", messageJson);

        try
        {
            var message = JsonSerializer.Deserialize<RegistrationEventMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (message == null)
            {
                _logger.LogWarning("Deserialized registration message is null. Skipping.");
                return;
            }

            await _emailProcessor.ProcessEventMessageAsync(message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration queue message: {Message}", ex.Message);
            // Re-throw to trigger Azure Queue Storage retry policy and eventual poison queue routing
            throw;
        }
    }
}
