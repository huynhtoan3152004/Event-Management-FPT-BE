using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("events")]
public class Event
{
    [Key]
    [Column("event_id")]
    [StringLength(50)]
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    [Column("title")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Column("location")]
    [StringLength(500)]
    public string? Location { get; set; }

    [Column("hall_id")]
    [StringLength(50)]
    public string? HallId { get; set; }

    [Column("organizer_id")]
    [StringLength(50)]
    public string OrganizerId { get; set; } = string.Empty;

    [Column("club_id")]
    [StringLength(50)]
    public string? ClubId { get; set; }

    [Column("club_name")]
    [StringLength(200)]
    public string? ClubName { get; set; }

    [Column("image_url")]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "draft"; // draft, pending, approved, rejected, published, cancelled, completed

    [Column("total_seats")]
    public int TotalSeats { get; set; }

    [Column("registered_count")]
    public int RegisteredCount { get; set; } = 0;

    [Column("checked_in_count")]
    public int CheckedInCount { get; set; } = 0;

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("max_tickets_per_user")]
    public int MaxTicketsPerUser { get; set; } = 1;

    [Column("registration_start")]
    public DateTime? RegistrationStart { get; set; }

    [Column("registration_end")]
    public DateTime? RegistrationEnd { get; set; }

    [Column("approved_by")]
    [StringLength(50)]
    public string? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("number_of_rows")]
    public int NumberOfRows { get; set; }

    [Column("seats_per_row")]
    public int SeatsPerRow { get; set; }

    // Navigation properties
    [ForeignKey("HallId")]
    public virtual Hall? Hall { get; set; }

    [ForeignKey("OrganizerId")]
    public virtual User? Organizer { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<EventStaff> EventStaffs { get; set; } = new List<EventStaff>();
    public virtual ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
