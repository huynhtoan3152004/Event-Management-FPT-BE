using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class EventSummaryReportDto
    {
        // Mã định danh duy nhất của sự kiện, dùng để tham chiếu khi cần
        public string EventId { get; set; } = string.Empty;

        // Tiêu đề / tên sự kiện để hiển thị trong báo cáo
        public string Title { get; set; } = string.Empty;

        // Ngày diễn ra sự kiện (DateOnly trùng với kiểu trong entity Event)
        public DateOnly Date { get; set; }

        // Thời gian bắt đầu sự kiện
        public TimeOnly StartTime { get; set; }

        // Thời gian kết thúc sự kiện
        public TimeOnly EndTime { get; set; }

        // Tên hội trường (Hall) diễn ra sự kiện, có thể null nếu không gán hall
        public string? HallName { get; set; }

        // Địa chỉ cụ thể của hội trường, phục vụ hiển thị rõ ràng hơn cho báo cáo
        public string? HallAddress { get; set; }

        // Tên người hoặc đơn vị tổ chức sự kiện (Organizer)
        public string OrganizerName { get; set; } = string.Empty;

        // Tổng số ghế của sự kiện để làm mẫu số tính tỷ lệ lấp ghế
        public int TotalSeats { get; set; }

        // Trạng thái hiện tại của sự kiện (draft, pending, approved, completed, ...)
        public string Status { get; set; } = string.Empty;

        // Tổng số lượt đăng ký (tương ứng với RegisteredCount trong bảng events)
        public int RegisteredCount { get; set; }

        // Tỷ lệ lấp ghế = RegisteredCount / TotalSeats * 100, phục vụ biểu đồ occupancy
        public double SeatOccupancyPercent { get; set; }

        // Số lượng vé đã bị hủy (cancelled)
        public int CancelledCount { get; set; }

        // Tổng số người đã check-in (CheckedInCount trong bảng events)
        public int CheckedInCount { get; set; }

        // Tỷ lệ check-in = CheckedInCount / RegisteredCount * 100
        public double CheckInRatePercent { get; set; }

        // Số lượng log check-in thất bại (TicketCheckin.Status = failed)
        public int FailedCheckinCount { get; set; }

        // Số lượng vé đang ở trạng thái active (đã đăng ký, chưa check-in)
        public int ActiveTickets { get; set; }

        // Số lượng vé đã sử dụng (used = đã check-in thành công)
        public int UsedTickets { get; set; }

        // Số lượng vé đã bị hủy (cancelled)
        public int CancelledTickets { get; set; }

        // Số lượng vé hết hạn (expired) nếu hệ thống vẫn sử dụng trạng thái này
        public int ExpiredTickets { get; set; }

        // Danh sách số lượng check-in được nhóm theo từng khoảng thời gian (time slot)
        public List<CheckinTimeSlotDto> CheckinByTimeSlots { get; set; } = new();
    }

    public class CheckinTimeSlotDto
    {
        // Thời điểm bắt đầu của khoảng thời gian (ví dụ: 09:00, 10:00, 11:00)
        public DateTime TimeSlotStart { get; set; }

        // Số lượng check-in diễn ra trong khoảng thời gian tương ứng
        public int Count { get; set; }
    }
}
