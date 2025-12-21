using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.DTOs.Response.Hall;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntervalEventRegistrationService.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IHallRepository _hallRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketCheckinRepository _ticketCheckinRepository;
    private readonly ILogger<EventService> _logger;
    private readonly ISpeakerRepository _speakerRepository;

    public EventService(
        
        IEventRepository eventRepository, 
       
        ICloudinaryService cloudinaryService, 
       
        IHallRepository hallRepository, 
       
        ISeatRepository seatRepository,
        ISpeakerRepository speakerRepository,
        ITicketRepository ticketRepository,
        ITicketCheckinRepository ticketCheckinRepository,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _cloudinaryService = cloudinaryService;
        _hallRepository = hallRepository;
        _seatRepository = seatRepository;
        _ticketRepository = ticketRepository;
        _ticketCheckinRepository = ticketCheckinRepository;
        _logger = logger;
        _speakerRepository = speakerRepository;
    }

    public async Task<PagedResponse<EventListItemDto>> GetAllEventsAsync(
        EventFilterRequest request, 
        string? currentUserId = null, 
        string? currentUserRole = null)
    {
        // Với sinh viên, chỉ hiển thị các sự kiện đã công bố
        // Organizer có thể xem tất cả sự kiện
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

        // Kiểm tra quyền cho các sự kiện draft/pending/rejected
        // Chỉ organizer hoặc chủ sự kiện mới được xem
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
        // Kiểm tra thời gian
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Thời gian kết thúc phải sau thời gian bắt đầu");
        }

        if (request.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Ngày sự kiện không được ở quá khứ");
        }

        var hasHall = !string.IsNullOrWhiteSpace(request.HallId);

        int totalSeats = 0;
        int maxRows = 0;
        int maxSeatsPerRow = 0;
        Hall? hall = null;

        if (hasHall)
        {
            // Kiểm tra Hall tồn tại và lấy cấu hình
            hall = await _hallRepository.GetByIdAsync(request.HallId!);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy hội trường");
            }

            // Kiểm tra nếu hall có ghế không
            var hallSeats = await _seatRepository.GetByHallIdAsync(request.HallId!);
            if (!hallSeats.Any())
            {
                return ApiResponse<EventDetailDto>.FailureResponse(
                    "Hội trường chưa có ghế. Vui lòng tạo ghế cho hội trường trước khi tạo sự kiện.");
            }

            // tự động lấy cấu hình hội trường
            totalSeats = hallSeats.Count; // sử dụng số ghế thực tế từ hội trường
            maxRows = hall.MaxRows;
            maxSeatsPerRow = hall.MaxSeatsPerRow;

            // Kiểm tra sức chứa và cấu hình hội trường để tránh lỗi
            if (totalSeats <= 0)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường không có sức chứa hợp lệ (Capacity phải > 0)");
            }
            if (maxRows <= 0)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường không có cấu hình hàng ghế hợp lệ (MaxRows phải > 0)");
            }
            if (maxSeatsPerRow <= 0)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường không có cấu hình ghế mỗi hàng hợp lệ (MaxSeatsPerRow phải > 0)");
            }

            // Kiểm tra hội trường còn trống không
            var hallAvailable = await _hallRepository.IsHallAvailableAsync(request.HallId!, request.Date, request.StartTime, request.EndTime);
            if (!hallAvailable)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường đang có sự kiện trùng thời gian");
            }

            // Kiểm tra yêu cầu khoảng cách 5 giờ giữa các sự kiện
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
        }
        else
        {
            // Không có HallId: yêu cầu Location để có địa điểm hiển thị
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Vui lòng nhập Location nếu không chọn Hall");
            }
        }

        // Chuẩn hóa thời gian đăng ký về UTC (tránh Kind=Unspecified cho PostgreSQL)
        DateTime? regStartUtc = null;
        DateTime? regEndUtc = null;
        if (request.RegistrationStart.HasValue)
        {
            regStartUtc = DateTime.SpecifyKind(request.RegistrationStart.Value, DateTimeKind.Utc);
        }
        if (request.RegistrationEnd.HasValue)
        {
            regEndUtc = DateTime.SpecifyKind(request.RegistrationEnd.Value, DateTimeKind.Utc);
        }

        // Upload ảnh nếu có
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
            Location = request.Location ?? hall?.Address,
            HallId = hasHall ? request.HallId : null,
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
            RegistrationStart = regStartUtc,
            RegistrationEnd = regEndUtc,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        // Thêm Speakers nếu có - kiểm tra tồn tại trước để tránh lỗi FK
        if (request.SpeakerIds != null && request.SpeakerIds.Any())
        {
            foreach (var speakerId in request.SpeakerIds)
            {
                if (string.IsNullOrWhiteSpace(speakerId))
                {
                    return ApiResponse<EventDetailDto>.FailureResponse("SpeakerId không được để trống");
                }

                var exists = await _speakerRepository.ExistsAsync(speakerId);
                if (!exists)
                {
                    return ApiResponse<EventDetailDto>.FailureResponse($"Không tìm thấy Speaker với ID: {speakerId}");
                }
            }

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

        // ❌ ĐÃ XÓA: KHÔNG tạo ghế cho event - sử dụng ghế của Hall
        // Ghế thuộc về Hall, không thuộc Event. Event chỉ tham chiếu HallId.

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

        // Kiểm tra quyền - Chỉ organizer mới được cập nhật
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Bạn không có quyền chỉnh sửa sự kiện này");
        }

        // ✨ MỚI: Kiểm tra trạng thái sự kiện - Không thể cập nhật completed/cancelled/ongoing
        if (eventEntity.Status == "completed")
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                "Không thể chỉnh sửa sự kiện đã hoàn thành");
        }

        if (eventEntity.Status == "cancelled")
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                "Không thể chỉnh sửa sự kiện đã bị hủy");
        }

        if (eventEntity.Status == "ongoing")
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                "Không thể chỉnh sửa sự kiện đang diễn ra");
        }

        // ✨ Chỉ cho phép cập nhật sự kiện draft/published
        if (eventEntity.Status != "draft" && eventEntity.Status != "published")
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                $"Không thể chỉnh sửa sự kiện ở trạng thái '{eventEntity.Status}'");
        }

        // Kiểm tra thời gian
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Thời gian kết thúc phải sau thời gian bắt đầu");
        }

        // Lấy cấu hình hội trường (tương tự CreateEvent)
        bool hasHall = !string.IsNullOrWhiteSpace(request.HallId);
        Hall? hall = null;
        int totalSeats = eventEntity.TotalSeats; // Keep existing if no hall change
        int maxRows = eventEntity.NumberOfRows;
        int maxSeatsPerRow = eventEntity.SeatsPerRow;
        bool seatConfigChanged = false;

        if (hasHall)
        {
            hall = await _hallRepository.GetByIdAsync(request.HallId!);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy hội trường");
            }

            // ✅ Kiểm tra hall có ghế không
            var hallSeats = await _seatRepository.GetByHallIdAsync(request.HallId!);
            if (!hallSeats.Any())
            {
                return ApiResponse<EventDetailDto>.FailureResponse(
                    "Hội trường chưa có ghế. Vui lòng tạo ghế cho hội trường trước.");
            }

            // Tự động lấy cấu hình hội trường
            totalSeats = hallSeats.Count; // ✅ Use actual seat count from hall
            maxRows = hall.MaxRows;
            maxSeatsPerRow = hall.MaxSeatsPerRow;

            // Kiểm tra cấu hình hội trường
            if (totalSeats <= 0 || maxRows <= 0 || maxSeatsPerRow <= 0)
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường không có cấu hình hợp lệ");
            }

            // Không được giảm số ghế xuống dưới số người đã đăng ký
            if (totalSeats < eventEntity.RegisteredCount)
            {
                return ApiResponse<EventDetailDto>.FailureResponse(
                    $"Hội trường mới chỉ có {totalSeats} ghế, không đủ cho {eventEntity.RegisteredCount} người đã đăng ký");
            }

            // ✅ Kiểm tra nếu hall thay đổi
            seatConfigChanged = eventEntity.HallId != request.HallId;

            // Kiểm tra hội trường còn trống (loại trừ event hiện tại)
            var conflicts = await _hallRepository.GetConflictingEventsAsync(request.HallId!, request.Date, request.StartTime, request.EndTime);
            if (conflicts.Any(c => c.EventId != eventId))
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Hội trường đang có sự kiện trùng thời gian");
            }

            // Kiểm tra yêu cầu khoảng cách 5 giờ
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
        else
        {
            // Không có HallId: yêu cầu Location
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return ApiResponse<EventDetailDto>.FailureResponse("Vui lòng nhập Location nếu không chọn Hall");
            }
        }

        // Chuẩn hóa thời gian đăng ký về UTC
        DateTime? regStartUtc = null;
        DateTime? regEndUtc = null;
        if (request.RegistrationStart.HasValue)
        {
            regStartUtc = DateTime.SpecifyKind(request.RegistrationStart.Value, DateTimeKind.Utc);
        }
        if (request.RegistrationEnd.HasValue)
        {
            regEndUtc = DateTime.SpecifyKind(request.RegistrationEnd.Value, DateTimeKind.Utc);
        }

        // Upload ảnh mới nếu có
        if (request.ImageFile != null)
        {
            eventEntity.ImageUrl = await _cloudinaryService.UploadAsync(request.ImageFile, "events");
        }

        // Cập nhật các trường
        eventEntity.Title = request.Title;
        eventEntity.Description = request.Description;
        eventEntity.Date = request.Date;
        eventEntity.StartTime = request.StartTime;
        eventEntity.EndTime = request.EndTime;
        
        // ✅ SỬa: Khi chọn hall, luôn sử dụng địa chỉ của hall
        // Khi thay đổi hall hoặc có hall, ưu tiên địa chỉ hall hơn request.Location
        if (hasHall && hall != null)
        {
            eventEntity.Location = hall.Address; // Always use hall's address when hall is selected
        }
        else
        {
            eventEntity.Location = request.Location; // Use custom location when no hall
        }
        
        eventEntity.HallId = hasHall ? request.HallId : null;
        eventEntity.TotalSeats = totalSeats;
        eventEntity.NumberOfRows = maxRows;
        eventEntity.SeatsPerRow = maxSeatsPerRow;
        eventEntity.Tags = request.Tags;
        eventEntity.MaxTicketsPerUser = request.MaxTicketsPerUser;
        eventEntity.RegistrationStart = regStartUtc;
        eventEntity.RegistrationEnd = regEndUtc;

        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();

        // ❌ ĐÃ XÓA: KHÔNG tạo lại ghế - sử dụng ghế của Hall
        // Khi thay đổi hall, chỉ cần tham chiếu đến ghế hiện có của hall mới.
        // Không cần tạo ghế mới cho event.

        // Cập nhật speakers nếu có
        if (request.SpeakerIds != null && request.SpeakerIds.Any())
        {
            // Kiểm tra speakers tồn tại
            foreach (var speakerId in request.SpeakerIds.Distinct())
            {
                if (string.IsNullOrWhiteSpace(speakerId))
                {
                    return ApiResponse<EventDetailDto>.FailureResponse("SpeakerId không được để trống");
                }

                var exists = await _speakerRepository.ExistsAsync(speakerId);
                if (!exists)
                {
                    return ApiResponse<EventDetailDto>.FailureResponse($"Không tìm thấy Speaker với ID: {speakerId}");
                }
            }

            // Xóa speakers cũ
            eventEntity.EventSpeakers.Clear();

            // Thêm speakers mới
            foreach (var speakerId in request.SpeakerIds.Distinct())
            {
                var eventSpeaker = new EventSpeaker
                {
                    EventId = eventId,
                    SpeakerId = speakerId,
                    DisplayOrder = request.SpeakerIds.IndexOf(speakerId),
                    CreatedAt = DateTime.UtcNow
                };
                eventEntity.EventSpeakers.Add(eventSpeaker);
            }
            await _eventRepository.SaveChangesAsync();
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

        // Chỉ organizer mới được xóa
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
        
        if (eventEntity.Status == "cancelled")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Sự kiện đã bị hủy trước đó");
        }
        
        if (eventEntity.Status == "completed")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không thể hủy sự kiện đã hoàn thành");
        }
        
        // ✅ Kiểm tra thời gian hủy (48 giờ trước sự kiện)
        var eventStartUtc = new DateTime(
            eventEntity.Date.Year, eventEntity.Date.Month, eventEntity.Date.Day,
            eventEntity.StartTime.Hour, eventEntity.StartTime.Minute, eventEntity.StartTime.Second, 
            DateTimeKind.Utc
        );
        var now = DateTime.UtcNow;
        
        if (now > eventStartUtc.AddHours(-48))
        {
            var hoursUntilEvent = (eventStartUtc - now).TotalHours;
            return ApiResponse<EventDetailDto>.FailureResponse(
                $"Chỉ được hủy trước thời điểm diễn ra 48 giờ. Còn {hoursUntilEvent:F1} giờ nữa đến sự kiện."
            );
        }
        
        // ✅ Kiểm tra ngưỡng đăng ký (tối đa 50% đã đăng ký)
        if (eventEntity.TotalSeats > 0 && eventEntity.RegisteredCount > (eventEntity.TotalSeats / 2))
        {
            return ApiResponse<EventDetailDto>.FailureResponse(
                $"Không thể hủy khi số lượng đăng ký ({eventEntity.RegisteredCount}/{eventEntity.TotalSeats}) vượt quá 50% số ghế"
            );
        }
        
        // ✅ Hủy trạng thái sự kiện
        eventEntity.Status = "cancelled";

        // ✅ Hủy tất cả vé và trả ghế
        var tickets = await _ticketRepository.GetByEventIdAsync(eventId);
        int cancelledTicketsCount = 0;
        int returnedSeatsCount = 0;

        foreach (var ticket in tickets.Where(t => t.Status == "active" || t.Status == "checked-in"))
        {
            ticket.Status = "cancelled";
            ticket.CancelledAt = DateTime.UtcNow;
            ticket.CancelReason = "Event cancelled by organizer";
            await _ticketRepository.UpdateAsync(ticket);
            cancelledTicketsCount++;

            // ✅ Trả ghế về trạng thái trống
            if (!string.IsNullOrWhiteSpace(ticket.SeatId))
            {
                var seat = await _seatRepository.GetByIdAsync(ticket.SeatId);
                if (seat != null && (seat.Status == "reserved" || seat.Status == "occupied"))
                {
                    seat.Status = "available";
                    await _seatRepository.UpdateAsync(seat);
                    returnedSeatsCount++;
                }
            }
        }

        // ✅ Reset số lượng sự kiện
        eventEntity.RegisteredCount = 0;
        eventEntity.CheckedInCount = 0;

        await _eventRepository.UpdateAsync(eventEntity);
        await _ticketRepository.SaveChangesAsync();
        await _eventRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Cancelled event {EventId}: {CancelledTickets} tickets cancelled",
            eventId, cancelledTicketsCount);

        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(
            dto, 
            $"Hủy sự kiện thành công. Đã hủy {cancelledTicketsCount} vé."
        );
    }

    public async Task<ApiResponse<EventDetailDto>> CompleteEventAsync(string eventId, string currentUserId, string currentUserRole)
    {
        if (currentUserRole != "organizer")
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Đáng không có quyền đóng sự kiện này");
        }
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            return ApiResponse<EventDetailDto>.FailureResponse("Không tìm thấy sự kiện");
        }
        
        eventEntity.Status = "completed";
        await _eventRepository.UpdateAsync(eventEntity);
        await _eventRepository.SaveChangesAsync();
        
        // ❌ ĐÃ XÓA: Không cần reset seat.Status (trạng thái được tính động theo từng event)
        
        var dto = MapToDetailDto(eventEntity);
        return ApiResponse<EventDetailDto>.SuccessResponse(dto, "Đóng sự kiện thành công");
    }

    // Các hàm helper
    private async Task<bool> ApplyAutoTransitionsAsync(Event e)
    {
        bool changed = false;
        var now = DateTime.UtcNow;

        DateTime eventStart = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day,
            e.StartTime.Hour, e.StartTime.Minute, e.StartTime.Second, DateTimeKind.Utc);
        DateTime eventEnd = new DateTime(e.Date.Year, e.Date.Month, e.Date.Day,
            e.EndTime.Hour, e.EndTime.Minute, e.EndTime.Second, DateTimeKind.Utc);

        // 1. Tự động công bố khi bắt đầu đăng ký
        if ((e.Status == "draft" || e.Status == "pending") && 
            e.RegistrationStart.HasValue && 
            now >= e.RegistrationStart.Value)
        {
            e.Status = "published";
            changed = true;
        }

        // 2. ✨ MỚI: Tự động chuyển sang "ongoing" khi sự kiện bắt đầu
        if (e.Status == "published" && now >= eventStart)
        {
            e.Status = "ongoing";
            changed = true;
        }

        // 3. ✨ MỚI: Tự động hoàn thành và đánh dấu vé no-show/abandoned
        if ((e.Status == "published" || e.Status == "ongoing") && 
            e.Status != "cancelled" && 
            e.Status != "completed" && 
            now >= eventEnd.AddHours(5))
        {
            e.Status = "completed";
            
            var tickets = await _ticketRepository.GetByEventIdAsync(e.EventId);
            
            // ✨ Đánh dấu vé "no-show" nếu chưa check-in
            foreach (var ticket in tickets.Where(t => t.Status == "active"))
            {
                ticket.Status = "no-show"; // Đã đăng ký nhưng không check-in
                await _ticketRepository.UpdateAsync(ticket);
            }
            
            // ✨ Đánh dấu vé "abandoned" nếu đã check-in nhưng KHÔNG check-out
            foreach (var ticket in tickets.Where(t => t.Status == "checked-in"))
            {
                ticket.Status = "abandoned"; // Check-in nhưng không check-out
                await _ticketRepository.UpdateAsync(ticket);
            }
            
            // ❌ ĐÃ XÓA: Không cần reset seat.Status (trạng thái được tính động theo từng event)
            
            // Lưu ý: Vé với status "completed" đã check-out rồi - giữ nguyên
            
            changed = true;
        }

        if (changed)
        {
            await _eventRepository.UpdateAsync(e);
            await _eventRepository.SaveChangesAsync();
        }
        return changed;
    }

    /// <summary>
    /// Lấy danh sách ghế của sự kiện với trạng thái tính toán động từ tickets.
    /// Ghế thuộc về Hall, trạng thái được tính dựa trên tickets của event cụ thể.
    /// </summary>
    public async Task<ApiResponse<List<SeatDto>>> GetEventAvailableSeatsAsync(string eventId)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev == null || string.IsNullOrWhiteSpace(ev.HallId))
        {
            return ApiResponse<List<SeatDto>>.FailureResponse("Sự kiện không có hội trường");
        }
        
        // Lấy tất cả ghế từ Hall
        var seats = await _seatRepository.GetByHallIdAsync(ev.HallId);
        
        // Lấy tất cả tickets của event này để tính toán trạng thái ghế động
        var tickets = await _ticketRepository.GetByEventIdAsync(eventId);
        var seatTickets = tickets
            .Where(t => t.SeatId != null && t.Status != "cancelled")
            .GroupBy(t => t.SeatId!)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(t => t.RegisteredAt).FirstOrDefault()
            );
        
        // Tính toán trạng thái động cho từng ghế dựa trên tickets của event
        var seatDtos = seats.Select(s => new SeatDto
        {
            SeatId = s.SeatId,
            SeatNumber = s.SeatNumber,
            RowLabel = s.RowLabel,
            Status = CalculateSeatStatusForEvent(s.SeatId, seatTickets)
        }).OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).ToList();
        
        var availableCount = seatDtos.Count(s => s.Status == "available");
        var reservedCount = seatDtos.Count(s => s.Status == "reserved");
        var occupiedCount = seatDtos.Count(s => s.Status == "occupied");
        
        return ApiResponse<List<SeatDto>>.SuccessResponse(
            seatDtos, 
            $"Lấy danh sách ghế thành công (Tổng: {seatDtos.Count}, Trống: {availableCount}, Đã đặt: {reservedCount}, Đang sử dụng: {occupiedCount})"
        );
    }
    
    /// <summary>
    /// Tính toán trạng thái ghế động dựa trên tickets của event.
    /// - available: Không có ticket nào cho ghế này trong event
    /// - reserved: Có ticket active nhưng chưa check-in
    /// - occupied: Ticket đã check-in
    /// </summary>
    private string CalculateSeatStatusForEvent(
        string seatId, 
        Dictionary<string, Ticket?> seatTickets)
    {
        if (!seatTickets.TryGetValue(seatId, out var ticket) || ticket == null)
        {
            return "available"; // Không có ticket → ghế trống
        }

        // Đã check-in → đang sử dụng
        if (ticket.CheckInTime.HasValue)
        {
            return "occupied";
        }

        // Có ticket active/registered/confirmed nhưng chưa check-in → đã đặt
        if (ticket.Status == "active" || ticket.Status == "registered" || ticket.Status == "confirmed")
        {
            return "reserved";
        }

        return "available"; // Mặc định là trống
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

    /// <summary>
    /// ❌ ĐÃ LỖI THỜI: Phương thức này KHÔNG nên sử dụng nữa.
    /// Ghế thuộc về Hall, không thuộc Event. Events nên tham chiếu ghế hiện có của Hall.
    /// Tạo ghế cho mỗi event gây ra trùng lặp ghế cho cùng một hall.
    /// </summary>
    [Obsolete("Đừng sử dụng. Ghế chỉ nên được tạo cho Hall, không phải cho mỗi Event.")]
    private async Task GenerateSeatsForEventAsync(string eventId, string hallId, int numberOfRows, int seatsPerRow)
    {
        // Phương thức này được giữ lại cho tương thích ngược nhưng không nên gọi
        throw new InvalidOperationException(
            "GenerateSeatsForEventAsync đã lỗi thời. Ghế chỉ nên được tạo cho Hall, không phải Event.");
    }

    public async Task<ApiResponse<EventStatisticsDto>> GetEventStatisticsAsync(string eventId)
    {
        try
        {
            _logger.LogInformation("Lấy thống kê cho sự kiện: {EventId}", eventId);

            // Lấy event với các quan hệ
            var eventEntity = await _eventRepository.GetByIdAsync(eventId, includeRelations: true);
            
            if (eventEntity == null)
            {
                _logger.LogWarning("Không tìm thấy sự kiện: {EventId}", eventId);
                return ApiResponse<EventStatisticsDto>.FailureResponse("Không tìm thấy sự kiện");
            }

            // Lấy tất cả vé của sự kiện
            var allTickets = await _ticketRepository.GetByEventIdAsync(eventId);
            
            // Lấy các bản ghi check-in
            var checkIns = await _ticketCheckinRepository.GetByEventIdAsync(eventId);
            
            _logger.LogInformation("Event {EventId}: {TicketCount} tickets, {CheckInCount} check-ins", 
                eventId, allTickets.Count, checkIns.Count);

            // Tính toán thống kê
            var registeredCount = allTickets.Count(t => 
                t.Status == "active" || 
                t.Status == "checked-in" || 
                t.Status == "completed" || 
                t.Status == "abandoned" || // ✨ Check-in nhưng không check-out
                t.Status == "no-show");
            var checkedInCount = checkIns.Count;
            var checkedOutCount = allTickets.Count(t => t.Status == "completed");
            var stillInVenueCount = allTickets.Count(t => t.Status == "checked-in"); // Đang tham dự
            var abandonedCount = allTickets.Count(t => t.Status == "abandoned"); // ✨ Check-in nhưng không check-out
            
            var checkInRate = registeredCount > 0 
                ? Math.Round((double)checkedInCount / registeredCount * 100, 1) 
                : 0;
            
            var checkOutRate = checkedInCount > 0
                ? Math.Round((double)checkedOutCount / checkedInCount * 100, 1)
                : 0;

            // Tính toán thống kê thời gian tham dự
            var completedCheckIns = checkIns.Where(c => c.CheckoutTime != null).ToList();
            TimeSpan? avgDuration = null;
            TimeSpan? minDuration = null;
            TimeSpan? maxDuration = null;

            if (completedCheckIns.Any())
            {
                var durations = completedCheckIns
                    .Select(c => c.CheckoutTime!.Value - c.CheckinTime)
                    .ToList();

                avgDuration = TimeSpan.FromSeconds(durations.Average(d => d.TotalSeconds));
                minDuration = durations.Min();
                maxDuration = durations.Max();
            }

            // Lấy 10 check-in gần nhất với thông tin checkout
            var recentCheckIns = checkIns
                .OrderByDescending(c => c.CheckinTime)
                .Take(10)
                .Select(c =>
                {
                    var ticket = allTickets.FirstOrDefault(t => t.TicketId == c.TicketId);
                    var seat = ticket?.Seat;
                    var student = ticket?.Student;
                    
                    // Tính thời lượng nếu đã checkout
                    TimeSpan? duration = c.CheckoutTime.HasValue 
                        ? c.CheckoutTime.Value - c.CheckinTime 
                        : null;
                    
                    // Lấy trạng thái hiển thị
                    string statusDisplay = c.CheckoutTime.HasValue 
                        ? "Đã check-out" 
                        : "Đang tham dự";
                    
                    return new RecentCheckInDto
                    {
                        AttendeeName = student?.Name ?? "Unknown",
                        TicketCode = ticket?.TicketCode ?? "N/A",
                        SeatNumber = seat?.SeatNumber ?? "-",
                        CheckInTime = c.CheckinTime,
                        CheckOutTime = c.CheckoutTime,
                        Duration = duration,
                        Status = statusDisplay
                    };
                })
                .ToList();

            // Map speakers
            var speakers = eventEntity.EventSpeakers?
                .Select(es => new IntervalEventRegistrationService.DTOs.Response.SpeakerSimpleDto
                {
                    SpeakerId = es.Speaker?.SpeakerId ?? string.Empty,
                    Name = es.Speaker?.Name ?? string.Empty,
                    Title = es.Speaker?.Title,
                    Organization = es.Speaker?.Company,
                    ImageUrl = es.Speaker?.AvatarUrl
                })
                .ToList();

            var statistics = new EventStatisticsDto
            {
                EventId = eventEntity.EventId,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Date = eventEntity.Date,
                StartTime = eventEntity.StartTime,
                EndTime = eventEntity.EndTime,
                Location = eventEntity.Location,
                ImageUrl = eventEntity.ImageUrl,
                Status = eventEntity.Status,
                TotalSeats = eventEntity.TotalSeats,
                RegisteredCount = registeredCount,
                CheckedInCount = checkedInCount,
                CheckedOutCount = checkedOutCount,
                StillInVenueCount = stillInVenueCount,
                CheckInRate = checkInRate,
                CheckOutRate = checkOutRate,
                AverageAttendanceDuration = avgDuration,
                MinAttendanceDuration = minDuration,
                MaxAttendanceDuration = maxDuration,
                Speakers = speakers,
                RecentCheckIns = recentCheckIns
            };

            _logger.LogInformation(
                "Statistics for event {EventId}: CheckIn={CheckInRate}%, CheckOut={CheckOutRate}%, StillInVenue={StillInVenue}", 
                eventId, checkInRate, checkOutRate, stillInVenueCount);

            return ApiResponse<EventStatisticsDto>.SuccessResponse(
                statistics, 
                "Lấy thống kê sự kiện thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event statistics for {EventId}", eventId);
            return ApiResponse<EventStatisticsDto>.FailureResponse(
                $"Lỗi khi lấy thống kê: {ex.Message}");
        }
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

