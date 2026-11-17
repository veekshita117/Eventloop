using Eventloop.Core.DTOs.Common;
using Eventloop.Core.DTOs.Registrations;

namespace Eventloop.Core.Interfaces;

public interface IRegistrationService
{
    Task<RegistrationResponseDto> RegisterAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<RegistrationResponseDto> CancelRegistrationAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyRegistrationDto>> GetUserRegistrationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<RegistrationResponseDto>> GetEventRegistrationsAsync(Guid eventId, Guid currentUserId, bool isAdmin, RegistrationFilterParametersDto filters, CancellationToken cancellationToken = default);
    Task<int> PromoteWaitlistedAttendeesAsync(Guid eventId, int spotsAvailable, CancellationToken cancellationToken = default);
}
