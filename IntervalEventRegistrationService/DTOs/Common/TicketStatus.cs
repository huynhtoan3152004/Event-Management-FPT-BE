namespace IntervalEventRegistrationService.DTOs.Common;

/// <summary>
/// Ticket status constants (3 trạng thái chính)
/// </summary>
public static class TicketStatus
{
    public const string Active = "active";           // Đã đăng ký, chưa check-in
    public const string Used = "used";               // Đã check-in
    public const string Cancelled = "cancelled";     // Đã hủy

    /// <summary>
    /// Check if ticket is active
    /// </summary>
    public static bool IsActive(string status)
    {
        return status == Active;
    }

    /// <summary>
    /// Check if ticket can check-in
    /// </summary>
    public static bool CanCheckIn(string status)
    {
        return status == Active;
    }

    /// <summary>
    /// Check if ticket can be cancelled
    /// </summary>
    public static bool CanCancel(string status)
    {
        return status == Active;
    }

    /// <summary>
    /// Get user-friendly status name
    /// </summary>
    public static string GetDisplayName(string status)
    {
        return status switch
        {
            Active => "Đã đăng ký",
            Used => "Đã check-in",
            Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }

    /// <summary>
    /// Get color for UI display
    /// </summary>
    public static string GetStatusColor(string status)
    {
        return status switch
        {
            Active => "success",      // Green
            Used => "primary",        // Blue
            Cancelled => "danger",    // Red
            _ => "default"
        };
    }

    /// <summary>
    /// Get icon for UI display
    /// </summary>
    public static string GetStatusIcon(string status)
    {
        return status switch
        {
            Active => "✓",           // Checkmark
            Used => "✓✓",            // Double checkmark
            Cancelled => "✗",        // X mark
            _ => "?"
        };
    }
}
