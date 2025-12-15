namespace IntervalEventRegistrationService.DTOs.Request.Seat;

public class SeatMapFilterRequest
{
    /// <summary>
    /// Filter by status: available, reserved, occupied
    /// </summary>
    public List<string>? Statuses { get; set; }
    
    /// <summary>
    /// Include occupant details (only for Organizer/Staff)
    /// </summary>
    public bool IncludeOccupantDetails { get; set; } = false;
    
    /// <summary>
    /// Filter by specific rows
    /// </summary>
    public List<int>? RowNumbers { get; set; }
}
