using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Speaker> Speakers { get; set; }
    public DbSet<AuthProvider> AuthProviders { get; set; }
    public DbSet<UserAuthProvider> UserAuthProviders { get; set; }
    public DbSet<Hall> Halls { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<EventStaff> EventStaffs { get; set; }
    public DbSet<EventSpeaker> EventSpeakers { get; set; }
    public DbSet<TicketCheckin> TicketCheckins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ===== COMPOSITE KEYS =====
        
        // EventStaff - composite key (event_id, staff_id)
        modelBuilder.Entity<EventStaff>()
            .HasKey(es => new { es.EventId, es.StaffId });

        // EventSpeaker - composite key (event_id, speaker_id)
        modelBuilder.Entity<EventSpeaker>()
            .HasKey(es => new { es.EventId, es.SpeakerId });

        // ===== UNIQUE INDEXES =====
        
        // Role
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RoleName)
            .IsUnique();

        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.StudentCode)
            .IsUnique()
            .HasFilter("student_code IS NOT NULL");

        // AuthProvider
        modelBuilder.Entity<AuthProvider>()
            .HasIndex(ap => ap.ProviderName)
            .IsUnique();

        // UserAuthProvider - unique constraint (provider_id, provider_user_id)
        modelBuilder.Entity<UserAuthProvider>()
            .HasIndex(uap => new { uap.ProviderId, uap.ProviderUserId })
            .IsUnique();

        // Ticket
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TicketCode)
            .IsUnique();

        // ===== RELATIONSHIPS =====

        // User -> Role (Many-to-One)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Event -> Hall (Many-to-One)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Hall)
            .WithMany(h => h.Events)
            .HasForeignKey(e => e.HallId)
            .OnDelete(DeleteBehavior.SetNull);

        // Event -> User/Organizer (Many-to-One)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.OrganizedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seat -> Hall (Many-to-One)
        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Hall)
            .WithMany(h => h.Seats)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ticket -> Event (Many-to-One)
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket -> User/Student (Many-to-One)
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Student)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket -> Seat (Many-to-One)
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Seat)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.SeatId)
            .OnDelete(DeleteBehavior.SetNull);

        // EventStaff -> Event (Many-to-One)
        modelBuilder.Entity<EventStaff>()
            .HasOne(es => es.Event)
            .WithMany(e => e.EventStaffs)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventStaff -> User/Staff (Many-to-One)
        modelBuilder.Entity<EventStaff>()
            .HasOne(es => es.Staff)
            .WithMany(u => u.EventStaffs)
            .HasForeignKey(es => es.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventSpeaker -> Event (Many-to-One)
        modelBuilder.Entity<EventSpeaker>()
            .HasOne(es => es.Event)
            .WithMany(e => e.EventSpeakers)
            .HasForeignKey(es => es.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // EventSpeaker -> Speaker (Many-to-One)
        modelBuilder.Entity<EventSpeaker>()
            .HasOne(es => es.Speaker)
            .WithMany(s => s.EventSpeakers)
            .HasForeignKey(es => es.SpeakerId)
            .OnDelete(DeleteBehavior.Cascade);

        // TicketCheckin -> Ticket (Many-to-One)
        modelBuilder.Entity<TicketCheckin>()
            .HasOne(tc => tc.Ticket)
            .WithMany(t => t.TicketCheckins)
            .HasForeignKey(tc => tc.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // TicketCheckin -> User/Staff (Many-to-One)
        modelBuilder.Entity<TicketCheckin>()
            .HasOne(tc => tc.Staff)
            .WithMany(u => u.TicketCheckins)
            .HasForeignKey(tc => tc.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserAuthProvider -> User (Many-to-One)
        modelBuilder.Entity<UserAuthProvider>()
            .HasOne(uap => uap.User)
            .WithMany(u => u.UserAuthProviders)
            .HasForeignKey(uap => uap.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserAuthProvider -> AuthProvider (Many-to-One)
        modelBuilder.Entity<UserAuthProvider>()
            .HasOne(uap => uap.AuthProvider)
            .WithMany(ap => ap.UserAuthProviders)
            .HasForeignKey(uap => uap.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== GLOBAL QUERY FILTERS (Soft Delete) =====
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Speaker>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Hall>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<Seat>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);

        // ===== SEED DATA =====
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = "admin", RoleName = "Admin", Description = "System Administrator", Permissions = "*" },
            new Role { RoleId = "organizer", RoleName = "Organizer", Description = "Event Organizer", Permissions = "events.*,tickets.read,reports.read" },
            new Role { RoleId = "staff", RoleName = "Staff", Description = "Event Staff", Permissions = "checkin.*,events.read" },
            new Role { RoleId = "student", RoleName = "Student", Description = "Student/Attendee", Permissions = "events.read,tickets.own" }
        );

        // Seed AuthProviders
        modelBuilder.Entity<AuthProvider>().HasData(
            new AuthProvider { ProviderId = "google", ProviderName = "Google", Description = "Google OAuth 2.0" },
            new AuthProvider { ProviderId = "local", ProviderName = "Local", Description = "Local username/password" }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Set UpdatedAt for modified entities
            if (entry.State == EntityState.Modified)
            {
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.CurrentValue = DateTime.UtcNow;
                }
            }

            // Set CreatedAt for new entities
            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProperty != null && createdAtProperty.CurrentValue == null)
                {
                    createdAtProperty.CurrentValue = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
