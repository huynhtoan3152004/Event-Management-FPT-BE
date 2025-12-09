using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Ticket;
using IntervalEventRegistrationService.DTOs.Response.Ticket;

namespace IntervalEventRegistrationService.Interfaces;

public interface ITicketService
{
    Task<ApiResponse<TicketDto>> RegisterAsync(string eventId, string studentId, RegisterTicketRequestDto request);
    Task<ApiResponse<TicketDto>> GetByCodeAsync(string ticketCode);
    Task<ApiResponse<CheckinResultDto>> CheckinByCodeAsync(string ticketCode, string staffId, string staffRole);
    Task<ApiResponse<bool>> CancelAsync(string ticketId, string currentUserId, string currentUserRole);
    Task<ApiResponse<List<TicketDto>>> GetByEventAsync(string eventId);
    Task<ApiResponse<List<TicketDto>>> GetByStudentAsync(string studentId);
}
