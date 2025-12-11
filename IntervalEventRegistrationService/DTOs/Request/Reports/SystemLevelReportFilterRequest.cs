using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Reports
{
    public class SystemLevelReportFilterRequest
    {
        // Ngày bắt đầu khoảng thời gian cần thống kê, nếu null thì service sẽ tự chọn mặc định
        public DateTime? From { get; set; }

        // Ngày kết thúc khoảng thời gian cần thống kê, nếu null thì service sẽ tự chọn mặc định
        public DateTime? To { get; set; }

        // Trạng thái event dùng để filter (vd: approved, published, completed), nếu null thì lấy tất cả
        public string? EventStatus { get; set; }
    }
}
