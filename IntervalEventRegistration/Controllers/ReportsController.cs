using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Reports;
using IntervalEventRegistrationService.DTOs.Response.Reports;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntervalEventRegistration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        private static readonly string[] AllowedTicketStatuses = { "active", "used", "cancelled", "expired" };
        private static readonly string[] AllowedEventStatuses = { "draft", "pending", "approved", "published", "completed", "cancelled", "rejected" };

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        [HttpGet("event-summary")]
        [Authorize(Roles = "organizer")]
        public async Task<IActionResult> GetEventSummary([FromQuery] EventSummaryFilterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EventId)) // Kiểm tra nếu EventId bị bỏ trống hoặc chỉ toàn khoảng trắng
            {
                ApiResponse<EventSummaryReportDto> errorResponse = ApiResponse<EventSummaryReportDto>.FailureResponse("EventId là bắt buộc"); // Tạo response lỗi chuẩn hóa với message rõ ràng
                return BadRequest(errorResponse); // Trả về HTTP 400 BadRequest cùng body chứa thông tin lỗi
            }

            if (!string.IsNullOrWhiteSpace(request.TicketStatus)) // Nếu client có truyền ticketStatus lên để filter
            {
                bool isValidStatus = AllowedTicketStatuses // Lấy danh sách các trạng thái vé hợp lệ
                    .Contains(request.TicketStatus, StringComparer.OrdinalIgnoreCase); // Kiểm tra ticketStatus có nằm trong danh sách allowed hay không (không phân biệt hoa thường)

                if (!isValidStatus) // Nếu ticketStatus không nằm trong danh sách hợp lệ
                {
                    string validValues = string.Join(", ", AllowedTicketStatuses); // Ghép các giá trị hợp lệ thành một chuỗi để hiển thị trong message
                    _logger.LogWarning("Giá trị ticketStatus không hợp lệ: {TicketStatus}. Hợp lệ: {ValidValues}", request.TicketStatus, validValues); // Ghi log cảnh báo để dễ debug sau này

                    ApiResponse<EventSummaryReportDto> errorResponse = ApiResponse<EventSummaryReportDto>.FailureResponse($"ticketStatus không hợp lệ. Giá trị hợp lệ: {validValues}"); // Tạo response lỗi với message chỉ rõ các giá trị hợp lệ
                    return BadRequest(errorResponse); // Trả về HTTP 400 cùng thông báo lỗi cho client
                }
            }

            try
            {
                _logger.LogInformation("Organizer yêu cầu Event Summary Report cho EventId = {EventId}, TicketStatus = {TicketStatus}", request.EventId, request.TicketStatus); // Ghi log thông tin đầu vào để phục vụ theo dõi

                ApiResponse<EventSummaryReportDto> result = await _reportService.GetEventSummaryAsync(request); // Gọi service để lấy dữ liệu báo cáo tổng quan cho một sự kiện

                if (!result.Success) // Nếu service trả về trạng thái thất bại (ví dụ event không tồn tại)
                {
                    _logger.LogWarning("Lấy Event Summary Report thất bại cho EventId = {EventId}. Message = {Message}", request.EventId, result.Message); // Ghi log cảnh báo lý do thất bại
                    return NotFound(result); // Trả về HTTP 404 NotFound với body là ApiResponse chứa thông tin lỗi
                }

                _logger.LogInformation("Lấy Event Summary Report thành công cho EventId = {EventId}", request.EventId); // Ghi log khi lấy báo cáo thành công
                return Ok(result); // Trả về HTTP 200 OK kèm dữ liệu báo cáo trong body
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong đợi khi lấy Event Summary Report cho EventId = {EventId}", request.EventId); // Ghi log lỗi hệ thống kèm exception chi tiết để tiện điều tra

                ApiResponse<EventSummaryReportDto> errorResponse = ApiResponse<EventSummaryReportDto>.FailureResponse("Đã xảy ra lỗi khi tạo báo cáo sự kiện"); // Tạo ApiResponse lỗi với message chung thân thiện với người dùng
                return StatusCode(500, errorResponse); // Trả về HTTP 500 Internal Server Error với body thống nhất format
            }
        }

        [HttpGet("system-summary")]
        [Authorize(Roles = "organizer")]
        public async Task<IActionResult> GetSystemSummary([FromQuery] SystemLevelReportFilterRequest request)
        {
            if (request.From.HasValue && request.To.HasValue && request.From > request.To) // Kiểm tra nếu cả from và to đều được truyền vào và from lại lớn hơn to
            {
                _logger.LogWarning("Giá trị khoảng thời gian không hợp lệ: From = {From}, To = {To}", request.From, request.To); // Ghi log cảnh báo khoảng thời gian bị đảo ngược

                ApiResponse<SystemLevelReportDto> errorResponse = ApiResponse<SystemLevelReportDto>.FailureResponse("'from' không được lớn hơn 'to'"); // Tạo ApiResponse lỗi với message rõ ràng cho người dùng
                return BadRequest(errorResponse); // Trả về HTTP 400 BadRequest vì dữ liệu đầu vào không hợp lệ
            }

            if (!string.IsNullOrWhiteSpace(request.EventStatus)) // Nếu client có truyền eventStatus để filter
            {
                bool isValidEventStatus = AllowedEventStatuses // Lấy danh sách các trạng thái event hợp lệ
                    .Contains(request.EventStatus, StringComparer.OrdinalIgnoreCase); // Kiểm tra eventStatus có nằm trong danh sách allowed hay không (không phân biệt hoa thường)

                if (!isValidEventStatus) // Nếu eventStatus không hợp lệ
                {
                    string validValues = string.Join(", ", AllowedEventStatuses); // Ghép các trạng thái hợp lệ thành chuỗi để hiển thị trong thông báo lỗi
                    _logger.LogWarning("Giá trị eventStatus không hợp lệ: {EventStatus}. Hợp lệ: {ValidValues}", request.EventStatus, validValues); // Ghi log cảnh báo về giá trị không hợp lệ

                    ApiResponse<SystemLevelReportDto> errorResponse = ApiResponse<SystemLevelReportDto>.FailureResponse($"eventStatus không hợp lệ. Giá trị hợp lệ: {validValues}"); // Tạo ApiResponse lỗi với message chứa danh sách trạng thái hợp lệ
                    return BadRequest(errorResponse); // Trả về HTTP 400 kèm thông tin lỗi cho client
                }
            }

            try
            {
                _logger.LogInformation("Organizer yêu cầu System-Level Report với From = {From}, To = {To}, EventStatus = {EventStatus}", request.From, request.To, request.EventStatus); // Ghi log thông tin filter mà client gửi lên

                ApiResponse<SystemLevelReportDto> result = await _reportService.GetSystemLevelReportAsync(request); // Gọi service để tổng hợp dữ liệu báo cáo toàn hệ thống theo điều kiện filter

                _logger.LogInformation("Lấy System-Level Report thành công với From = {From}, To = {To}, EventStatus = {EventStatus}", request.From, request.To, request.EventStatus); // Ghi log khi xử lý báo cáo thành công
                return Ok(result); // Trả về HTTP 200 OK cùng dữ liệu báo cáo trong body
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong đợi khi lấy System-Level Report với From = {From}, To = {To}, EventStatus = {EventStatus}", request.From, request.To, request.EventStatus); // Ghi log lỗi hệ thống kèm exception chi tiết

                ApiResponse<SystemLevelReportDto> errorResponse = ApiResponse<SystemLevelReportDto>.FailureResponse("Đã xảy ra lỗi khi tạo báo cáo toàn hệ thống"); // Tạo ApiResponse lỗi với message thân thiện
                return StatusCode(500, errorResponse); // Trả về HTTP 500 Internal Server Error kèm body lỗi chuẩn
            }
        }
    }
}
