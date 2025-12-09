using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICloudinaryService _cloudinaryService;

    public EventService(IEventRepository eventRepository, ICloudinaryService cloudinaryService)
    {
        _eventRepository = eventRepository;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<PagedResponse<EventListItemDto>> GetAllEventsAsync(
        EventFilterRequest request, 
        string? currentUserId = null, 
        string? currentUserRole = null)
    {
        // For students, only show published events
        // Organizers can see all events
        var statusFilter = request.Status;
        if (currentUserRole == "student" && string.IsNullOrWhiteSpace(statusFilter))
        {
            statusFilter = "published";
        }

        var (events, totalCount) = await _eventRepository.GetAllAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            statusFilter,
            request.DateFrom,
            request.DateTo,
            request.HallId,
            request.OrganizerId);

        var eventDtos = events.Select(e => MapToListItemDto(e)).ToList();

        return new PagedResponse<EventListItemDto>
        {
            Success = true,
            Message = "Lấy danh sách sự kiện thành công",
            Data = eventDtos,
            Pagination = new PaginationMeta
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            }
        };
    }

    public async Task<ApiResponse<EventDetailDto>> GetEventByIdAsync(
        string eventId, 
        string? currentUserId = null, 
        string? currentUserRole = null)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, includeRelations: true);

        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }

        // Check permission for draft/pending/rejected events
        // Only organizer role or event owner can view
        if (eventEntity.Status is "draft" or "pending" or "rejected")
        {
            if (currentUserRole != "organizer" && eventEntity.OrganizerId != currentUserId)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền xem sự kiện này");
            }
        }

        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Lấy thông tin sự kiện thành công");
    }

    public async Task<ApiResponse<EventDetailDto>> CreateEventAsync(CreateEventRequest request, string organizerId)
    {
        // Validate time
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Thời gian kết thúc phải sau thời gian bắt đầu");
        }

        if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Ngày sự kiện không được ở quá khứ");
        }

        // Upload image if provided
        string? imageUrl = null;
        if (request.ImageFile != null)
        {
            imageUrl = await _cloudinaryService.UploadAsync(request.ImageFile, "events");
        }

        var eventEntity = new Event
        {
            EventId = Guid.NewGuid().ToString(),
            Title = request.Title,
            Description = request.Description,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location,
            HallId = request.HallId,
            OrganizerId = organizerId,
            ClubId = request.ClubId,
            ClubName = request.ClubName,
            ImageUrl = imageUrl,
            Status = "draft", // Default status
            TotalSeats = request.TotalSeats,
            RegisteredCount = 0,
            CheckedInCount = 0,
            Tags = request.Tags,
            MaxTicketsPerUser = request.MaxTicketsPerUser,
            RegistrationStart = request.RegistrationStart,
            RegistrationEnd = request.RegistrationEnd,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        var createdEvent = await _eventRepository.GetByIdAsync(eventEntity.EventId, includeRelations: true);
        var dto = MapToDetailDto(createdEvent!);

        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Tạo sự kiện thành công");
    }

    public async Task<ApiResponse<EventDetailDto>> UpdateEventAsync(
        string eventId, 
        UpdateEventRequest request, 
        string currentUserId, 
        string currentUserRole)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }

        // Check permission - Only organizer role can update
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền chỉnh sửa sự kiện này");
        }

        // Organizers can edit all events including cancelled/completed

        // Validate time
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Thời gian kết thúc phải sau thời gian bắt đầu");
        }

        // Cannot reduce total seats below registered count
        if (request.TotalSeats < eventEntity.RegisteredCount)
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                $"Không thể giảm số ghế xuống dưới {eventEntity.RegisteredCount} (số người đã đăng ký)");
        }

        // Upload new image if provided
        if (request.ImageFile != null)
        {
            eventEntity.ImageUrl = await _cloudinaryService.UploadAsync(request.ImageFile, "events");
        }

        // Update fields
        eventEntity.Title = request.Title;
        eventEntity.Description = request.Description;
        eventEntity.Date = request.Date;
        eventEntity.StartTime = request.StartTime;
        eventEntity.EndTime = request.EndTime;
        eventEntity.Location = request.Location;
        eventEntity.HallId = request.HallId;
        eventEntity.ClubId = request.ClubId;
        eventEntity.ClubName = request.ClubName;
        eventEntity.TotalSeats = request.TotalSeats;
        eventEntity.Tags = request.Tags;
        eventEntity.MaxTicketsPerUser = request.MaxTicketsPerUser;
        eventEntity.RegistrationStart = request.RegistrationStart;
        eventEntity.RegistrationEnd = request.RegistrationEnd;

        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        var updatedEvent = await _eventRepository.GetByIdAsync(eventId, includeRelations: true);
        var dto = MapToDetailDto(updatedEvent!);

        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Cập nhật sự kiện thành công");
    }

    public async Task<ApiResponse<bool>> DeleteEventAsync(string eventId, string currentUserId, string currentUserRole)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);

        if (eventEntity == null)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy sự kiện");
        }

        // Only organizer can delete
        if (currentUserRole != "organizer")
        {
            return ApiResponse<bool>.FailureResponse("Chỉ Organizer mới có quyền xóa sự kiện");
        }

        var result = await _eventRepository.DeleteAsync(eventId);
        await _eventRepository.SaveChangesAsync();

        if (!result)
        {
            return ApiResponse<bool>.FailureResponse("Xóa sự kiện thất bại");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa sự kiện thành công");
    }

    // Helper methods
    private EventListItemDto MapToListItemDto(Event e)
    {
        return new EventListItemDto
        {
            EventId = e.EventId,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Location = e.Location,
            ImageUrl = e.ImageUrl,
            Status = e.Status,
            TotalSeats = e.TotalSeats,
            RegisteredCount = e.RegisteredCount,
            ClubName = e.ClubName,
            RegistrationStart = e.RegistrationStart,
            RegistrationEnd = e.RegistrationEnd,
            CreatedAt = e.CreatedAt
        };
    }

    private EventDetailDto MapToDetailDto(Event e)
    {
        return new EventDetailDto
        {
            EventId = e.EventId,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Location = e.Location,
            ImageUrl = e.ImageUrl,
            Status = e.Status,
            TotalSeats = e.TotalSeats,
            RegisteredCount = e.RegisteredCount,
            ClubName = e.ClubName,
            RegistrationStart = e.RegistrationStart,
            RegistrationEnd = e.RegistrationEnd,
            CreatedAt = e.CreatedAt,
            HallId = e.HallId,
            HallName = e.Hall?.Name,
            OrganizerId = e.OrganizerId,
            OrganizerName = e.Organizer?.Name,
            ClubId = e.ClubId,
            CheckedInCount = e.CheckedInCount,
            Tags = e.Tags,
            MaxTicketsPerUser = e.MaxTicketsPerUser,
            ApprovedBy = e.ApprovedBy,
            ApprovedAt = e.ApprovedAt,
            RejectionReason = e.RejectionReason,
            UpdatedAt = e.UpdatedAt
        };
    }
}