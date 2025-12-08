namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class HallAvailabilityDto
{
    public string HallId { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<ConflictingEventDto> ConflictingEvents { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class ConflictingEventDto
{
    public string EventId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
