namespace IntervalEventRegistrationService.DTOs.Response;

public class EventStatisticsDto
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
    
    // Statistics
    public int TotalSeats { get; set; }
    public int RegisteredCount { get; set; }
    public int CheckedInCount { get; set; }
    public double CheckInRate { get; set; } // Percentage
    
    // Speakers
    public List<SpeakerSimpleDto>? Speakers { get; set; }
    
    // Recent check-ins
    public List<RecentCheckInDto> RecentCheckIns { get; set; } = new();
}

public class RecentCheckInDto
{
    public string AttendeeName { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string? SeatNumber { get; set; }
    public DateTime CheckInTime { get; set; }
    public string Status { get; set; } = string.Empty; // "Entered" or "Already Used"
}
