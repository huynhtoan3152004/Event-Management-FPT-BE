using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    [StringLength(50)]
    public string RoleId { get; set; } = Guid.NewGuid().ToString();

    [Column("role_name")]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("permissions")]
    public string? Permissions { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
