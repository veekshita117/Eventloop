using Eventloop.Core.Enums;

namespace Eventloop.Core.Entities;

public class Registration
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;
    public int? WaitlistPosition { get; set; }
    public Guid TicketCode { get; set; } = Guid.NewGuid();
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInBy { get; set; }
    public User? CheckedInByUser { get; set; }
}
