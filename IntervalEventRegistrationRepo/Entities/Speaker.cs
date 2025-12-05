using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("speakers")]
public class Speaker
{
    [Key]
    [Column("speaker_id")]
    [StringLength(50)]
    public string SpeakerId { get; set; } = Guid.NewGuid().ToString();

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("title")]
    [StringLength(100)]
    public string? Title { get; set; }

    [Column("company")]
    [StringLength(200)]
    public string? Company { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("linkedin_url")]
    [StringLength(500)]
    public string? LinkedinUrl { get; set; }

    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<EventSpeaker> EventSpeakers { get; set; } = new List<EventSpeaker>();
}
