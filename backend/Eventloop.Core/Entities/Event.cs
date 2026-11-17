using Eventloop.Core.Enums;

namespace Eventloop.Core.Entities;

public class Event
{
    public Guid Id { get; set; }
    public Guid OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Concurrency token for optimistic concurrency when reserving spots
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
