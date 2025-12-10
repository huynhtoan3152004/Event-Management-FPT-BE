using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách sự kiện (có filter + phân trang)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllEvents([FromQuery] EventFilterRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        var result = await _eventService.GetAllEventsAsync(request, userId, userRole);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một sự kiện
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        var result = await _eventService.GetEventByIdAsync(id, userId, userRole);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("{id}/seats")]
    [Authorize]
    public async Task<IActionResult> GetEventSeats(string id)
    {
        var result = await _eventService.GetEventAvailableSeatsAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Tạo sự kiện mới (Chỉ Organizer)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "organizer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateEvent([FromForm] CreateEventRequest request)
    {
        try
        {
            _logger.LogInformation("=== CreateEvent called ===");
            _logger.LogInformation("Request received - Title: {Title}, Date: {Date}", request.Title, request.Date);
            _logger.LogInformation("User: {UserId}, Role: {Role}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), 
                User.FindFirstValue(ClaimTypes.Role));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("ModelState invalid: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<EventDetailDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
            }

            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            _logger.LogInformation("Calling EventService.CreateEventAsync with organizerId: {OrganizerId}", organizerId);
            
            var result = await _eventService.CreateEventAsync(request, organizerId);

            if (!result.Success)
            {
                _logger.LogWarning("CreateEvent failed: {Message}", result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("CreateEvent succeeded - EventId: {EventId}", result.Data!.EventId);
            return CreatedAtAction(nameof(GetEventById), new { id = result.Data!.EventId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EXCEPTION in CreateEvent: {Message}", ex.Message);
            _logger.LogError("StackTrace: {StackTrace}", ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                return StatusCode(500, ApiResponse<EventDetailDto>.FailureResponse($"Lỗi server: {ex.Message} | Chi tiết: {ex.InnerException.Message}"));
            }

            return StatusCode(500, ApiResponse<EventDetailDto>.FailureResponse($"Lỗi server: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cập nhật thông tin sự kiện (Chỉ Organizer)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "organizer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateEvent(string id, [FromForm] UpdateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<EventDetailDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _eventService.UpdateEventAsync(id, request, userId, userRole);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa sự kiện (soft delete - Chỉ Organizer)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _eventService.DeleteEventAsync(id, userId, userRole);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }


    [HttpPost("{id}/publish")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> PublishEvent(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _eventService.PublishEventAsync(id, userId, userRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> CancelEvent(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _eventService.CancelEventAsync(id, userId, userRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> CompleteEvent(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _eventService.CompleteEventAsync(id, userId, userRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
