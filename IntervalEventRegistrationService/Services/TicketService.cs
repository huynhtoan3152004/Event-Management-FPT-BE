using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Ticket;
using IntervalEventRegistrationService.DTOs.Response.Ticket;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

/// <summary>
/// Ticket Service - Manages ticket registration, check-in, and check-out
/// 
/// ✨ NEW LOGIC: SEAT STATUS IS CALCULATED DYNAMICALLY PER EVENT
/// - Seats belong to Hall (shared across multiple events)
/// - Seat.Status in database is NOT used (ignored)
/// - Status is calculated dynamically from Tickets of each event:
///   • available: No active ticket for this seat in this event
///   • reserved: Has active ticket (registered/confirmed) but not checked-in
///   • occupied: Has ticket that is checked-in
/// 
/// TICKET STATUS FLOW:
/// 1. REGISTER: active (when created)
/// 2. CHECK-IN: checked-in (when student enters)
/// 3. CHECK-OUT: completed (when student leaves)
/// 4. CANCEL: cancelled (if student cancels before checkin)
/// 5. NO-SHOW: no-show (if not checked-in after event ends)
/// 6. ABANDONED: abandoned (if checked-in but not checked-out after event ends)
/// </summary>
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
        var now = DateTime.UtcNow;
        if (ev.RegistrationStart.HasValue && now < ev.RegistrationStart.Value)
        {
            return ApiResponse<TicketDto>.FailureResponse("Chưa đến thời điểm đăng ký");
        }
        if (ev.RegistrationEnd.HasValue && now > ev.RegistrationEnd.Value)
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
        // ✅ Check time conflict with other events (compare TimeSpan directly)
        var myTickets = await _ticketRepository.GetByStudentIdAsync(studentId);
        foreach (var t in myTickets.Where(t => t.Status == "active" || t.Status == "checked-in"))
        {
            var other = await _eventRepository.GetByIdAsync(t.EventId);
            if (other != null && other.Date == ev.Date)
            {
                // Compare TimeSpan directly for overlap detection
                bool overlap = other.StartTime < ev.EndTime && other.EndTime > ev.StartTime;
                if (overlap)
                {
                    return ApiResponse<TicketDto>.FailureResponse(
                        $"Bạn đã đăng ký sự kiện '{other.Title}' trùng thời gian ({other.StartTime:hh\\:mm} - {other.EndTime:hh\\:mm})"
                    );
                }
            }
        }

        string? seatId = null;
        string? seatNumber = null;
        if (!string.IsNullOrWhiteSpace(ev.HallId))
        {
            // Get existing tickets for this event to check seat availability
            var existingTickets = await _ticketRepository.GetByEventIdAsync(eventId);
            var takenSeatIds = existingTickets
                .Where(t => t.SeatId != null && t.Status != "cancelled")
                .Select(t => t.SeatId)
                .ToHashSet();

            if (!string.IsNullOrWhiteSpace(request.SeatId))
            {
                var seat = await _seatRepository.GetByIdAsync(request.SeatId);
                // ✅ CHECK: Seat thuộc Hall
                if (seat == null || seat.HallId != ev.HallId)
                {
                    return ApiResponse<TicketDto>.FailureResponse("Ghế không hợp lệ");
                }
                
                // ✅ CHECK: Seat chưa được đặt bởi ai trong event này
                if (takenSeatIds.Contains(seat.SeatId))
                {
                    return ApiResponse<TicketDto>.FailureResponse("Ghế đã được đặt bởi người khác");
                }
                
                seatId = seat.SeatId;
                seatNumber = seat.SeatNumber;
                // ❌ REMOVED: Do NOT update seat.Status in database (status is calculated dynamically per event)
            }
            else
            {
                // ✅ Auto-assign seat from Hall's available seats (not taken by anyone in this event)
                var seats = await _seatRepository.GetByHallIdAsync(ev.HallId);
                var availableSeat = seats.FirstOrDefault(s => !takenSeatIds.Contains(s.SeatId));
                if (availableSeat != null)
                {
                    seatId = availableSeat.SeatId;
                    seatNumber = availableSeat.SeatNumber;
                    // ❌ REMOVED: Do NOT update seat.Status in database (status is calculated dynamically per event)
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
        // ✅ Check if already checked-in (removed legacy "used" status)
        if (ticket.Status == "checked-in")
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Already Checked In");
        }

        var ev = await _eventRepository.GetByIdAsync(ticket.EventId);
        if (ev == null)
        {
            return ApiResponse<CheckinResultDto>.FailureResponse("Ticket Not Found");
        }

        ticket.Status = "checked-in"; // ✅ CHECK-IN: active → checked-in
        ticket.CheckInTime = DateTime.UtcNow;
        await _ticketRepository.UpdateAsync(ticket);
        ev.CheckedInCount += 1;
        await _eventRepository.UpdateAsync(ev);

        // ❌ REMOVED: Do NOT update seat.Status (status is calculated dynamically per event)

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
        await _ticketCheckinRepository.SaveChangesAsync();

        return ApiResponse<CheckinResultDto>.SuccessResponse(
            new CheckinResultDto 
            { 
                Result = "Valid",
                TicketCode = ticketCode
            }, 
            "Check-in thành công");
    }

    public async Task<ApiResponse<bool>> CancelAsync(string ticketId, string currentUserId, string currentUserRole)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy vé");
        }
        if (ticket.Status == "cancelled")
        {
            return ApiResponse<bool>.FailureResponse("Vé đã bị hủy");
        }
        if (ticket.Status == "completed")
        {
            return ApiResponse<bool>.FailureResponse("Vé đã hoàn thành, không thể hủy");
        }
        if (ticket.Status == "checked-in")
        {
            return ApiResponse<bool>.FailureResponse("Vé đã check-in, không thể hủy");
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

        // ✅ Student only cancel 24h before event (UTC comparison)
        if (currentUserRole == "student")
        {
            var eventStartUtc = new DateTime(
                ev.Date.Year, ev.Date.Month, ev.Date.Day,
                ev.StartTime.Hour, ev.StartTime.Minute, ev.StartTime.Second, 
                DateTimeKind.Utc
            );
            
            var hoursUntilEvent = (eventStartUtc - DateTime.UtcNow).TotalHours;
            
            if (hoursUntilEvent < 24)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Chỉ được hủy vé trước 24 giờ. Còn {hoursUntilEvent:F1} giờ nữa đến sự kiện."
                );
            }
        }
        // Organizer can cancel anytime

        ticket.Status = "cancelled";
        ticket.CancelledAt = DateTime.UtcNow; // ✅ UTC
        ticket.CancelReason = currentUserRole == "organizer" 
            ? "Cancelled by organizer" 
            : "Cancelled by student";
        await _ticketRepository.UpdateAsync(ticket);

        // ❌ REMOVED: Do NOT update seat.Status (status is calculated dynamically per event)

        if (ev.RegisteredCount > 0)
        {
            ev.RegisteredCount -= 1;
            await _eventRepository.UpdateAsync(ev);
        }

        await _ticketRepository.SaveChangesAsync();
        await _eventRepository.SaveChangesAsync();

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

    public async Task<ApiResponse<CheckoutResponseDto>> CheckOutAsync(
        string ticketCode,
        string staffId,
        string staffRole)
    {
        try
        {
            // 1. Validate staff role
            if (staffRole != "staff" && staffRole != "organizer")
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse("Không có quyền check-out");
            }

            // 2. Validate ticket code and get ticket
            var ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode);
            
            if (ticket == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "QR code không hợp lệ hoặc vé không tồn tại."
                );
            }

            // 2. Check if ticket has been checked in
            var activeCheckin = await _ticketCheckinRepository.GetActiveCheckinByTicketIdAsync(ticket.TicketId);
            
            if (activeCheckin == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "Vé này chưa được check-in. Không thể check-out."
                );
            }

            // 3. Validate event status
            var eventData = await _eventRepository.GetByIdAsync(ticket.EventId);
            
            if (eventData == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "Sự kiện không tồn tại."
                );
            }

            // ✅ Allow checkout anytime after check-in (no time restriction)
            // If ticket has been checked-in, student can checkout whenever they want to leave
            
            // ❌ COMMENTED OUT - Old validation: Only allow checkout 30 minutes before event ends
            // var now = DateTime.UtcNow;
            // var eventStart = new DateTime(
            //     eventData.Date.Year, eventData.Date.Month, eventData.Date.Day,
            //     eventData.StartTime.Hour, eventData.StartTime.Minute, eventData.StartTime.Second,
            //     DateTimeKind.Utc);
            // 
            // var eventEnd = new DateTime(
            //     eventData.Date.Year, eventData.Date.Month, eventData.Date.Day,
            //     eventData.EndTime.Hour, eventData.EndTime.Minute, eventData.EndTime.Second,
            //     DateTimeKind.Utc);
            // 
            // // Cannot checkout before event starts
            // if (now < eventStart)
            // {
            //     return ApiResponse<CheckoutResponseDto>.FailureResponse(
            //         "Sự kiện chưa bắt đầu. Không thể check-out."
            //     );
            // }
            // 
            // // Allow checkout 30 minutes before event ends
            // var checkoutAllowedFrom = eventEnd.AddMinutes(-30);
            // if (now < checkoutAllowedFrom)
            // {
            //     var minutesRemaining = (checkoutAllowedFrom - now).TotalMinutes;
            //     return ApiResponse<CheckoutResponseDto>.FailureResponse(
            //         $"Chỉ có thể check-out từ 30 phút trước khi sự kiện kết thúc. Còn {minutesRemaining:F0} phút nữa."
            //     );
            // }

            // 4. Update checkout time in TicketCheckin
            var checkoutTime = DateTime.UtcNow;
            await _ticketCheckinRepository.UpdateCheckoutTimeAsync(
                activeCheckin.CheckinId, 
                checkoutTime
            );

            // ✅ CHECK-OUT: checked-in → completed
            ticket.Status = "completed";
            await _ticketRepository.UpdateAsync(ticket);

            // ✅ KEEP seat status = "occupied" for statistics
            // Seats will be reset to "available" when event completes

            await _ticketRepository.SaveChangesAsync();

            // 5. Calculate duration
            var duration = checkoutTime - activeCheckin.CheckinTime;

            // 6. Get staff info
            var staff = activeCheckin.Staff;

            // 7. Build response
            var response = new CheckoutResponseDto
            {
                TicketId = ticket.TicketId,
                TicketCode = ticket.TicketCode,
                StudentName = ticket.Student?.Name ?? activeCheckin.Ticket?.Student?.Name ?? "N/A",
                EventName = eventData.Title,
                SeatLabel = ticket.Seat != null 
                    ? $"{ticket.Seat.RowLabel}{ticket.Seat.SeatNumber}" 
                    : activeCheckin.Ticket?.Seat != null 
                        ? $"{activeCheckin.Ticket.Seat.RowLabel}{activeCheckin.Ticket.Seat.SeatNumber}"
                        : "N/A",
                CheckinTime = activeCheckin.CheckinTime,
                CheckoutTime = checkoutTime,
                Duration = duration,
                CheckedOutBy = staff?.Name ?? "N/A",
                Message = $"Check-out thành công! Thời gian tham dự: {duration.Hours}h {duration.Minutes}m"
            };

            return ApiResponse<CheckoutResponseDto>.SuccessResponse(
                response,
                "Check-out thành công!"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<CheckoutResponseDto>.FailureResponse(
                $"Đã xảy ra lỗi khi check-out: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// ❌ DEPRECATED: No longer needed since seat.Status is calculated dynamically per event.
    /// Seats belong to Hall (shared across events), status is determined by tickets, not database.
    /// </summary>
    [Obsolete("Do not use. Seat status is now calculated dynamically from tickets per event.")]
    public async Task<ApiResponse<bool>> ResetEventSeatsAsync(string eventId)
    {
        // No-op: Seat status is calculated dynamically, no need to reset
        return ApiResponse<bool>.SuccessResponse(true, "Seat status is calculated dynamically, no reset needed");
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
            StudentName = t.Student?.Name ?? string.Empty,
            SeatId = t.SeatId,
            SeatNumber = seatNumber
        };
    }
}
