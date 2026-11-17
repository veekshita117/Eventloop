using Eventloop.Core.DTOs.CheckIn;
using Eventloop.Core.DTOs.Stats;

namespace Eventloop.Core.Interfaces;

public interface ICheckInService
{
    Task<TicketDetailsDto> GetTicketDetailsAsync(Guid registrationId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<CheckInResponseDto> CheckInTicketAsync(Guid eventId, Guid ticketCode, Guid scannerUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<EventAttendanceStatsDto> GetAttendanceStatsAsync(Guid eventId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAttendeesCsvAsync(Guid eventId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
