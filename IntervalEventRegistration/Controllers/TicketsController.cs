using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Ticket;
using IntervalEventRegistrationService.DTOs.Response.Ticket;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistration.Controllers;

[ApiController]
[Route("api")] 
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost("events/{id}/register")]
    [Authorize(Roles = "student")] 
    public async Task<IActionResult> Register(string id, [FromBody] RegisterTicketRequestDto request)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _ticketService.RegisterAsync(id, studentId, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("tickets/{ticketCode}")]
    [Authorize]
    public async Task<IActionResult> GetByCode(string ticketCode)
    {
        var result = await _ticketService.GetByCodeAsync(ticketCode);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost("tickets/{ticketCode}/checkin")]
    [Authorize(Roles = "staff,organizer")]
    public async Task<IActionResult> Checkin(string ticketCode)
    {
        var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var staffRole = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _ticketService.CheckinByCodeAsync(ticketCode, staffId, staffRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("tickets/{ticketId}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(string ticketId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userRole = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _ticketService.CancelAsync(ticketId, userId, userRole);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("events/{id}/tickets")]
    [Authorize(Roles = "organizer,staff")]
    public async Task<IActionResult> GetEventTickets(string id)
    {
        var result = await _ticketService.GetByEventAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpGet("users/me/tickets")]
    [Authorize]
    public async Task<IActionResult> GetMyTickets()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _ticketService.GetByStudentAsync(studentId);
        return Ok(result);
    }
}
