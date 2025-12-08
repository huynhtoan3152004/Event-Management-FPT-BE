namespace IntervalEventRegistrationService.DTOs.Response;

public class EventListItemDto
{
    public string EventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int RegisteredCount { get; set; }
    public int AvailableSeats => TotalSeats - RegisteredCount;
    public string? ClubName { get; set; }
    public DateTime? RegistrationStart { get; set; }
    public DateTime? RegistrationEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EventDetailDto : EventListItemDto
{
    public string? HallId { get; set; }
    public string? HallName { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public string? OrganizerName { get; set; }
    public string? ClubId { get; set; }
    public int CheckedInCount { get; set; }
    public string? Tags { get; set; }
    public int MaxTicketsPerUser { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? UpdatedAt { get; set; }
}