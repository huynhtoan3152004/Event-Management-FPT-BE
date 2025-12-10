using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.DTOs.Response.Hall;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IHallRepository _hallRepository;
    private readonly ISeatRepository _seatRepository;

    public EventService(IEventRepository eventRepository, ICloudinaryService cloudinaryService, IHallRepository hallRepository, ISeatRepository seatRepository)
    {
        _eventRepository = eventRepository;
        _cloudinaryService = cloudinaryService;
        _hallRepository = hallRepository;
        _seatRepository = seatRepository;
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

        foreach (var e in events)
        {
            await ApplyAutoTransitionsAsync(e);
        }

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

        await ApplyAutoTransitionsAsync(eventEntity);
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

        // Validate Hall exists and get configuration
        var hall = await _hallRepository.GetByIdAsync(request.HallId);
        if (hall == null || hall.IsDeleted)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy hội trường");
        }

        // Auto get seat configuration from Hall
        var totalSeats = hall.Capacity;
        var maxRows = hall.MaxRows;
        var maxSeatsPerRow = hall.MaxSeatsPerRow;

        // Check hall availability
        var hallAvailable = await _hallRepository.IsHallAvailableAsync(request.HallId, request.Date, request.StartTime, request.EndTime);
        if (!hallAvailable)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Hội trường đang có sự kiện trùng thời gian");
        }

        // Check 5-hour gap requirement
        var (sameDayEvents, _) = await _eventRepository.GetAllAsync(1, 1000, null, null, request.Date, request.Date, request.HallId, null);
        foreach (var e in sameDayEvents)
        {
            var startA = request.StartTime;
            var endA = request.EndTime;
            var startB = e.StartTime;
            var endB = e.EndTime;
            if (endA <= startB)
            {
                var gap = startB.ToTimeSpan() - endA.ToTimeSpan();
                if (gap.TotalHours < 5)
                {
                    return ApiResponse<EventDetailDto>.FailureResponse("Khoảng cách giữa các sự kiện cùng ngày phải tối thiểu 5 giờ");
                }
            }
            else if (endB <= startA)
            {
                var gap = startA.ToTimeSpan() - endB.ToTimeSpan();
                if (gap.TotalHours < 5)
                {
                    return ApiResponse<EventDetailDto>.FailureResponse("Khoảng cách giữa các sự kiện cùng ngày phải tối thiểu 5 giờ");
                }
            }
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
            Location = request.Location ?? hall.Address,
            HallId = request.HallId,
            OrganizerId = organizerId,
            ImageUrl = imageUrl,
            Status = "draft",
            TotalSeats = totalSeats,
            NumberOfRows = maxRows,
            SeatsPerRow = maxSeatsPerRow,
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

        // Add Speakers if provided
        if (request.SpeakerIds != null && request.SpeakerIds.Any())
        {
            foreach (var speakerId in request.SpeakerIds)
            {
                var eventSpeaker = new EventSpeaker
                {
                    EventId = eventEntity.EventId,
                    SpeakerId = speakerId,
                    DisplayOrder = request.SpeakerIds.IndexOf(speakerId),
                    CreatedAt = DateTime.UtcNow
                };
                eventEntity.EventSpeakers.Add(eventSpeaker);
            }
            await _eventRepository.SaveChangesAsync();
        }

        // Generate seats for the event using Hall configuration
        await GenerateSeatsForEventAsync(eventEntity.EventId, request.HallId, maxRows, maxSeatsPerRow);

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

        // Validate Rows * SeatsPerRow = TotalSeats
        if (request.Rows * request.SeatsPerRow != request.TotalSeats)
        {
            return ApiResponse<EventDetailDto>.FailureResponse($"Tổng số ghế phải bằng Số hàng × Số ghế mỗi hàng ({request.Rows} × {request.SeatsPerRow} = {request.Rows * request.SeatsPerRow})");
        }

        // Cannot reduce total seats below registered count
        if (request.TotalSeats < eventEntity.RegisteredCount)
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                $"Không thể giảm số ghế xuống dưới {eventEntity.RegisteredCount} (số người đã đăng ký)");
        }

        // Check if seat configuration changed
        bool seatConfigChanged = eventEntity.NumberOfRows != request.Rows || 
                                 eventEntity.SeatsPerRow != request.SeatsPerRow;

        if (!string.IsNullOrWhiteSpace(request.HallId))
        {
            var hall = await _hallRepository.GetByIdAsync(request.HallId!);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy hội trường");
            }

            // Validate Hall capacity limits
            if (request.Rows > hall.MaxRows)
            {
                return ApiResponse<EventDetailDto>.FailureResponse($"Số hàng ({request.Rows}) vượt quá giới hạn hội trường ({hall.MaxRows} hàng)");
            }

            if (request.SeatsPerRow > hall.MaxSeatsPerRow)
            {
                return ApiResponse<EventDetailDto>.FailureResponse($"Số ghế mỗi hàng ({request.SeatsPerRow}) vượt quá giới hạn hội trường ({hall.MaxSeatsPerRow} ghế/hàng)");
            }

            if (request.TotalSeats > hall.Capacity)
            {
                return ApiResponse<EventDetailDto>.FailureResponse($"Tổng số ghế ({request.TotalSeats}) vượt quá sức chứa hội trường ({hall.Capacity})");
            }
            var conflicts = await _hallRepository.GetConflictingEventsAsync(request.HallId!, request.Date, request.StartTime, request.EndTime);
            if (conflicts.Any(c => c.EventId != eventId))
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường đang có sự kiện trùng thời gian");
            }
            // Khoảng cách tối thiểu 5 giờ với các sự kiện khác cùng ngày
            var (sameDayEvents, _) = await _eventRepository.GetAllAsync(1, 1000, null, null, request.Date, request.Date, request.HallId, null);
            foreach (var e in sameDayEvents.Where(x => x.EventId != eventId))
            {
                var startA = request.StartTime;
                var endA = request.EndTime;
                var startB = e.StartTime;
                var endB = e.EndTime;
                if (endA <= startB)
                {
                    var gap = startB.ToTimeSpan() - endA.ToTimeSpan();
                    if (gap.TotalHours < 5)
                    {
                        return ApiResponse<EventDetailDto>.FailureResponse("Khoảng cách giữa các sự kiện cùng ngày phải tối thiểu 5 giờ");
                    }
                }
                else if (endB <= startA)
                {
                    var gap = startA.ToTimeSpan() - endB.ToTimeSpan();
                    if (gap.TotalHours < 5)
                    {
                        return ApiResponse<EventDetailDto>.FailureResponse("Khoảng cách giữa các sự kiện cùng ngày phải tối thiểu 5 giờ");
                    }
                }
            }
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
        eventEntity.NumberOfRows = request.Rows;
        eventEntity.SeatsPerRow = request.SeatsPerRow;
        eventEntity.Tags = request.Tags;
        eventEntity.MaxTicketsPerUser = request.MaxTicketsPerUser;
        eventEntity.RegistrationStart = request.RegistrationStart;
        eventEntity.RegistrationEnd = request.RegistrationEnd;

        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        // Regenerate seats if configuration changed and event has a hall
        if (seatConfigChanged && !string.IsNullOrWhiteSpace(request.HallId))
        {
            // Delete old seats for this event
            await _seatRepository.DeleteByEventIdAsync(eventId);
            await _seatRepository.SaveChangesAsync();

            // Generate new seats
            await GenerateSeatsForEventAsync(eventId, request.HallId!, request.Rows, request.SeatsPerRow);
        }

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


    public async Task<ApiResponse<EventDetailDto>> PublishEventAsync(string eventId, string currentUserId, string currentUserRole)
    {
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền công bố sự kiện này");
        }
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }
        if (eventEntity.Status == "cancelled" || eventEntity.Status == "completed")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không thể công bố sự kiện đã hủy hoặc đã hoàn thành");
        }
        var now = DateTime.UtcNow;
        if (eventEntity.RegistrationStart.HasValue && now < eventEntity.RegistrationStart.Value)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Chưa đến thời điểm publish (RegistrationStart)");
        }
        eventEntity.Status = "published";
        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();
        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Công bố sự kiện thành công");
    }

    public async Task<ApiResponse<EventDetailDto>> CancelEventAsync(string eventId, string currentUserId, string currentUserRole)
    {
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền hủy sự kiện này");
        }
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }
        var eventStart = new DateTime(eventEntity.Date.Year, eventEntity.Date.Month, eventEntity.Date.Day,
            eventEntity.StartTime.Hour, eventEntity.StartTime.Minute, eventEntity.StartTime.Second, DateTimeKind.Utc);
        var now = DateTime.UtcNow;
        if (now > eventStart.AddHours(-48))
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Chỉ được hủy trước thời điểm diễn ra 48 giờ");
        }
        if (eventEntity.TotalSeats > 0 && eventEntity.RegisteredCount > (eventEntity.TotalSeats / 2))
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không thể hủy khi số lượng đăng ký vượt quá 50% số ghế");
        }
        eventEntity.Status = "cancelled";
        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();
        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Hủy sự kiện thành công");
    }

    public async Task<ApiResponse<EventDetailDto>> CompleteEventAsync(string eventId, string currentUserId, string currentUserRole)
    {
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền đóng sự kiện này");
        }
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }
        eventEntity.Status = "completed";
        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();
        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Đóng sự kiện thành công");
    }

    // Helper methods
    private async Task<bool> ApplyAutoTransitionsAsync(Event e)
    {
        bool changed = false;
        var now = DateTime.UtcNow;

        DateTime eventStart = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day,
            e.StartTime.Hour, e.StartTime.Minute, e.StartTime.Second, DateTimeKind.Utc);
        DateTime eventEnd = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day,
            e.EndTime.Hour, e.EndTime.Minute, e.EndTime.Second, DateTimeKind.Utc);

        if ((e.Status == "draft" || e.Status == "pending") && e.RegistrationStart.HasValue && now >= e.RegistrationStart.Value)
        {
            e.Status = "published";
            changed = true;
        }

        if (e.Status != "cancelled" && e.Status != "completed" && now >= eventEnd.AddHours(5))
        {
            e.Status = "completed";
            changed = true;
        }

        if (changed)
        {
            await _eventRepository.UpdateAsync(e);
            await _eventRepository.SaveChangesAsync();
        }
        return changed;
    }

    public async Task<ApiResponse<List<SeatDto>>> GetEventAvailableSeatsAsync(string eventId)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev == null || string.IsNullOrWhiteSpace(ev.HallId))
        {
            return ApiResponse<List<SeatDto>>.FailureResponse("Sự kiện không có hội trường");
        }
        var seats = await _seatRepository.GetByEventIdAsync(eventId);
        var available = seats.Where(s => s.Status == "available").Select(s => new SeatDto
        {
            SeatId = s.SeatId,
            SeatNumber = s.SeatNumber,
            RowLabel = s.RowLabel,
            Status = s.Status
        }).ToList();
        return ApiResponse<List<SeatDto>>.SuccessResponse(available, "Lấy danh sách ghế trống theo sự kiện thành công");
    }
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
            UpdatedAt = e.UpdatedAt,
            Speakers = e.EventSpeakers?.Select(es => new SpeakerSimpleDto
            {
                SpeakerId = es.Speaker?.SpeakerId ?? string.Empty,
                Name = es.Speaker?.Name ?? string.Empty,
                Title = es.Speaker?.Title,
                Organization = es.Speaker?.Company,
                ImageUrl = es.Speaker?.AvatarUrl
            }).ToList()
        };
    }

    private async Task GenerateSeatsForEventAsync(string eventId, string hallId, int numberOfRows, int seatsPerRow)
    {
        var seats = new List<Seat>();

        for (int row = 1; row <= numberOfRows; row++)
        {
            // Generate row label: A, B, C, ..., Z, AA, AB, ...
            string rowLabel = GetRowLabel(row);

            for (int seatNum = 1; seatNum <= seatsPerRow; seatNum++)
            {
                seats.Add(new Seat
                {
                    SeatId = Guid.NewGuid().ToString(),
                    HallId = hallId,
                    EventId = eventId,
                    RowLabel = rowLabel,
                    SeatNumber = $"{rowLabel}{seatNum}",
                    Status = "available",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _seatRepository.AddRangeAsync(seats);
        await _seatRepository.SaveChangesAsync();
    }

    private string GetRowLabel(int rowNumber)
    {
        string label = "";
        while (rowNumber > 0)
        {
            rowNumber--;
            label = (char)('A' + (rowNumber % 26)) + label;
            rowNumber /= 26;
        }
        return label;
    }
}

