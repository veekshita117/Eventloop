using Eventloop.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Eventloop.Functions;

public class DailyEventReminderFunction
{
    private readonly IRegistrationEmailProcessor _emailProcessor;
    private readonly ILogger<DailyEventReminderFunction> _logger;

    public DailyEventReminderFunction(
        IRegistrationEmailProcessor emailProcessor,
        ILogger<DailyEventReminderFunction> logger)
    {
        _emailProcessor = emailProcessor;
        _logger = logger;
    }

    // Runs once daily at 08:00 UTC ("0 0 8 * * *")
    [Function("DailyEventReminder")]
    public async Task Run(
        [TimerTrigger("0 0 8 * * *")] TimerInfo timerInfo,
        FunctionContext context)
    {
        _logger.LogInformation("Daily event reminder timer function executed at: {Time}", DateTime.UtcNow);

        try
        {
            var sentCount = await _emailProcessor.SendDailyRemindersAsync(context.CancellationToken);
            _logger.LogInformation("Completed daily reminders job. Sent {Count} reminder emails.", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing daily event reminders timer job: {Message}", ex.Message);
            throw;
        }
    }
}
