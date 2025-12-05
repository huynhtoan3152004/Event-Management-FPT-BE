using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("user_auth_providers")]
public class UserAuthProvider
{
    [Key]
    [Column("user_auth_id")]
    [StringLength(50)]
    public string UserAuthId { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    [Column("provider_id")]
    [StringLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    [Column("provider_user_id")]
    [StringLength(100)]
    public string ProviderUserId { get; set; } = string.Empty; // sub trong Google ID token

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("display_name")]
    [StringLength(100)]
    public string? DisplayName { get; set; }

    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("access_token")]
    public string? AccessToken { get; set; }

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("id_token")]
    public string? IdToken { get; set; }

    [Column("token_expiry")]
    public DateTime? TokenExpiry { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("ProviderId")]
    public virtual AuthProvider? AuthProvider { get; set; }
}
