namespace IntervalEventRegistrationService.DTOs.Response.Seat;

public class SeatMapDto
{
    public string EventId { get; set; } = string.Empty;
    public string? HallId { get; set; }
    public string HallName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int MaxSeatsPerRow { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int ReservedSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public List<SeatRowDto> Rows { get; set; } = new();
}

public class SeatRowDto
{
    public int RowNumber { get; set; }
    public string RowLabel { get; set; } = string.Empty; // A, B, C...
    public List<SeatItemDto> Seats { get; set; } = new();
}

public class SeatItemDto
{
    public string SeatId { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string Label { get; set; } = string.Empty; // A1, A2, B1...
    public string Status { get; set; } = string.Empty; // available, reserved, occupied
    
    // Optional - chi tiết cho Organizer/Staff
    public SeatOccupantDto? Occupant { get; set; }
}

public class SeatOccupantDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string TicketStatus { get; set; } = string.Empty;
}
