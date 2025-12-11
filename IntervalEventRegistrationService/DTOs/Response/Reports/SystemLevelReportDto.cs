using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class SystemLevelReportDto
    {
        // Tổng số sự kiện trong khoảng thời gian from - to sau khi áp dụng filter status
        public int TotalEvents { get; set; }

        // Tổng số sinh viên (student) tham gia, tính theo số StudentId phân biệt đã check-in/used
        public int TotalStudentsParticipated { get; set; }

        // Tổng số vé được tạo trong khoảng thời gian from - to (mọi trạng thái)
        public int TotalTickets { get; set; }

        // Tổng số lượt check-in (log check-in thành công) trên toàn hệ thống
        public int TotalCheckins { get; set; }

        // Danh sách thống kê số lượng sự kiện theo từng tháng (year + month + count)
        public List<MonthlyEventsDto> EventsByMonth { get; set; } = new();

        // Danh sách thống kê số lượng người tham dự theo từng tháng
        public List<MonthlyAttendanceDto> AttendanceByMonth { get; set; } = new();
    }

    public class MonthlyEventsDto
    {
        // Năm của nhóm thống kê (ví dụ: 2025)
        public int Year { get; set; }

        // Tháng của nhóm thống kê (1 - 12)
        public int Month { get; set; }

        // Số lượng sự kiện diễn ra trong tháng tương ứng
        public int EventCount { get; set; }
    }

    public class MonthlyAttendanceDto
    {
        // Năm của nhóm thống kê (ví dụ: 2025)
        public int Year { get; set; }

        // Tháng của nhóm thống kê (1 - 12)
        public int Month { get; set; }

        // Số lượng người tham dự phân biệt (distinct student) trong tháng
        public int ParticipantCount { get; set; }
    }
}
