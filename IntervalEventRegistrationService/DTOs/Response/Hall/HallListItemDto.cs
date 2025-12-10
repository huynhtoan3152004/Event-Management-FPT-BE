namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class HallListItemDto
{
    public string HallId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalSeats { get; set; } // Số ghế đã tạo
    public DateTime CreatedAt { get; set; }
}
