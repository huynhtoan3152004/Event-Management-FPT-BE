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

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
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

    /// <summary>
    /// Tạo sự kiện mới (Chỉ Organizer)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "organizer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateEvent([FromForm] CreateEventRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<EventDetailDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _eventService.CreateEventAsync(request, organizerId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetEventById), new { id = result.Data!.EventId }, result);
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
}