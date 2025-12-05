using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntervalEventRegistrationRepo.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    [StringLength(50)]
    public string UserId { get; set; } = Guid.NewGuid().ToString();

    [Column("role_id")]
    [StringLength(50)]
    public string? RoleId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("avatar_url")]
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("password_hash")]
    [StringLength(256)]
    public string? PasswordHash { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "active";

    // Trường đặc thù theo role
    [Column("student_code")]
    [StringLength(20)]
    public string? StudentCode { get; set; }

    [Column("organization")]
    [StringLength(200)]
    public string? Organization { get; set; }

    [Column("department")]
    [StringLength(100)]
    public string? Department { get; set; }

    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }

    public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<EventStaff> EventStaffs { get; set; } = new List<EventStaff>();
    public virtual ICollection<TicketCheckin> TicketCheckins { get; set; } = new List<TicketCheckin>();
    public virtual ICollection<UserAuthProvider> UserAuthProviders { get; set; } = new List<UserAuthProvider>();
}
