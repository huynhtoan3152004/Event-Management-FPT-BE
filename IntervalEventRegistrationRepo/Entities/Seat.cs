using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("seats")]
public class Seat
{
    [Key]
    [Column("seat_id")]
    [StringLength(50)]
    public string SeatId { get; set; } = Guid.NewGuid().ToString();

    [Column("hall_id")]
    [StringLength(50)]
    public string HallId { get; set; } = string.Empty;

    [Column("seat_number")]
    [StringLength(10)]
    public string SeatNumber { get; set; } = string.Empty;

    [Column("row_label")]
    [StringLength(10)]
    public string? RowLabel { get; set; }

    [Column("section")]
    [StringLength(50)]
    public string? Section { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "available"; // available, reserved, occupied, maintenance

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("HallId")]
    public virtual Hall? Hall { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
