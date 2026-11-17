namespace Eventloop.Core.Messages;

public class RegistrationEventMessage
{
    public const string QueueName = "registration-events";

    public string EventType { get; set; } = string.Empty;
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public RegistrationEventMessage() { }

    public RegistrationEventMessage(string eventType, Guid registrationId, Guid userId, Guid eventId)
    {
        EventType = eventType;
        RegistrationId = registrationId;
        UserId = userId;
        EventId = eventId;
        TimestampUtc = DateTime.UtcNow;
    }
}

public static class RegistrationEventTypes
{
    public const string TicketIssued = "TicketIssued";
    public const string WaitlistPromoted = "WaitlistPromoted";
    public const string RegistrationCancelled = "RegistrationCancelled";
}
