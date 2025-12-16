using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Models.Reports
{
    public class MonthlyAttendanceRawData
    {
        public int Year { get; set; } // Năm của tháng thống kê
        public int Month { get; set; } // Tháng thống kê (1-12)
        public int TotalTickets { get; set; } // Tổng lượt đăng ký (tổng ticket)
        public int ParticipatedTickets { get; set; } // Số người đã tham gia (theo status tham gia)
    }
}
