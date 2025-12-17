using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Reports
{
    public class ReportDateFilterDto
    {
        // Thuộc tính ngày bắt đầu (FromDate)
        // DataType.Date chỉ định đây là kiểu ngày tháng
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; } // Cho phép null nếu muốn lấy từ đầu

        // Thuộc tính ngày kết thúc (ToDate)
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; } // Cho phép null nếu muốn lấy đến hiện tại
    }
    
}
