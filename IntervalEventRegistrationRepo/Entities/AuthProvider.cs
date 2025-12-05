using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("auth_providers")]
public class AuthProvider
{
    [Key]
    [Column("provider_id")]
    [StringLength(50)]
    public string ProviderId { get; set; } = string.Empty; // e.g., 'google'

    [Column("provider_name")]
    [StringLength(50)]
    public string ProviderName { get; set; } = string.Empty; // e.g., 'Google'

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserAuthProvider> UserAuthProviders { get; set; } = new List<UserAuthProvider>();
}
