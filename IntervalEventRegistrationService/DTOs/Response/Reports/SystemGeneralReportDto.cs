using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Reports
{
    public class SystemGeneralReportDto
    {
        // Tổng số sự kiện được tạo trong khoảng thời gian lọc
        public int TotalEventsCreated { get; set; }

        // Tổng số lượt đăng kí vé (vé đã bán/đăng ký thành công)
        public int TotalTicketRegistrations { get; set; }

        // Số lượng người thực tế đã tham gia (Check-in)
        public int TotalParticipants { get; set; }

        // Số lượng người đăng ký nhưng không tham gia (No-show)
        public int TotalAbsences { get; set; }

        // Phần trăm (%) người tham gia so với tổng vé đăng ký
        public double ParticipationRate { get; set; }

        // Phần trăm (%) người không tham gia so với tổng vé đăng ký
        public double AbsenceRate { get; set; }
    }
}
