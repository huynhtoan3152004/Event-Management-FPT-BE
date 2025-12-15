using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Seat;
using IntervalEventRegistrationService.DTOs.Response.Seat;

namespace IntervalEventRegistrationService.Interfaces;

public interface ISeatService
{
    /// <summary>
    /// Get Hall seat map template (for Organizer preview)
    /// </summary>
    Task<ApiResponse<HallSeatMapDto>> GetHallSeatMapAsync(string hallId);
    
    /// <summary>
    /// Get Event seat map with real-time status
    /// </summary>
    Task<ApiResponse<SeatMapDto>> GetEventSeatMapAsync(
        string eventId, 
        string? userId = null, 
        string? userRole = null,
        SeatMapFilterRequest? filter = null
    );
    
    /// <summary>
    /// Get available seats for Student registration
    /// </summary>
    Task<ApiResponse<SeatMapDto>> GetAvailableSeatsForRegistrationAsync(string eventId);
    
    /// <summary>
    /// Check seat availability in real-time
    /// </summary>
    Task<ApiResponse<bool>> CheckSeatAvailabilityAsync(string seatId);
    
    /// <summary>
    /// Get seat statistics summary
    /// </summary>
    Task<ApiResponse<Dictionary<string, int>>> GetSeatStatisticsAsync(string eventId);
}
