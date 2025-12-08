namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class SeatDto
{
    public string SeatId { get; set; } = string.Empty;
    public string SeatCode { get; set; } = string.Empty; // A1, B2, VIP-1
    public string? SeatRow { get; set; }
    public int? SeatNumber { get; set; }
    public string SeatType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
