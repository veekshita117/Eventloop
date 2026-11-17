namespace Eventloop.Core.DTOs.Stats;

public record DailyRegistrationStatDto(
    DateTime Date,
    int Count
);
