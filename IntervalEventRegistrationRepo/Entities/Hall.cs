using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("halls")]
public class Hall
{
    [Key]
    [Column("hall_id")]
    [StringLength(50)]
    public string HallId { get; set; } = Guid.NewGuid().ToString();

    [Column("name")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; }

    [Column("facilities")]
    public string? Facilities { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "available"; // available, maintenance, closed

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
