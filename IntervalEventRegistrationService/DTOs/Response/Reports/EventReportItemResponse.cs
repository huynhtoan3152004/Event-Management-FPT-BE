using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class EventReportItemResponse
    {
        public string EventName { get; set; } = string.Empty; // Tên sự kiện
        public DateOnly EventDate { get; set; } // Ngày diễn ra sự kiện
        public int TotalRegistrations { get; set; } // Tổng lượt đăng ký của event
        public int ParticipatedCount { get; set; } // Số người tham gia
        public int NotParticipatedCount { get; set; } // Số người không tham gia
        public double ParticipatedPercent { get; set; } // % tham gia
        public double NotParticipatedPercent { get; set; } // % không tham gia
        public int AbandonedCount { get; set; } // Số người check-in nhưng chưa check-out của event
    }
}
