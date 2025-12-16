using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class SystemReportResponse
    {
        public int TotalEvents { get; set; } // Tổng số sự kiện
        public int TotalRegistrations { get; set; } // Tổng số lượt đăng ký (tổng ticket)
        public int ParticipatedCount { get; set; } // Số người tham gia
        public int NotParticipatedCount { get; set; } // Số người không tham gia
        public double ParticipatedPercent { get; set; } // % tham gia
        public double NotParticipatedPercent { get; set; } // % không tham gia
    }
}
