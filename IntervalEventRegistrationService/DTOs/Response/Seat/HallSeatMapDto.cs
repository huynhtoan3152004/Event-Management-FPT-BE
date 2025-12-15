namespace IntervalEventRegistrationService.DTOs.Response.Seat;

/// <summary>
/// Seat map template của Hall (chưa có event cụ thể)
/// </summary>
public class HallSeatMapDto
{
    public string HallId { get; set; } = string.Empty;
    public string HallName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxRows { get; set; }
    public int MaxSeatsPerRow { get; set; }
    public int TotalSeatsGenerated { get; set; }
    public List<HallSeatRowDto> Rows { get; set; } = new();
}

public class HallSeatRowDto
{
    public int RowNumber { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public List<HallSeatItemDto> Seats { get; set; } = new();
}

public class HallSeatItemDto
{
    public string SeatId { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public int SeatNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
