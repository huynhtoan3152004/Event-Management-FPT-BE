using Microsoft.AspNetCore.Mvc;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpeakersController : ControllerBase
{
    private readonly ISpeakerService _speakerService;

    public SpeakersController(ISpeakerService speakerService)
    {
        _speakerService = speakerService;
    }

    /// <summary>
    /// Lấy danh sách tất cả speakers (có phân trang và tìm kiếm)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSpeakers([FromQuery] PaginationRequest request)
    {
        var result = await _speakerService.GetAllSpeakersAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một speaker theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSpeakerById(string id)
    {
        var result = await _speakerService.GetSpeakerByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Tạo speaker mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSpeaker([FromBody] CreateSpeakerRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<SpeakerResponseDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var result = await _speakerService.CreateSpeakerAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetSpeakerById), new { id = result.Data!.SpeakerId }, result);
    }

    /// <summary>
    /// Cập nhật thông tin speaker
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSpeaker(string id, [FromBody] UpdateSpeakerRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<SpeakerResponseDto>.FailureResponse("Dữ liệu không hợp lệ", errors));
        }

        var result = await _speakerService.UpdateSpeakerAsync(id, request);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa speaker (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSpeaker(string id)
    {
        var result = await _speakerService.DeleteSpeakerAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách sự kiện của một speaker
    /// </summary>
    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetSpeakerEvents(string id, [FromQuery] PaginationRequest request)
    {
        var result = await _speakerService.GetSpeakerEventsAsync(id, request);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
