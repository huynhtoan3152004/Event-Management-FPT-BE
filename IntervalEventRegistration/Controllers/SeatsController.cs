using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IntervalEventRegistrationService.DTOs.Request.Seat;
using IntervalEventRegistrationService.Interfaces;
using IntervalEventRegistrationService.DTOs.Response.Seat;

namespace IntervalEventRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;
    private readonly ILogger<SeatsController> _logger;

    public SeatsController(ISeatService seatService, ILogger<SeatsController> logger)
    {
        _seatService = seatService;
        _logger = logger;
    }

    /// <summary>
    /// Get Hall seat map template (for Organizer to preview before creating event)
    /// </summary>
    /// <param name="hallId">Hall ID</param>
    /// <returns>Hall seat layout</returns>
    [HttpGet("halls/{hallId}/seat-map")]
    [Authorize(Roles = "organizer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHallSeatMap(string hallId)
    {
        _logger.LogInformation("Organizer getting hall seat map: {HallId}", hallId);
        
        var result = await _seatService.GetHallSeatMapAsync(hallId);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get Event seat map with real-time status (Organizer/Staff only)
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="filter">Optional filters</param>
    /// <returns>Event seat map with status and occupant details</returns>
    [HttpGet("events/{eventId}/seat-map")]
    [Authorize(Roles = "organizer,staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventSeatMap(
        string eventId,
        [FromQuery] SeatMapFilterRequest? filter = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        _logger.LogInformation(
            "{Role} getting event seat map: {EventId}",
            userRole, eventId
        );

        // Default: include occupant details for organizer/staff
        if (filter == null)
        {
            filter = new SeatMapFilterRequest { IncludeOccupantDetails = true };
        }
        else if (!filter.IncludeOccupantDetails)
        {
            filter.IncludeOccupantDetails = true;
        }

        var result = await _seatService.GetEventSeatMapAsync(eventId, userId, userRole, filter);
        
        if (!result.Success)
        {
            return result.Message.Contains("không có quyền") 
                ? Forbid() 
                : NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get available seats for Student registration
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Available seats only (for seat selection)</returns>
    [HttpGet("events/{eventId}/available-seats")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSeats(string eventId)
    {
        _logger.LogInformation("Student getting available seats for event: {EventId}", eventId);
        
        var result = await _seatService.GetAvailableSeatsForRegistrationAsync(eventId);
        
        if (!result.Success)
        {
            return result.Message.Contains("không tồn tại") 
                ? NotFound(result) 
                : BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if a specific seat is available (real-time check)
    /// </summary>
    /// <param name="seatId">Seat ID</param>
    /// <returns>Boolean indicating availability</returns>
    [HttpGet("{seatId}/availability")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckSeatAvailability(string seatId)
    {
        _logger.LogInformation("Checking seat availability: {SeatId}", seatId);
        
        var result = await _seatService.CheckSeatAvailabilityAsync(seatId);
        
        return Ok(result);
    }

    /// <summary>
    /// Get seat statistics for an Event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Seat count by status</returns>
    [HttpGet("events/{eventId}/statistics")]
    [Authorize(Roles = "organizer,staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeatStatistics(string eventId)
    {
        _logger.LogInformation("Getting seat statistics for event: {EventId}", eventId);
        
        var result = await _seatService.GetSeatStatisticsAsync(eventId);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get seat map for check-in dashboard (Staff/Organizer)
    /// Shows real-time check-in status with student info
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Seat map with check-in status</returns>
    [HttpGet("events/{eventId}/checkin-map")]
    [Authorize(Roles = "organizer,staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCheckinSeatMap(string eventId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        _logger.LogInformation(
            "Staff getting check-in seat map for event: {EventId}",
            eventId
        );

        var filter = new SeatMapFilterRequest
        {
            IncludeOccupantDetails = true,
            Statuses = new List<string> { "reserved", "occupied" } // Only show booked seats
        };

        var result = await _seatService.GetEventSeatMapAsync(eventId, userId, userRole, filter);
        
        if (!result.Success)
        {
            return result.Message.Contains("không có quyền") 
                ? Forbid() 
                : NotFound(result);
        }

        return Ok(result);
    }
}
