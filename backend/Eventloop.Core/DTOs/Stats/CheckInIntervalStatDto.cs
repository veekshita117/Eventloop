namespace Eventloop.Core.DTOs.Stats;

public record CheckInIntervalStatDto(
    DateTime IntervalStart,
    int Count
);
