using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Ticket;
using IntervalEventRegistrationService.DTOs.Response.Ticket;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketCheckinRepository _ticketCheckinRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ISeatRepository _seatRepository;

    public TicketService(ITicketRepository ticketRepository, ITicketCheckinRepository ticketCheckinRepository, IEventRepository eventRepository, ISeatRepository seatRepository)
    {
        _ticketRepository = ticketRepository;
        _ticketCheckinRepository = ticketCheckinRepository;
        _eventRepository = eventRepository;
        _seatRepository = seatRepository;
    }

    public async Task<ApiResponse<TicketDto>> RegisterAsync(string eventId, string studentId, RegisterTicketRequestDto request)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev == null || ev.IsDeleted)
        {
            return ApiResponse<TicketDto>.FailureResponse("Không tìm thấy sự kiện");
        }
        if (ev.Status != "published")
        {
            return ApiResponse<TicketDto>.FailureResponse("Sự kiện chưa mở đăng ký");
        }
        var now = DateTime.Now; // Use local time instead of UTC
        if (ev.RegistrationStart.HasValue && now < ev.RegistrationStart.Value.ToLocalTime())
        {
            return ApiResponse<TicketDto>.FailureResponse("Chưa đến thời điểm đăng ký");
        }
        if (ev.RegistrationEnd.HasValue && now > ev.RegistrationEnd.Value.ToLocalTime())
        {
            return ApiResponse<TicketDto>.FailureResponse("Đã hết thời gian đăng ký");
        }
        if (ev.RegisteredCount >= ev.TotalSeats)
        {
            return ApiResponse<TicketDto>.FailureResponse("Hết chỗ");
        }
        var existing = await _ticketRepository.GetActiveByEventAndStudentAsync(eventId, studentId);
        if (existing != null)
        {
            return ApiResponse<TicketDto>.FailureResponse("Đã đăng ký vé cho sự kiện này");
        }
        // Không cho đăng ký trùng giờ với sự kiện khác
        var myTickets = await _ticketRepository.GetByStudentIdAsync(studentId);
        foreach (var t in myTickets.Where(t => t.Status == "active"))
        {
            var other = await _eventRepository.GetByIdAsync(t.EventId);
            if (other != null && other.Date == ev.Date)
            {
                bool overlap = other.StartTime < ev.EndTime && other.EndTime > ev.StartTime;
                if (overlap)
                {
                    return ApiResponse<TicketDto>.FailureResponse("Bạn đã đăng ký sự kiện khác trùng thời gian");
                }
            }
        }

        string? seatId = null;
        string? seatNumber = null;
        if (!string.IsNullOrWhiteSpace(ev.HallId))
        {
            if (!string.IsNullOrWhiteSpace(request.SeatId))
            {
                var seat = await _seatRepository.GetByIdAsync(request.SeatId);
                if (seat == null || seat.EventId != eventId || seat.Status != "available")
                {
                    return ApiResponse<TicketDto>.FailureResponse("Ghế không hợp lệ hoặc không trống");
                }
                seatId = seat.SeatId;
                seatNumber = seat.SeatNumber;
                seat.Status = "reserved";
                await _seatRepository.UpdateAsync(seat);
                await _seatRepository.SaveChangesAsync();
            }
            else
            {
                // Auto-assign seat from available seats for this event
                var seats = await _seatRepository.GetByEventIdAsync(eventId);
                var availableSeat = seats.FirstOrDefault(s => s.Status == "available");
                if (availableSeat != null)
                {
                    seatId = availableSeat.SeatId;
                    seatNumber = availableSeat.SeatNumber;
                    availableSeat.Status = "reserved";
                    await _seatRepository.UpdateAsync(availableSeat);
                    await _seatRepository.SaveChangesAsync();
                }
            }
        }

        var ticketCode = Guid.NewGuid().ToString("N");
        var ticket = new Ticket
        {
            TicketId = Guid.NewGuid().ToString(),
            EventId = eventId,
            StudentId = studentId,
            SeatId = seatId,
            TicketCode = ticketCode,
            QrCode = ticketCode,
            Status = "active",
            RegisteredAt = DateTime.UtcNow
        };
        await _ticketRepository.AddAsync(ticket);
        ev.RegisteredCount += 1;
        await _eventRepository.UpdateAsync(ev);
        await _ticketRepository.SaveChangesAsync();
        await _eventRepository.SaveChangesAsync();

        var dto = MapToDto(ticket, ev, seatNumber);
        return ApiResponse<TicketDto>.SuccessResponse(dto, "Đăng ký vé thành công");
    }

    public async Task<ApiResponse<TicketDto>> GetByCodeAsync(string ticketCode)
    {
        var ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode);
        if (ticket == null)
        {
            return ApiResponse<TicketDto>.FailureResponse("Không tìm thấy vé");
        }
        var seatNumber = ticket.Seat?.SeatNumber;
        var dto = MapToDto(ticket, ticket.Event!, seatNumber);
        return ApiResponse<TicketDto>.SuccessResponse(dto, "Lấy thông tin vé thành công");
    }

    public async Task<ApiResponse<CheckinResultDto>> CheckinByCodeAsync(string ticketCode, string staffId, string staffRole)
    {
        if (staffRole != "staff" && staffRole != "organizer")
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Không có quyền check-in");
        }
        var ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode);
        if (ticket == null)
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Ticket Not Found");
        }
        if (ticket.Status == "cancelled")
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Ticket Cancelled");
        }
        if (ticket.Status == "used" || ticket.CheckInTime.HasValue)
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Already Checked In");
        }

        var ev = await _eventRepository.GetByIdAsync(ticket.EventId);
        if (ev == null)
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Ticket Not Found");
        }

        ticket.Status = "used";
        ticket.CheckInTime = DateTime.UtcNow;
        await _ticketRepository.UpdateAsync(ticket);
        ev.CheckedInCount += 1;
        await _eventRepository.UpdateAsync(ev);

        if (!string.IsNullOrWhiteSpace(ticket.SeatId))
        {
            var seat = await _seatRepository.GetByIdAsync(ticket.SeatId!);
            if (seat != null)
            {
                seat.Status = "occupied";
                await _seatRepository.UpdateAsync(seat);
            }
        }

        var checkin = new TicketCheckin
        {
            CheckinId = Guid.NewGuid().ToString(),
            TicketId = ticket.TicketId,
            StaffId = staffId,
            CheckinTime = DateTime.UtcNow,
            Status = "success"
        };
        await _ticketCheckinRepository.AddAsync(checkin);

        await _ticketRepository.SaveChangesAsync();
        await _eventRepository.SaveChangesAsync();
        await _seatRepository.SaveChangesAsync();
        await _ticketCheckinRepository.SaveChangesAsync();

        return ApiResponse<CheckinResultDto>.SuccessResponse(new CheckinResultDto { Result = "Valid" }, "Check-in thành công");
    }

    public async Task<ApiResponse<bool>> CancelAsync(string ticketId, string currentUserId, string currentUserRole)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy vé");
        }
        if (ticket.Status == "used")
        {
            return ApiResponse<bool>.FailureResponse("Vé đã sử dụng");
        }

        var ev = await _eventRepository.GetByIdAsync(ticket.EventId);
        if (ev == null)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy sự kiện");
        }

        if (currentUserRole != "organizer" && currentUserId != ticket.StudentId)
        {
            return ApiResponse<bool>.FailureResponse("Không có quyền hủy vé");
        }

        // Student chỉ được hủy trước 24 giờ
        if (currentUserRole == "student")
        {
            var eventStart = new DateTime(ev.Date.Year, ev.Date.Month, ev.Date.Day,
                ev.StartTime.Hour, ev.StartTime.Minute, ev.StartTime.Second, DateTimeKind.Utc);
            
            var hoursUntilEvent = (eventStart - DateTime.UtcNow).TotalHours;
            
            if (hoursUntilEvent < 24)
            {
                return ApiResponse<bool>.FailureResponse("Chỉ được hủy vé trước 24 giờ");
            }
        }
        // Organizer có thể hủy bất cứ lúc nào

        ticket.Status = "cancelled";
        ticket.CancelledAt = DateTime.UtcNow;
        ticket.CancelReason = "User cancelled";
        await _ticketRepository.UpdateAsync(ticket);

        if (!string.IsNullOrWhiteSpace(ticket.SeatId))
        {
            var seat = await _seatRepository.GetByIdAsync(ticket.SeatId!);
            if (seat != null)
            {
                seat.Status = "available";
                await _seatRepository.UpdateAsync(seat);
            }
        }

        if (ev.RegisteredCount > 0)
        {
            ev.RegisteredCount -= 1;
            await _eventRepository.UpdateAsync(ev);
        }

        await _ticketRepository.SaveChangesAsync();
        await _eventRepository.SaveChangesAsync();
        await _seatRepository.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Hủy vé thành công");
    }

    public async Task<ApiResponse<List<TicketDto>>> GetByEventAsync(string eventId)
    {
        var tickets = await _ticketRepository.GetByEventIdAsync(eventId);
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev == null)
        {
            return ApiResponse<List<TicketDto>>.FailureResponse("Không tìm thấy sự kiện");
        }
        var list = tickets.Select(t => MapToDto(t, ev, t.Seat?.SeatNumber)).ToList();
        return ApiResponse<List<TicketDto>>.SuccessResponse(list, "Lấy danh sách vé theo sự kiện thành công");
    }

    public async Task<ApiResponse<List<TicketDto>>> GetByStudentAsync(string studentId)
    {
        var tickets = await _ticketRepository.GetByStudentIdAsync(studentId);
        var result = new List<TicketDto>();
        foreach (var t in tickets)
        {
            var ev = await _eventRepository.GetByIdAsync(t.EventId);
            if (ev != null)
            {
                result.Add(MapToDto(t, ev, t.Seat?.SeatNumber));
            }
        }
        return ApiResponse<List<TicketDto>>.SuccessResponse(result, "Lấy danh sách vé của người dùng thành công");
    }

    private TicketDto MapToDto(Ticket t, Event ev, string? seatNumber)
    {
        return new TicketDto
        {
            TicketId = t.TicketId,
            TicketCode = t.TicketCode,
            Status = t.Status,
            EventId = t.EventId,
            EventTitle = ev.Title,
            EventDate = ev.Date,
            EventStartTime = ev.StartTime,
            EventEndTime = ev.EndTime,
            StudentId = t.StudentId,
            SeatId = t.SeatId,
            SeatNumber = seatNumber
        };
    }
}
