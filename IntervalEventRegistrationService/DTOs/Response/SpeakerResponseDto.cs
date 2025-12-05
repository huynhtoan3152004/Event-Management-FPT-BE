namespace IntervalEventRegistrationService.DTOs.Response;

public class SpeakerResponseDto
{
    public string SpeakerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SpeakerDetailResponseDto : SpeakerResponseDto
{
    public int TotalEvents { get; set; }
}

public class SpeakerEventResponseDto
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
}
