namespace IntervalEventRegistrationService.DTOs.Response.Ticket;

public class TicketDto
{
    public string TicketId { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string? EventTitle { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly EventStartTime { get; set; }
    public TimeOnly EventEndTime { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? SeatId { get; set; }
    public string? SeatNumber { get; set; }
}

public class CheckinResultDto
{
    public string Result { get; set; } = string.Empty;
}
