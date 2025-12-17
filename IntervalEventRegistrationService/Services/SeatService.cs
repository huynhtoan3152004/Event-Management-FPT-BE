using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Seat;
using IntervalEventRegistrationService.DTOs.Response.Seat;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.Extensions.Logging;

namespace IntervalEventRegistrationService.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepo;
    private readonly IHallRepository _hallRepo;
    private readonly IEventRepository _eventRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ILogger<SeatService> _logger;

    public SeatService(
        ISeatRepository seatRepo,
        IHallRepository hallRepo,
        IEventRepository eventRepo,
        ITicketRepository ticketRepo,
        ILogger<SeatService> logger)
    {
        _seatRepo = seatRepo;
        _hallRepo = hallRepo;
        _eventRepo = eventRepo;
        _ticketRepo = ticketRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<HallSeatMapDto>> GetHallSeatMapAsync(string hallId)
    {
        try
        {
            _logger.LogInformation("Getting seat map for hall: {HallId}", hallId);

            var hall = await _hallRepo.GetByIdAsync(hallId);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<HallSeatMapDto>.FailureResponse(
                    "Hội trường không tồn tại."
                );
            }

            var seats = await _seatRepo.GetSeatsByHallIdAsync(hallId);
            var seatsList = seats.ToList();

            // Group seats by row label
            var groupedSeats = seatsList
                .GroupBy(s => s.RowLabel)
                .OrderBy(g => g.Key)
                .ToList();

            var rows = groupedSeats.Select(group => new HallSeatRowDto
            {
                RowNumber = GetRowNumberFromLabel(group.Key ?? ""),
                RowLabel = group.Key ?? "",
                Seats = group.OrderBy(s => s.SeatNumber).Select(seat => new HallSeatItemDto
                {
                    SeatId = seat.SeatId,
                    RowNumber = GetRowNumberFromLabel(seat.RowLabel ?? ""),
                    SeatNumber = int.TryParse(seat.SeatNumber, out int seatNum) ? seatNum : 0,
                    Label = seat.SeatNumber,
                    IsDeleted = seat.IsDeleted
                }).ToList()
            }).ToList();

            var result = new HallSeatMapDto
            {
                HallId = hall.HallId,
                HallName = hall.Name,
                Location = hall.Address ?? "",
                Capacity = hall.Capacity,
                MaxRows = hall.MaxRows,
                MaxSeatsPerRow = hall.MaxSeatsPerRow,
                TotalSeatsGenerated = seatsList.Count,
                Rows = rows
            };

            return ApiResponse<HallSeatMapDto>.SuccessResponse(
                result,
                "Lấy sơ đồ ghế hội trường thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hall seat map for: {HallId}", hallId);
            return ApiResponse<HallSeatMapDto>.FailureResponse(
                "Đã xảy ra lỗi khi lấy sơ đồ ghế."
            );
        }
    }

    public async Task<ApiResponse<SeatMapDto>> GetEventSeatMapAsync(
        string eventId,
        string? userId = null,
        string? userRole = null,
        SeatMapFilterRequest? filter = null)
    {
        try
        {
            _logger.LogInformation(
                "Getting seat map for event: {EventId}, User: {UserId}, Role: {Role}",
                eventId, userId, userRole
            );

            // Validate event
            var eventData = await _eventRepo.GetByIdAsync(eventId, includeRelations: true);
            if (eventData == null || eventData.IsDeleted)
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Sự kiện không tồn tại."
                );
            }

            // Check permissions for detailed view
            bool includeOccupantDetails = filter?.IncludeOccupantDetails ?? false;
            if (includeOccupantDetails)
            {
                if (userRole != "organizer" && userRole != "staff")
                {
                    return ApiResponse<SeatMapDto>.FailureResponse(
                        "Bạn không có quyền xem thông tin chi tiết người đặt ghế."
                    );
                }

                // Organizer must own the event
                if (userRole == "organizer" && eventData.OrganizerId != userId)
                {
                    return ApiResponse<SeatMapDto>.FailureResponse(
                        "Bạn chỉ có thể xem thông tin chi tiết sự kiện của mình."
                    );
                }
            }

            // Get seats with optional occupant info
            // ✅ FIX: Get seats from Hall instead of querying by EventId (since seats belong to Hall, not Event)
            var seats = eventData.HallId != null
                ? await _seatRepo.GetSeatsByHallIdAsync(eventData.HallId)
                : new List<IntervalEventRegistrationRepo.Entities.Seat>();
            var seatsList = seats.ToList();

            // Apply filters
            if (filter?.Statuses != null && filter.Statuses.Any())
            {
                seatsList = seatsList
                    .Where(s => filter.Statuses.Contains(s.Status.ToLower()))
                    .ToList();
            }

            if (filter?.RowNumbers != null && filter.RowNumbers.Any())
            {
                seatsList = seatsList
                    .Where(s => filter.RowNumbers.Contains(GetRowNumberFromLabel(s.RowLabel ?? "")))
                    .ToList();
            }

            // Get statistics
            // ✅ FIX: Get statistics from Hall instead of querying by EventId
            var statistics = eventData.HallId != null
                ? await GetSeatStatisticsByHallAsync(eventData.HallId)
                : new Dictionary<string, int> { ["total"] = 0, ["available"] = 0, ["reserved"] = 0, ["occupied"] = 0 };

            // Get tickets for this event to populate occupant info
            Dictionary<string, IntervalEventRegistrationRepo.Entities.Ticket?> seatTickets = new();
            if (includeOccupantDetails)
            {
                var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
                seatTickets = tickets
                    .Where(t => t.SeatId != null && t.Status != "cancelled")
                    .GroupBy(t => t.SeatId!)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(t => t.RegisteredAt).FirstOrDefault()
                    );
            }

            // Group seats by row label
            var groupedSeats = seatsList
                .GroupBy(s => s.RowLabel)
                .OrderBy(g => g.Key)
                .ToList();

            var rows = groupedSeats.Select(group => new SeatRowDto
            {
                RowNumber = GetRowNumberFromLabel(group.Key ?? ""),
                RowLabel = group.Key ?? "",
                Seats = group.OrderBy(s => s.SeatNumber).Select(seat => 
                    MapToSeatItemDto(seat, includeOccupantDetails, seatTickets)
                ).ToList()
            }).ToList();

            var hall = eventData.HallId != null 
                ? await _hallRepo.GetByIdAsync(eventData.HallId) 
                : null;

            var result = new SeatMapDto
            {
                EventId = eventData.EventId,
                HallId = eventData.HallId,
                HallName = hall?.Name ?? "External Event",
                TotalRows = groupedSeats.Count,
                MaxSeatsPerRow = groupedSeats.Any() 
                    ? groupedSeats.Max(g => g.Count()) 
                    : 0,
                TotalSeats = statistics.GetValueOrDefault("total", 0),
                AvailableSeats = statistics.GetValueOrDefault("available", 0),
                ReservedSeats = statistics.GetValueOrDefault("reserved", 0),
                OccupiedSeats = statistics.GetValueOrDefault("occupied", 0),
                Rows = rows
            };

            return ApiResponse<SeatMapDto>.SuccessResponse(
                result,
                "Lấy sơ đồ ghế sự kiện thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event seat map for: {EventId}", eventId);
            return ApiResponse<SeatMapDto>.FailureResponse(
                "Đã xảy ra lỗi khi lấy sơ đồ ghế."
            );
        }
    }

    public async Task<ApiResponse<SeatMapDto>> GetAvailableSeatsForRegistrationAsync(string eventId)
    {
        try
        {
            _logger.LogInformation("Getting available seats for event: {EventId}", eventId);

            // Validate event
            var eventData = await _eventRepo.GetByIdAsync(eventId);
            if (eventData == null || eventData.IsDeleted)
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Sự kiện không tồn tại."
                );
            }

            // Check event status
            if (eventData.Status != "published")
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Sự kiện chưa mở đăng ký."
                );
            }

            // Check registration time window
            var now = DateTime.UtcNow;
            if (eventData.RegistrationStart.HasValue && now < eventData.RegistrationStart.Value)
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Chưa đến thời gian đăng ký."
                );
            }

            if (eventData.RegistrationEnd.HasValue && now > eventData.RegistrationEnd.Value)
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Đã hết thời gian đăng ký."
                );
            }

            // Get ALL seats with their current status (available, reserved, occupied)
            // ✅ FIX: Get seats directly from Hall instead of using GetEventSeatMapAsync
            var registrationEvent = await _eventRepo.GetByIdAsync(eventId);
            if (registrationEvent == null || registrationEvent.IsDeleted)
            {
                return ApiResponse<SeatMapDto>.FailureResponse(
                    "Sự kiện không tồn tại."
                );
            }

            var seats = registrationEvent.HallId != null
                ? await _seatRepo.GetSeatsByHallIdAsync(registrationEvent.HallId)
                : new List<IntervalEventRegistrationRepo.Entities.Seat>();
            var seatsList = seats.ToList();

            // Get statistics
            var statistics = registrationEvent.HallId != null
                ? await GetSeatStatisticsByHallAsync(registrationEvent.HallId)
                : new Dictionary<string, int> { ["total"] = 0, ["available"] = 0, ["reserved"] = 0, ["occupied"] = 0 };

            // Get tickets for this event to populate occupant info
            Dictionary<string, IntervalEventRegistrationRepo.Entities.Ticket?> seatTickets = new();
            var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
            seatTickets = tickets
                .Where(t => t.SeatId != null && t.Status != "cancelled")
                .GroupBy(t => t.SeatId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(t => t.RegisteredAt).FirstOrDefault()
                );

            // Group seats by row label
            var groupedSeats = seatsList
                .GroupBy(s => s.RowLabel)
                .OrderBy(g => g.Key)
                .ToList();

            var rows = groupedSeats.Select(group => new SeatRowDto
            {
                RowNumber = GetRowNumberFromLabel(group.Key ?? ""),
                RowLabel = group.Key ?? "",
                Seats = group.OrderBy(s => s.SeatNumber).Select(seat =>
                    MapToSeatItemDto(seat, false, seatTickets)
                ).ToList()
            }).ToList();

            var hall = registrationEvent.HallId != null
                ? await _hallRepo.GetByIdAsync(registrationEvent.HallId)
                : null;

            var result = new SeatMapDto
            {
                EventId = registrationEvent.EventId,
                HallId = registrationEvent.HallId,
                HallName = hall?.Name ?? "External Event",
                TotalRows = groupedSeats.Count,
                MaxSeatsPerRow = groupedSeats.Any()
                    ? groupedSeats.Max(g => g.Count())
                    : 0,
                TotalSeats = statistics.GetValueOrDefault("total", 0),
                AvailableSeats = statistics.GetValueOrDefault("available", 0),
                ReservedSeats = statistics.GetValueOrDefault("reserved", 0),
                OccupiedSeats = statistics.GetValueOrDefault("occupied", 0),
                Rows = rows
            };

            return ApiResponse<SeatMapDto>.SuccessResponse(
                result,
                "Lấy sơ đồ ghế trống cho đăng ký thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available seats for: {EventId}", eventId);
            return ApiResponse<SeatMapDto>.FailureResponse(
                "Đã xảy ra lỗi khi lấy danh sách ghế trống."
            );
        }
    }

    public async Task<ApiResponse<bool>> CheckSeatAvailabilityAsync(string seatId)
    {
        try
        {
            var isAvailable = await _seatRepo.IsSeatAvailableAsync(seatId);
            
            return ApiResponse<bool>.SuccessResponse(
                isAvailable,
                isAvailable ? "Ghế còn trống." : "Ghế đã được đặt."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seat availability: {SeatId}", seatId);
            return ApiResponse<bool>.FailureResponse(
                "Không thể kiểm tra trạng thái ghế."
            );
        }
    }

    public async Task<ApiResponse<Dictionary<string, int>>> GetSeatStatisticsAsync(string eventId)
    {
        try
        {
            var statistics = await _seatRepo.GetSeatStatisticsByEventAsync(eventId);

            return ApiResponse<Dictionary<string, int>>.SuccessResponse(
                statistics,
                "Lấy thống kê ghế thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat statistics: {EventId}", eventId);
            return ApiResponse<Dictionary<string, int>>.FailureResponse(
                "Không thể lấy thống kê ghế."
            );
        }
    }

    // ===== HELPER METHODS =====

    private async Task<Dictionary<string, int>> GetSeatStatisticsByHallAsync(string hallId)
    {
        var seats = await _seatRepo.GetSeatsByHallIdAsync(hallId);
        var seatsList = seats.ToList();

        return new Dictionary<string, int>
        {
            ["total"] = seatsList.Count,
            ["available"] = seatsList.Count(s => s.Status == "available"),
            ["reserved"] = seatsList.Count(s => s.Status == "reserved"),
            ["occupied"] = seatsList.Count(s => s.Status == "occupied")
        };
    }

    // ===== HELPER METHODS =====

    private int GetRowNumberFromLabel(string rowLabel)
    {
        // Convert A -> 1, B -> 2, etc.
        if (string.IsNullOrEmpty(rowLabel)) return 0;
        if (rowLabel.Length == 1)
        {
            return rowLabel[0] - 'A' + 1;
        }
        // Handle AA, AB, etc. if needed
        return 0;
    }

    private SeatItemDto MapToSeatItemDto(
        IntervalEventRegistrationRepo.Entities.Seat seat, 
        bool includeOccupant,
        Dictionary<string, IntervalEventRegistrationRepo.Entities.Ticket?> seatTickets)
    {
        var seatDto = new SeatItemDto
        {
            SeatId = seat.SeatId,
            RowNumber = GetRowNumberFromLabel(seat.RowLabel ?? ""),
            SeatNumber = int.TryParse(seat.SeatNumber, out int seatNum) ? seatNum : 0,
            Label = seat.SeatNumber,
            Status = seat.Status
        };

        if (includeOccupant && seatTickets.TryGetValue(seat.SeatId, out var ticket) && ticket != null)
        {
            seatDto.Occupant = new SeatOccupantDto
            {
                StudentId = ticket.StudentId,
                StudentName = ticket.Student?.Name ?? "N/A",
                StudentCode = ticket.Student?.StudentCode ?? "N/A",
                TicketCode = ticket.TicketCode,
                RegisteredAt = ticket.RegisteredAt,
                CheckInTime = ticket.CheckInTime,
                TicketStatus = ticket.Status
            };
        }

        return seatDto;
    }

    public async Task<ApiResponse<SeatDetailDto>> GetSeatDetailAsync(
        string seatId,
        string userId,
        string userRole)
    {
        try
        {
            _logger.LogInformation(
                "Getting seat detail: {SeatId}, User: {UserId}, Role: {Role}",
                seatId, userId, userRole
            );

            // Validate permissions
            if (userRole != "organizer" && userRole != "staff")
            {
                return ApiResponse<SeatDetailDto>.FailureResponse(
                    "Bạn không có quyền xem thông tin chi tiết ghế."
                );
            }

            // Get seat with full details
            var seat = await _seatRepo.GetSeatDetailAsync(seatId);
            
            if (seat == null)
            {
                return ApiResponse<SeatDetailDto>.FailureResponse(
                    "Không tìm thấy ghế."
                );
            }

            // If organizer, check ownership
            if (userRole == "organizer" && !string.IsNullOrEmpty(seat.EventId))
            {
                var eventData = await _eventRepo.GetByIdAsync(seat.EventId);
                if (eventData?.OrganizerId != userId)
                {
                    return ApiResponse<SeatDetailDto>.FailureResponse(
                        "Bạn chỉ có thể xem thông tin ghế của sự kiện mình tổ chức."
                    );
                }
            }

            // Map to DTO
            var seatDetail = await MapToSeatDetailDto(seat);

            return ApiResponse<SeatDetailDto>.SuccessResponse(
                seatDetail,
                "Lấy thông tin chi tiết ghế thành công."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat detail: {SeatId}", seatId);
            return ApiResponse<SeatDetailDto>.FailureResponse(
                "Đã xảy ra lỗi khi lấy thông tin ghế."
            );
        }
    }

    private async Task<SeatDetailDto> MapToSeatDetailDto(IntervalEventRegistrationRepo.Entities.Seat seat)
    {
        // Get active ticket for this seat
        var activeTicket = seat.Tickets?
            .Where(t => t.Status != "cancelled")
            .OrderByDescending(t => t.RegisteredAt)
            .FirstOrDefault();

        var isBooked = activeTicket != null;
        
        // Get status display text
        string statusDisplay = seat.Status switch
        {
            "available" => "Còn trống",
            "reserved" => "Đã đặt",
            "occupied" => "Đã check-in",
            _ => "Không xác định"
        };

        // Generate label from RowLabel + SeatNumber (e.g., A9)
        string label = $"{seat.RowLabel ?? ""}{seat.SeatNumber}";

        var seatDetail = new SeatDetailDto
        {
            SeatId = seat.SeatId,
            EventId = seat.EventId ?? string.Empty,
            HallId = seat.HallId,
            Label = label,
            RowNumber = GetRowNumberFromLabel(seat.RowLabel ?? ""),
            SeatNumber = int.TryParse(seat.SeatNumber, out int seatNum) ? seatNum : 0,
            RowLabel = seat.RowLabel ?? "",
            Status = seat.Status,
            StatusDisplay = statusDisplay,
            IsBooked = isBooked,
            CreatedAt = seat.CreatedAt,
            UpdatedAt = seat.UpdatedAt
        };

        // Add occupant details if seat is booked
        if (isBooked && activeTicket?.Student != null)
        {
            var checkIn = activeTicket.TicketCheckins?.FirstOrDefault();
            
            seatDetail.Occupant = new SeatOccupantDetailDto
            {
                StudentId = activeTicket.StudentId,
                StudentName = activeTicket.Student.Name ?? "N/A",
                StudentCode = activeTicket.Student.StudentCode ?? "N/A",
                Email = activeTicket.Student.Email ?? "N/A",
                Phone = activeTicket.Student.Phone,
                TicketId = activeTicket.TicketId,
                TicketCode = activeTicket.TicketCode,
                TicketStatus = activeTicket.Status,
                RegisteredAt = activeTicket.RegisteredAt,
                IsCheckedIn = checkIn != null,
                CheckInTime = checkIn?.CheckinTime,
                CheckedInBy = checkIn?.Staff?.Name
            };
        }

        return seatDetail;
    }
}

