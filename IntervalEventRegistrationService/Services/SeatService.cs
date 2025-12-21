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

            // Get seats from Hall (seats belong to Hall, not Event)
            var seats = eventData.HallId != null
                ? await _seatRepo.GetSeatsByHallIdAsync(eventData.HallId)
                : new List<IntervalEventRegistrationRepo.Entities.Seat>();
            var seatsList = seats.ToList();

            // Get tickets for this event to calculate dynamic status
            var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
            var seatTickets = tickets
                .Where(t => t.SeatId != null && t.Status != "cancelled")
                .GroupBy(t => t.SeatId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(t => t.RegisteredAt).FirstOrDefault()
                );

            // Calculate dynamic status for each seat based on event tickets
            var seatsWithDynamicStatus = seatsList.Select(seat => new
            {
                Seat = seat,
                DynamicStatus = CalculateSeatStatus(seat.SeatId, seatTickets)
            }).ToList();

            // Apply filters on dynamic status
            if (filter?.Statuses != null && filter.Statuses.Any())
            {
                seatsWithDynamicStatus = seatsWithDynamicStatus
                    .Where(s => filter.Statuses.Contains(s.DynamicStatus.ToLower()))
                    .ToList();
            }

            if (filter?.RowNumbers != null && filter.RowNumbers.Any())
            {
                seatsWithDynamicStatus = seatsWithDynamicStatus
                    .Where(s => filter.RowNumbers.Contains(GetRowNumberFromLabel(s.Seat.RowLabel ?? "")))
                    .ToList();
            }

            seatsList = seatsWithDynamicStatus.Select(s => s.Seat).ToList();

            // Calculate statistics dynamically from current seats + tickets
            var statistics = CalculateSeatStatistics(seatsList, seatTickets);

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

            // Get tickets for this event to calculate dynamic status
            var tickets = await _ticketRepo.GetByEventIdAsync(eventId);
            var seatTickets = tickets
                .Where(t => t.SeatId != null && t.Status != "cancelled")
                .GroupBy(t => t.SeatId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(t => t.RegisteredAt).FirstOrDefault()
                );

            // Calculate statistics dynamically from current seats + tickets
            var statistics = CalculateSeatStatistics(seatsList, seatTickets);

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

    /// <summary>
    /// Calculate seat status dynamically based on event tickets.
    /// Since seats belong to Hall (shared across events), we calculate status per event.
    /// </summary>
    private string CalculateSeatStatus(
        string seatId, 
        Dictionary<string, IntervalEventRegistrationRepo.Entities.Ticket?> seatTickets)
    {
        if (!seatTickets.TryGetValue(seatId, out var ticket) || ticket == null)
        {
            return "available"; // No active ticket = available
        }

        // Check if ticket is checked in
        if (ticket.CheckInTime.HasValue || ticket.TicketCheckins?.Any() == true)
        {
            return "occupied"; // Checked in = occupied
        }

        // Has active ticket (registered/confirmed) but not checked in
        if (ticket.Status == "registered" || ticket.Status == "confirmed" || ticket.Status == "active")
        {
            return "reserved"; // Booked but not checked in = reserved
        }

        return "available"; // Default to available
    }

    /// <summary>
    /// Calculate seat statistics dynamically from current seats + event tickets
    /// </summary>
    private Dictionary<string, int> CalculateSeatStatistics(
        List<IntervalEventRegistrationRepo.Entities.Seat> seats,
        Dictionary<string, IntervalEventRegistrationRepo.Entities.Ticket?> seatTickets)
    {
        var total = seats.Count;
        var available = 0;
        var reserved = 0;
        var occupied = 0;

        foreach (var seat in seats)
        {
            var status = CalculateSeatStatus(seat.SeatId, seatTickets);
            switch (status)
            {
                case "available":
                    available++;
                    break;
                case "reserved":
                    reserved++;
                    break;
                case "occupied":
                    occupied++;
                    break;
            }
        }

        return new Dictionary<string, int>
        {
            ["total"] = total,
            ["available"] = available,
            ["reserved"] = reserved,
            ["occupied"] = occupied
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
        // ✅ Calculate dynamic status based on event tickets (not database seat.Status)
        string dynamicStatus = CalculateSeatStatus(seat.SeatId, seatTickets);

        var seatDto = new SeatItemDto
        {
            SeatId = seat.SeatId,
            RowNumber = GetRowNumberFromLabel(seat.RowLabel ?? ""),
            SeatNumber = int.TryParse(seat.SeatNumber, out int seatNum) ? seatNum : 0,
            Label = seat.SeatNumber,
            Status = dynamicStatus // ✅ Use dynamic status
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
        
        // ✅ Calculate dynamic status based on ticket
        string dynamicStatus = "available";
        if (activeTicket != null)
        {
            if (activeTicket.CheckInTime.HasValue || activeTicket.TicketCheckins?.Any() == true)
            {
                dynamicStatus = "occupied";
            }
            else if (activeTicket.Status == "active" || activeTicket.Status == "registered" || activeTicket.Status == "confirmed")
            {
                dynamicStatus = "reserved";
            }
        }

        // Get status display text
        string statusDisplay = dynamicStatus switch
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
            Status = dynamicStatus, // ✅ Use dynamic status
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

