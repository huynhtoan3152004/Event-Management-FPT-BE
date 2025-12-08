using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response; 

namespace IntervalEventRegistrationService.Interfaces
{
    /// <summary>
    /// Event Service Interface
    /// Phân quyền: 
    /// - Student: Chỉ xem events published
    /// - Organizer: Xem/Tạo/Sửa/Xóa tất cả events
    /// </summary>
    public interface IEventService
    {
        Task<PagedResponse<EventListItemDto>> GetAllEventsAsync(
            EventFilterRequest request, 
            string? currentUserId = null, 
            string? currentUserRole = null);

        Task<ApiResponse<EventDetailDto>> GetEventByIdAsync(
            string eventId, 
            string? currentUserId = null, 
            string? currentUserRole = null);

        Task<ApiResponse<EventDetailDto>> CreateEventAsync(
            CreateEventRequest request, 
            string organizerId);

        Task<ApiResponse<EventDetailDto>> UpdateEventAsync(
            string eventId, 
            UpdateEventRequest request, 
            string currentUserId,
            string currentUserRole);

        Task<ApiResponse<bool>> DeleteEventAsync(
            string eventId, 
            string currentUserId, 
            string currentUserRole);
    }
}