using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Hall;
using IntervalEventRegistrationService.DTOs.Response.Hall;

namespace IntervalEventRegistrationService.Interfaces;

/// <summary>
/// Hall Service Interface
/// Phân quyền: 
/// - Public/Student: Chỉ xem halls và availability
/// - Organizer: Full CRUD + generate seats
/// </summary>
public interface IHallService
{
    Task<PagedResponse<HallListItemDto>> GetAllHallsAsync(HallFilterRequest request);
    Task<ApiResponse<HallDetailDto>> GetHallByIdAsync(string hallId);
    Task<ApiResponse<HallDetailDto>> CreateHallAsync(CreateHallRequestDto request, string organizerId);
    Task<ApiResponse<HallDetailDto>> UpdateHallAsync(string hallId, UpdateHallRequestDto request, string currentUserId, string currentUserRole);
    Task<ApiResponse<bool>> DeleteHallAsync(string hallId, string currentUserId, string currentUserRole);
    Task<ApiResponse<List<SeatDto>>> GetHallSeatsAsync(string hallId, string? seatType = null, bool? isActive = null);
    Task<ApiResponse<List<SeatDto>>> GenerateSeatsAsync(string hallId, GenerateSeatsRequestDto request, string currentUserId, string currentUserRole);
    Task<ApiResponse<HallAvailabilityDto>> CheckAvailabilityAsync(string hallId);
}
