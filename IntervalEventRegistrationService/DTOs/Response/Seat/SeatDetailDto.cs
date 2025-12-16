namespace IntervalEventRegistrationService.DTOs.Response.Seat;

/// <summary>
/// Chi tiết đầy đủ của một ghế khi click vào
/// </summary>
public class SeatDetailDto
{
    public string SeatId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string? HallId { get; set; }
    
    // Thông tin ghế
    public string Label { get; set; } = string.Empty; // A9, B12, etc.
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string RowLabel { get; set; } = string.Empty; // A, B, C...
    
    // Trạng thái
    public string Status { get; set; } = string.Empty; // available, reserved, occupied
    public string StatusDisplay { get; set; } = string.Empty; // "Còn trống", "Đã đặt", "Đã check-in"
    
    // Thông tin người đặt (nếu có)
    public bool IsBooked { get; set; } = false;
    public SeatOccupantDetailDto? Occupant { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Thông tin chi tiết người đặt ghế
/// </summary>
public class SeatOccupantDetailDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    
    // Ticket info
    public string TicketId { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string TicketStatus { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    
    // Check-in info
    public bool IsCheckedIn { get; set; } = false;
    public DateTime? CheckInTime { get; set; }
    public string? CheckedInBy { get; set; } // Staff name who checked in
}
