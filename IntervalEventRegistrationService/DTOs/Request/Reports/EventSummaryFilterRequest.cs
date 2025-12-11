using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Reports
{
    public class EventSummaryFilterRequest
    {
        // Id của sự kiện cần lấy báo cáo chi tiết
        public string EventId { get; set; } = string.Empty;

        // Trạng thái vé cần filter (active, used, cancelled, expired), nếu null thì lấy tất cả
        public string? TicketStatus { get; set; }
    }
}
