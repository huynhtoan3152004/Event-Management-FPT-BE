using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Ticket
{
    public class TicketStatusListResponseDto
    {
        public List<string> Statuses { get; set; } = new(); // danh sách status để FE dùng làm filter
        public int Total { get; set; } // tổng số status để FE dễ hiển thị thống kê
    }
}
