using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class MonthlyReportItemResponse
    {
        public int Year { get; set; } // Năm
        public int Month { get; set; } // Tháng
        public int TotalRegistrations { get; set; } // Tổng lượt đăng ký trong tháng
        public int ParticipatedCount { get; set; } // Số người tham gia trong tháng
        public int NotParticipatedCount { get; set; } // Số người không tham gia trong tháng
    }
}
