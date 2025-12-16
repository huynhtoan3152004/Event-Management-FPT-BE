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
    public int CheckedOutCount { get; set; } // ✨ NEW
    public int StillInVenueCount { get; set; } // ✨ NEW - Checked-in but not checked-out
    public double CheckInRate { get; set; } // Percentage
    public double CheckOutRate { get; set; } // ✨ NEW - CheckedOut / CheckedIn * 100
    
    // ✨ NEW - Attendance duration statistics
    public TimeSpan? AverageAttendanceDuration { get; set; }
    public TimeSpan? MinAttendanceDuration { get; set; }
    public TimeSpan? MaxAttendanceDuration { get; set; }
    
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
    public DateTime? CheckOutTime { get; set; } // ✨ NEW
    public TimeSpan? Duration { get; set; } // ✨ NEW
    public string Status { get; set; } = string.Empty; // "Đã đăng ký", "Đã check-in", "Đã check-out"
}
