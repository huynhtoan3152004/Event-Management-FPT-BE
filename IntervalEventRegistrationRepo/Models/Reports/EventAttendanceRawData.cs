using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Models.Reports
{
    public class EventAttendanceRawData
    {
        public string EventId { get; set; } = string.Empty; // Id sự kiện để định danh
        public string Title { get; set; } = string.Empty; // Tên sự kiện để hiển thị report
        public DateOnly Date { get; set; } // Ngày diễn ra sự kiện để hiển thị report
        public int TotalTickets { get; set; } // Tổng lượt đăng ký của event
        public int ParticipatedTickets { get; set; } // Số người tham gia của event
        public int AbandonedTickets { get; set; } // Số người check-in nhưng chưa check-out của event (abandoned)
    }
}
