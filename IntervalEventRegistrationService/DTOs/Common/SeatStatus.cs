namespace IntervalEventRegistrationService.DTOs.Common;

/// <summary>
/// Seat status constants (3 trạng thái chính)
/// </summary>
public static class SeatStatus
{
    public const string Available = "available";     // Còn trống
    public const string Reserved = "reserved";       // Đã được book (có ticket active)
    public const string Occupied = "occupied";       // Đã check-in (có ticket used)

    /// <summary>
    /// Check if seat can be booked
    /// </summary>
    public static bool CanBook(string status)
    {
        return status == Available;
    }

    /// <summary>
    /// Check if seat is taken
    /// </summary>
    public static bool IsTaken(string status)
    {
        return status is Reserved or Occupied;
    }

    /// <summary>
    /// Get user-friendly status name
    /// </summary>
    public static string GetDisplayName(string status)
    {
        return status switch
        {
            Available => "Còn trống",
            Reserved => "Đã đặt",
            Occupied => "Đã check-in",
            _ => "Không xác định"
        };
    }

    /// <summary>
    /// Get seat status from ticket status
    /// </summary>
    public static string FromTicketStatus(string ticketStatus)
    {
        return ticketStatus switch
        {
            TicketStatus.Active => Reserved,
            TicketStatus.Used => Occupied,
            TicketStatus.Cancelled => Available,
            _ => Available
        };
    }

    /// <summary>
    /// Get color for UI display
    /// </summary>
    public static string GetStatusColor(string status)
    {
        return status switch
        {
            Available => "success",   // Green
            Reserved => "warning",    // Yellow
            Occupied => "primary",    // Blue
            _ => "default"
        };
    }
}
