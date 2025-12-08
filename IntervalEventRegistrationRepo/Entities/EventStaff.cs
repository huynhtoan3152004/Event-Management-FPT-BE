using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("event_staff")]
public class EventStaff
{
    [Column("event_id")]
    [StringLength(50)]
    public string EventId { get; set; } = string.Empty;

    [Column("staff_id")]
    [StringLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Column("role")]
    [StringLength(50)]
    public string? Role { get; set; } // MC, Hậu cần, Check-in...

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("EventId")]
    public virtual Event? Event { get; set; }

    [ForeignKey("StaffId")]
    public virtual User? Staff { get; set; }
}
