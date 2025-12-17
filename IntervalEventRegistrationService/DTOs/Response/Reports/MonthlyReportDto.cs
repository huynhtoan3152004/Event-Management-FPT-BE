using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class MonthlyReportDto
    {
        // Tháng của dữ liệu (1-12)
        public int Month { get; set; }

        // Năm của dữ liệu
        public int Year { get; set; }

        // Tổng số lượt đăng kí sự kiện trong tháng đó
        public int TotalRegistrations { get; set; }

        // Số người tham gia sự kiện (Check-in) trong tháng đó
        public int TotalParticipants { get; set; }

        // Số người không tham gia (No-show) trong tháng đó
        public int TotalAbsences { get; set; }
    }
}
