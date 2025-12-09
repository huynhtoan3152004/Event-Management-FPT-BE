namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class SeatDto
{
    public string SeatId { get; set; } = string.Empty;
    public string HallId { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty; // A1, B2, C3...
    public string? RowLabel { get; set; }                  // A, B, C...
    public string? Section { get; set; }                   // main, vip, balcony...
    public string Status { get; set; } = "available";      // available, reserved, occupied, maintenance
}
