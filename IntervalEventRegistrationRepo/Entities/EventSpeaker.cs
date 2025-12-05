using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("event_speakers")]
public class EventSpeaker
{
    [Column("event_id")]
    [StringLength(50)]
    public string EventId { get; set; } = string.Empty;

    [Column("speaker_id")]
    [StringLength(50)]
    public string SpeakerId { get; set; } = string.Empty;

    [Column("role")]
    [StringLength(50)]
    public string? Role { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("EventId")]
    public virtual Event? Event { get; set; }

    [ForeignKey("SpeakerId")]
    public virtual Speaker? Speaker { get; set; }
}
