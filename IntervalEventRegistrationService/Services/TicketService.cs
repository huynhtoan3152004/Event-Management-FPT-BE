using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Ticket;
using IntervalEventRegistrationService.DTOs.Response.Ticket;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

/// <summary>
/// Ticket Service - Quản lý đăng ký vé, check-in và check-out
/// 
/// ✨ LOGIC MỚI: TRẠNG THÁI GHẾ ĐƯỢC TÍNH TOÁN ĐỘNG THEO TỬNG EVENT
/// - Ghế thuộc về Hall (dùng chung cho nhiều sự kiện)
/// - Seat.Status trong database KHÔNG được sử dụng (bỏ qua)
/// - Trạng thái được tính động từ Tickets của mỗi event:
///   • available: Không có ticket active cho ghế này trong event
///   • reserved: Có ticket active (registered/confirmed) nhưng chưa check-in
///   • occupied: Có ticket đã check-in
/// 
/// LUỒNG TRẠNG THÁI VÉ:
/// 1. ĐĂNG KÝ: active (khi tạo)
/// 2. CHECK-IN: checked-in (khi sinh viên vào)
/// 3. CHECK-OUT: completed (khi sinh viên ra)
/// 4. HỦY: cancelled (nếu sinh viên hủy trước check-in)
/// 5. KHÔNG ĐẾN: no-show (nếu không check-in sau khi event kết thúc)
/// 6. BỎ DỞ: abandoned (nếu check-in nhưng không check-out sau khi event kết thúc)
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
        // ✅ Kiểm tra xung đột thời gian với các event khác (so sánh TimeSpan trực tiếp)
        var myTickets = await _ticketRepository.GetByStudentIdAsync(studentId);
        foreach (var t in myTickets.Where(t => t.Status == "active" || t.Status == "checked-in"))
        {
            var other = await _eventRepository.GetByIdAsync(t.EventId);
            if (other != null && other.Date == ev.Date)
            {
                // So sánh TimeSpan trực tiếp để phát hiện trùng lặp
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
            // Lấy tickets hiện có của event để kiểm tra ghế còn trống
            var existingTickets = await _ticketRepository.GetByEventIdAsync(eventId);
            var takenSeatIds = existingTickets
                .Where(t => t.SeatId != null && t.Status != "cancelled")
                .Select(t => t.SeatId)
                .ToHashSet();

            if (!string.IsNullOrWhiteSpace(request.SeatId))
            {
                var seat = await _seatRepository.GetByIdAsync(request.SeatId);
                // ✅ KIỂM TRA: Ghế thuộc Hall
                if (seat == null || seat.HallId != ev.HallId)
                {
                    return ApiResponse<TicketDto>.FailureResponse("Ghế không hợp lệ");
                }
                
                // ✅ KIỂM TRA: Ghế chưa được đặt bởi ai trong event này
                if (takenSeatIds.Contains(seat.SeatId))
                {
                    return ApiResponse<TicketDto>.FailureResponse("Ghế đã được đặt bởi người khác");
                }
                
                seatId = seat.SeatId;
                seatNumber = seat.SeatNumber;
                // ❌ ĐÃ XÓA: KHÔNG cập nhật seat.Status trong database (trạng thái được tính động theo event)
            }
            else
            {
                // ✅ Tự động gán ghế từ ghế trống của Hall (chưa ai đặt trong event này)
                var seats = await _seatRepository.GetByHallIdAsync(ev.HallId);
                var availableSeat = seats.FirstOrDefault(s => !takenSeatIds.Contains(s.SeatId));
                if (availableSeat != null)
                {
                    seatId = availableSeat.SeatId;
                    seatNumber = availableSeat.SeatNumber;
                    // ❌ ĐÃ XÓA: KHÔNG cập nhật seat.Status trong database (trạng thái được tính động theo event)
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
        // ✅ Kiểm tra đã check-in chưa (xóa trạng thái "used" cũ)
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

        // ❌ ĐÃ XÓA: KHÔNG cập nhật seat.Status (trạng thái được tính động theo event)

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

        // ✅ Sinh viên chỉ được hủy trước 24h (so sánh UTC)
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
        // Organizer có thể hủy bất cứ lúc nào

        ticket.Status = "cancelled";
        ticket.CancelledAt = DateTime.UtcNow; // ✅ UTC
        ticket.CancelReason = currentUserRole == "organizer" 
            ? "Cancelled by organizer" 
            : "Cancelled by student";
        await _ticketRepository.UpdateAsync(ticket);

        // ❌ ĐÃ XÓA: KHÔNG cập nhật seat.Status (trạng thái được tính động theo event)

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
            // 1. Kiểm tra quyền staff
            if (staffRole != "staff" && staffRole != "organizer")
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse("Không có quyền check-out");
            }

            // 2. Kiểm tra mã vé và lấy ticket
            var ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode);
            
            if (ticket == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "QR code không hợp lệ hoặc vé không tồn tại."
                );
            }

            // 2. Kiểm tra vé đã check-in chưa
            var activeCheckin = await _ticketCheckinRepository.GetActiveCheckinByTicketIdAsync(ticket.TicketId);
            
            if (activeCheckin == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "Vé này chưa được check-in. Không thể check-out."
                );
            }

            // 3. Kiểm tra trạng thái sự kiện
            var eventData = await _eventRepository.GetByIdAsync(ticket.EventId);
            
            if (eventData == null)
            {
                return ApiResponse<CheckoutResponseDto>.FailureResponse(
                    "Sự kiện không tồn tại."
                );
            }

            // ✅ Cho phép checkout bất cứ lúc nào sau check-in (không giới hạn thời gian)
            // Nếu vé đã check-in, sinh viên có thể checkout khi họ muốn rời đi
            
            // ❌ ĐÃ COMMENT - Logic cũ: Chỉ cho phép checkout 30 phút trước khi event kết thúc
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

            // 4. Cập nhật thời gian checkout trong TicketCheckin
            var checkoutTime = DateTime.UtcNow;
            await _ticketCheckinRepository.UpdateCheckoutTimeAsync(
                activeCheckin.CheckinId, 
                checkoutTime
            );

            // ✅ CHECK-OUT: checked-in → completed
            ticket.Status = "completed";
            await _ticketRepository.UpdateAsync(ticket);

            // ✅ GIỮ trạng thái seat = "occupied" cho thống kê
            // Ghế sẽ được reset về "available" khi event hoàn thành

            await _ticketRepository.SaveChangesAsync();

            // 5. Tính thời lượng
            var duration = checkoutTime - activeCheckin.CheckinTime;

            // 6. Lấy thông tin staff
            var staff = activeCheckin.Staff;

            // 7. Tạo response
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
    /// ❌ ĐÃ LỖI THỜI: Không cần thiết nữa vì seat.Status được tính động theo event.
    /// Ghế thuộc Hall (dùng chung), trạng thái được xác định bởi tickets, không phải database.
    /// </summary>
    [Obsolete("Do not use. Seat status is now calculated dynamically from tickets per event.")]
    public async Task<ApiResponse<bool>> ResetEventSeatsAsync(string eventId)
    {
        // Không làm gì: Trạng thái ghế được tính động, không cần reset
        return ApiResponse<bool>.SuccessResponse(true, "Trạng thái ghế được tính động, không cần reset");
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
