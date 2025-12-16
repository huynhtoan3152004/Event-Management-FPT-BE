namespace IntervalEventRegistrationService.DTOs.Response.Ticket;

/// <summary>
/// Response sau khi checkout thành công
/// </summary>
public class CheckoutResponseDto
{
    public string TicketId { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string SeatLabel { get; set; } = string.Empty;
    
    public DateTime CheckinTime { get; set; }
    public DateTime CheckoutTime { get; set; }
    public TimeSpan Duration { get; set; } // Thời gian tham dự
    
    public string CheckedOutBy { get; set; } = string.Empty; // Staff name
    public string Message { get; set; } = "Check-out thành công!";
}
