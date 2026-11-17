namespace Eventloop.Core.DTOs.Stats;

public record EventAttendanceStatsDto(
    Guid EventId,
    string EventTitle,
    int Capacity,
    int ConfirmedCount,
    int CheckedInCount,
    int WaitlistCount,
    int CancelledCount,
    double NoShowRate,
    IReadOnlyList<CheckInIntervalStatDto> CheckInsPer15MinInterval,
    IReadOnlyList<DailyRegistrationStatDto> RegistrationsPerDay
);
