using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("tickets")]
public class Ticket
{
    [Key]
    [Column("ticket_id")]
    [StringLength(50)]
    public string TicketId { get; set; } = Guid.NewGuid().ToString();

    [Column("event_id")]
    [StringLength(50)]
    public string EventId { get; set; } = string.Empty;

    [Column("student_id")]
    [StringLength(50)]
    public string StudentId { get; set; } = string.Empty;

    [Column("seat_id")]
    [StringLength(50)]
    public string? SeatId { get; set; }

    [Column("ticket_code")]
    [StringLength(50)]
    public string TicketCode { get; set; } = string.Empty;

    [Column("qr_code")]
    public string? QrCode { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "active"; // active, used, cancelled, expired

    [Column("registered_at")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [Column("check_in_time")]
    public DateTime? CheckInTime { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("cancel_reason")]
    public string? CancelReason { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("EventId")]
    public virtual Event? Event { get; set; }

    [ForeignKey("StudentId")]
    public virtual User? Student { get; set; }

    [ForeignKey("SeatId")]
    public virtual Seat? Seat { get; set; }

    public virtual ICollection<TicketCheckin> TicketCheckins { get; set; } = new List<TicketCheckin>();
}
