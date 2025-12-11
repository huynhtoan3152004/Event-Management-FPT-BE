using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Reports;
using IntervalEventRegistrationService.DTOs.Response.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Interfaces
{
    public interface IReportService
    {
        // Hàm này dùng để lấy báo cáo tổng quan cho một sự kiện cụ thể, có thể filter theo trạng thái vé
        Task<ApiResponse<EventSummaryReportDto>> GetEventSummaryAsync(EventSummaryFilterRequest request);

        // Hàm này dùng để lấy báo cáo tổng hợp toàn hệ thống trong khoảng thời gian from - to, có filter theo trạng thái event
        Task<ApiResponse<SystemLevelReportDto>> GetSystemLevelReportAsync(SystemLevelReportFilterRequest request);
    }
}
