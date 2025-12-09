namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class HallAvailabilityDto
{
    public string HallId { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int TotalCapacity { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int OccupiedSeats { get; set; }
    public int ActiveEventsCount { get; set; }
}
