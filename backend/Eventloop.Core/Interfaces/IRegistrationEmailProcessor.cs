using Eventloop.Core.Messages;

namespace Eventloop.Core.Interfaces;

public interface IRegistrationEmailProcessor
{
    Task ProcessEventMessageAsync(RegistrationEventMessage message, CancellationToken cancellationToken = default);
    Task<int> SendDailyRemindersAsync(CancellationToken cancellationToken = default);
}
