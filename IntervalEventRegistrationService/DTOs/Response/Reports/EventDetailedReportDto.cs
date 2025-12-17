using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class EventDetailedReportDto
    {
        // ID của sự kiện (để FE có thể link tới chi tiết nếu cần)
        public Guid EventId { get; set; }

        // Tên sự kiện
        public string EventName { get; set; }

        // Ngày diễn ra sự kiện
        public DateTime EventDate { get; set; }

        // Tổng số lượt đăng kí vé của sự kiện này
        public int TotalRegistrations { get; set; }

        // Số người tham gia (Check-in)
        public int ParticipantsCount { get; set; }

        // Số người không tham gia (Vắng mặt)
        public int AbsencesCount { get; set; }

        // % tham gia
        public double ParticipationRate { get; set; }

        // % không tham gia
        public double AbsenceRate { get; set; }
    }
}
