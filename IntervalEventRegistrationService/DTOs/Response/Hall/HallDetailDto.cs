namespace IntervalEventRegistrationService.DTOs.Response.Hall;

public class HallDetailDto : HallListItemDto
{
    public string? Facilities { get; set; }
    public FacilitiesDto? FacilitiesParsed { get; set; } // Parse JSON
    public List<SeatDto> Seats { get; set; } = new();
    public int ActiveEventsCount { get; set; } // Số event đang dùng hall
    public DateTime? UpdatedAt { get; set; }
}

public class FacilitiesDto
{
    public bool Projector { get; set; }
    public bool Microphone { get; set; }
    public bool Wifi { get; set; }
    public bool AirConditioner { get; set; }
    public bool Whiteboard { get; set; }
    public string? OtherFacilities { get; set; }
}
