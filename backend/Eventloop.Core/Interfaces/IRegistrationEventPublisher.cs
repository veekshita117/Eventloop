using Eventloop.Core.Entities;

namespace Eventloop.Core.Interfaces;

public interface IRegistrationEventPublisher
{
    Task PublishTicketIssuedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default);
    Task PublishWaitlistPromotedAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default);
    Task PublishRegistrationCancelledAsync(Registration registration, Event eventEntity, CancellationToken cancellationToken = default);
}
