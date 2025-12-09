using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Hall;
using IntervalEventRegistrationService.DTOs.Response.Hall;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HallsController : ControllerBase
{
    private readonly IHallService _hallService;

    public HallsController(IHallService hallService)
    {
        _hallService = hallService;
    }

    /// <summary>
    /// Lấy danh sách hội trường (có filter + phân trang)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllHalls([FromQuery] HallFilterRequest request)
    {
        var result = await _hallService.GetAllHallsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một hội trường
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHallById(string id)
    {
        var result = await _hallService.GetHallByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo hội trường mới (Chỉ Organizer)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> CreateHall([FromBody] CreateHallRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<HallDetailDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _hallService.CreateHallAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetHallById), new { id = result.Data?.HallId }, result);
    }

    /// <summary>
    /// Cập nhật thông tin hội trường (Chỉ Organizer)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> UpdateHall(string id, [FromBody] UpdateHallRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<HallDetailDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _hallService.UpdateHallAsync(id, request, userId, userRole);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa hội trường (soft delete - Chỉ Organizer)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> DeleteHall(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _hallService.DeleteHallAsync(id, userId, userRole);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách ghế của hội trường
    /// </summary>
    [HttpGet("{id}/seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHallSeats(
        string id, 
        [FromQuery] string? seatType = null, 
        [FromQuery] bool? isActive = null)
    {
        var result = await _hallService.GetHallSeatsAsync(id, seatType, isActive);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo ghế tự động cho hội trường (Chỉ Organizer)
    /// </summary>
    [HttpPost("{id}/seats/generate")]
    [Authorize(Roles = "organizer")]
    public async Task<IActionResult> GenerateSeats(string id, [FromBody] GenerateSeatsRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<List<SeatDto>>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;

        var result = await _hallService.GenerateSeatsAsync(id, request, userId, userRole);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Kiểm tra hội trường có trống không (Chỉ cần HallId)
    /// </summary>
    [HttpGet("{id}/availability")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAvailability(string id)
    {
        var result = await _hallService.CheckAvailabilityAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
