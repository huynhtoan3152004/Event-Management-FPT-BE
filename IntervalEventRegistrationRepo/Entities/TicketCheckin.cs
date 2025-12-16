using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("ticket_checkins")]
public class TicketCheckin
{
    [Key]
    [Column("checkin_id")]
    [StringLength(50)]
    public string CheckinId { get; set; } = Guid.NewGuid().ToString();

    [Column("ticket_id")]
    [StringLength(50)]
    public string TicketId { get; set; } = string.Empty;

    [Column("staff_id")]
    [StringLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Column("checkin_time")]
    public DateTime CheckinTime { get; set; } = DateTime.UtcNow;

    [Column("checkout_time")]
    public DateTime? CheckoutTime { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "checked-in"; // checked-in, checked-out

    [Column("notes")]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("TicketId")]
    public virtual Ticket? Ticket { get; set; }

    [ForeignKey("StaffId")]
    public virtual User? Staff { get; set; }
}
