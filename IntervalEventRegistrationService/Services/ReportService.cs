using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Reports;
using IntervalEventRegistrationService.DTOs.Response.Reports;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IReportRepository reportRepository, ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository;
            _logger = logger;
        }

        public async Task<ApiResponse<SystemReportResponse>> GetSystemReportAsync(string? fromDate, string? toDate, CancellationToken cancellationToken = default) // Service xử lý report tổng hợp
        {
            try // Bọc try để log lỗi runtime
            {
                var errors = new List<string>(); // Tạo list để gom lỗi validate

                DateOnly? from = TryParseDateOnly(fromDate, "fromDate", errors); // Parse fromDate (nullable)
                DateOnly? to = TryParseDateOnly(toDate, "toDate", errors); // Parse toDate (nullable)

                if (errors.Count > 0) // Nếu có lỗi parse/validate format
                {
                    _logger.LogWarning("GetSystemReportAsync validation failed. Errors={Errors}", string.Join(" | ", errors)); // Log cảnh báo validate
                    return ApiResponse<SystemReportResponse>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi 400 ở controller
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value) // Validate logic from <= to
                {
                    errors.Add("fromDate phải nhỏ hơn hoặc bằng toDate (định dạng yyyy-MM-dd)."); // Thêm lỗi vào danh sách
                    _logger.LogWarning("GetSystemReportAsync date range invalid. from={From}, to={To}", from, to); // Log cảnh báo date range
                    return ApiResponse<SystemReportResponse>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi
                }

                int totalEvents = await _reportRepository.CountEventsByEventDateAsync(from, to, cancellationToken); // Đếm tổng event trong khoảng ngày
                int totalTickets = await _reportRepository.CountTicketsByEventDateAsync(from, to, cancellationToken); // Đếm tổng ticket trong khoảng ngày
                int participated = await _reportRepository.CountParticipatedTicketsByEventDateAsync(from, to, cancellationToken); // Đếm tổng tham gia theo status fix cứng

                int notParticipated = totalTickets - participated; // Tính không tham gia bằng cách lấy tổng trừ tham gia
                if (notParticipated < 0) notParticipated = 0; // Chặn âm phòng trường hợp dữ liệu lệch

                double participatedPercent = totalTickets == 0 ? 0 : (double)participated * 100 / totalTickets; // Tính % tham gia (chia 0 thì =0)
                double notParticipatedPercent = totalTickets == 0 ? 0 : (double)notParticipated * 100 / totalTickets; // Tính % không tham gia

                int abandoned = await _reportRepository.CountAbandonedTicketsByEventDateAsync(from, to, cancellationToken); // Đếm abandoned theo khoảng ngày

                var response = new SystemReportResponse // Tạo DTO response trả về API
                {
                    TotalEvents = totalEvents, // Gán tổng event
                    TotalRegistrations = totalTickets, // Gán tổng đăng ký
                    ParticipatedCount = participated, // Gán số tham gia
                    NotParticipatedCount = notParticipated, // Gán số không tham gia
                    ParticipatedPercent = Math.Round(participatedPercent, 2), // Làm tròn % tham gia
                    NotParticipatedPercent = Math.Round(notParticipatedPercent, 2), // Làm tròn % không tham gia
                    AbandonedCount = abandoned // Gán số check-in chưa check-out
                };


                return ApiResponse<SystemReportResponse>.SuccessResponse(response, "Thành công"); // Trả về success
            }
            catch (Exception ex) // Bắt lỗi hệ thống để log
            {
                _logger.LogError(ex, "GetSystemReportAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm input
                return ApiResponse<SystemReportResponse>.FailureResponse("Lỗi hệ thống", new List<string> { "Có lỗi xảy ra khi tạo báo cáo." }); // Trả về lỗi chung
            }
        }

        public async Task<ApiResponse<List<MonthlyReportItemResponse>>> GetMonthlyReportAsync(string? fromDate, string? toDate, CancellationToken cancellationToken = default) // Service xử lý report theo tháng
        {
            try // Bọc try để log lỗi runtime
            {
                var errors = new List<string>(); // Tạo list gom lỗi validate

                DateOnly? from = TryParseDateOnly(fromDate, "fromDate", errors); // Parse fromDate
                DateOnly? to = TryParseDateOnly(toDate, "toDate", errors); // Parse toDate

                if (errors.Count > 0) // Nếu có lỗi format
                {
                    _logger.LogWarning("GetMonthlyReportAsync validation failed. Errors={Errors}", string.Join(" | ", errors)); // Log validate
                    return ApiResponse<List<MonthlyReportItemResponse>>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value) // Validate range
                {
                    errors.Add("fromDate phải nhỏ hơn hoặc bằng toDate (định dạng yyyy-MM-dd)."); // Thêm lỗi
                    _logger.LogWarning("GetMonthlyReportAsync date range invalid. from={From}, to={To}", from, to); // Log cảnh báo
                    return ApiResponse<List<MonthlyReportItemResponse>>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi
                }

                var raw = await _reportRepository.GetMonthlyAttendanceByEventDateAsync(from, to, cancellationToken); // Lấy raw data theo tháng từ repo

                var result = raw.Select(x => new MonthlyReportItemResponse // Map raw sang response DTO
                {
                    Year = x.Year, // Gán năm
                    Month = x.Month, // Gán tháng
                    TotalRegistrations = x.TotalTickets, // Gán tổng đăng ký
                    ParticipatedCount = x.ParticipatedTickets, // Gán số tham gia
                    NotParticipatedCount = Math.Max(0, x.TotalTickets - x.ParticipatedTickets), // Tính số không tham gia
                    AbandonedCount = x.AbandonedTickets // Gán số check-in chưa check-out trong tháng
                }).ToList(); // Convert sang List


                return ApiResponse<List<MonthlyReportItemResponse>>.SuccessResponse(result, "Thành công"); // Trả về success
            }
            catch (Exception ex) // Bắt lỗi hệ thống để log
            {
                _logger.LogError(ex, "GetMonthlyReportAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm input
                return ApiResponse<List<MonthlyReportItemResponse>>.FailureResponse("Lỗi hệ thống", new List<string> { "Có lỗi xảy ra khi tạo báo cáo theo tháng." }); // Trả về lỗi chung
            }
        }

        public async Task<ApiResponse<List<EventReportItemResponse>>> GetEventsReportAsync(string? fromDate, string? toDate, CancellationToken cancellationToken = default) // Service xử lý danh sách event report
        {
            try // Bọc try để log lỗi runtime
            {
                var errors = new List<string>(); // Tạo list gom lỗi validate

                DateOnly? from = TryParseDateOnly(fromDate, "fromDate", errors); // Parse fromDate
                DateOnly? to = TryParseDateOnly(toDate, "toDate", errors); // Parse toDate

                if (errors.Count > 0) // Nếu lỗi format
                {
                    _logger.LogWarning("GetEventsReportAsync validation failed. Errors={Errors}", string.Join(" | ", errors)); // Log validate
                    return ApiResponse<List<EventReportItemResponse>>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value) // Validate range
                {
                    errors.Add("fromDate phải nhỏ hơn hoặc bằng toDate (định dạng yyyy-MM-dd)."); // Thêm lỗi
                    _logger.LogWarning("GetEventsReportAsync date range invalid. from={From}, to={To}", from, to); // Log cảnh báo
                    return ApiResponse<List<EventReportItemResponse>>.FailureResponse("Dữ liệu không hợp lệ", errors); // Trả về lỗi
                }

                var raw = await _reportRepository.GetEventsAttendanceByEventDateAsync(from, to, cancellationToken); // Lấy raw list event + số liệu từ repo

                var result = raw.Select(x => // Map raw sang response DTO
                {
                    int notParticipated = Math.Max(0, x.TotalTickets - x.ParticipatedTickets); // Tính không tham gia (chặn âm)
                    double participatedPercent = x.TotalTickets == 0 ? 0 : (double)x.ParticipatedTickets * 100 / x.TotalTickets; // Tính % tham gia
                    double notParticipatedPercent = x.TotalTickets == 0 ? 0 : (double)notParticipated * 100 / x.TotalTickets; // Tính % không tham gia

                    return new EventReportItemResponse // Tạo object response theo event
                    {
                        EventId = x.EventId, // Gán EventId để frontend dùng
                        EventName = x.Title, // Gán tên event
                        EventDate = x.Date, // Gán ngày event
                        TotalRegistrations = x.TotalTickets, // Gán tổng đăng ký
                        ParticipatedCount = x.ParticipatedTickets, // Gán số tham gia
                        NotParticipatedCount = notParticipated, // Gán số không tham gia
                        ParticipatedPercent = Math.Round(participatedPercent, 2), // Làm tròn % tham gia
                        NotParticipatedPercent = Math.Round(notParticipatedPercent, 2), // Làm tròn % không tham gia
                        AbandonedCount = x.AbandonedTickets // Gán số check-in chưa check-out của event
                    };

                }).ToList(); // Convert sang List

                return ApiResponse<List<EventReportItemResponse>>.SuccessResponse(result, "Thành công"); // Trả về success
            }
            catch (Exception ex) // Bắt lỗi hệ thống để log
            {
                _logger.LogError(ex, "GetEventsReportAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm input
                return ApiResponse<List<EventReportItemResponse>>.FailureResponse("Lỗi hệ thống", new List<string> { "Có lỗi xảy ra khi tạo danh sách báo cáo sự kiện." }); // Trả về lỗi chung
            }
        }

        private static DateOnly? TryParseDateOnly(string? input, string fieldName, List<string> errors) // Helper parse DateOnly theo format yyyy-MM-dd
        {
            if (string.IsNullOrWhiteSpace(input)) return null; // Nếu null/rỗng thì coi như không filter

            if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)) // Parse theo format chuẩn
            {
                return value; // Parse ok thì trả về DateOnly
            }

            errors.Add($"{fieldName} không đúng định dạng yyyy-MM-dd."); // Parse fail thì thêm lỗi vào list
            return null; // Trả về null để service xử lý lỗi
        }

        public async Task<ApiResponse<EventSummaryReportDto>> GetEventSummaryAsync(EventSummaryFilterRequest request)
        {
            Event? ev = await _reportRepository.GetEventWithDetailsAsync(request.EventId); // Gọi repository để lấy thông tin sự kiện kèm theo Hall và Organizer theo EventId trong request

            if (ev == null) // Kiểm tra nếu không tìm thấy sự kiện trong database
            {
                return ApiResponse<EventSummaryReportDto>.FailureResponse("Event không tồn tại"); // Trả về ApiResponse thất bại nếu không có event với id tương ứng
            }

            List<Ticket> allTickets = await _reportRepository.GetTicketsByEventAsync(request.EventId, null); // Lấy toàn bộ danh sách ticket của sự kiện mà không filter trạng thái để có dữ liệu đầy đủ

            List<Ticket> filteredTickets = allTickets; // Khởi tạo danh sách ticket đã filter mặc định bằng toàn bộ ticket

            if (!string.IsNullOrWhiteSpace(request.TicketStatus)) // Nếu request có truyền trạng thái vé để filter
            {
                filteredTickets = allTickets // Bắt đầu từ toàn bộ danh sách ticket
                    .Where(t => t.Status == request.TicketStatus) // Lọc những ticket có Status trùng với trạng thái cần filter
                    .ToList(); // Chuyển kết quả về List để dễ xử lý tiếp theo
            }

            List<TicketCheckin> checkins = await _reportRepository.GetTicketCheckinsByEventAsync(request.EventId); // Lấy toàn bộ log check-in của sự kiện để dùng cho thống kê check-in

            int totalSeats = ev.TotalSeats; // Lấy tổng số ghế của sự kiện từ entity Event để làm mẫu số tính tỉ lệ lấp ghế

            int registeredCount = filteredTickets.Count; // Số lượt đăng ký được tính bằng số lượng ticket sau khi áp dụng filter trạng thái (nếu có)

            int cancelledTickets = filteredTickets // Bắt đầu từ danh sách ticket sau filter
                .Count(t => t.Status == "cancelled"); // Đếm số vé có trạng thái cancelled để dùng cho thống kê số lượng vé hủy

            int usedTickets = filteredTickets // Bắt đầu từ danh sách ticket đã filter
                .Count(t => t.Status == "used"); // Đếm số vé có trạng thái used, tương đương với số người đã check-in thành công

            int activeTickets = filteredTickets // Bắt đầu từ danh sách ticket đã filter
                .Count(t => t.Status == "active"); // Đếm số vé có trạng thái active, tức đã đăng ký nhưng chưa check-in

            int expiredTickets = filteredTickets // Bắt đầu từ danh sách ticket đã filter
                .Count(t => t.Status == "expired"); // Đếm số vé có trạng thái expired nếu hệ thống vẫn sử dụng trạng thái này

            double seatOccupancyPercent = 0; // Khởi tạo biến tỉ lệ lấp ghế mặc định là 0 để tránh lỗi chia cho 0

            if (totalSeats > 0) // Kiểm tra tổng số ghế phải lớn hơn 0 mới có ý nghĩa tính toán
            {
                seatOccupancyPercent = (double)registeredCount / totalSeats * 100; // Tính tỉ lệ lấp ghế dựa trên công thức RegisteredCount / TotalSeats * 100
            }

            int checkedInCount = usedTickets; // Số người check-in được tính bằng số lượng vé used trong danh sách ticket đã filter

            double checkinRatePercent = 0; // Khởi tạo biến tỉ lệ check-in mặc định là 0

            if (registeredCount > 0) // Kiểm tra phải có ít nhất một lượt đăng ký thì tỉ lệ check-in mới có ý nghĩa
            {
                checkinRatePercent = (double)checkedInCount / registeredCount * 100; // Tính tỉ lệ check-in theo công thức CheckedInCount / RegisteredCount * 100
            }

            int failedCheckins = checkins // Bắt đầu từ danh sách tất cả log check-in của sự kiện
                .Count(c => c.Status == "failed"); // Đếm số log có trạng thái failed để thống kê số check-in thất bại

            List<CheckinTimeSlotDto> checkinByTimeSlots = checkins // Bắt đầu từ toàn bộ log check-in của sự kiện
                .Where(c => c.Status == "success") // Chỉ lấy các log check-in thành công để vẽ biểu đồ theo thời gian
                .GroupBy(c => new DateTime( // Gom nhóm các log theo khung giờ
                    c.CheckinTime.Year, // Lấy năm của thời điểm check-in
                    c.CheckinTime.Month, // Lấy tháng của thời điểm check-in
                    c.CheckinTime.Day, // Lấy ngày của thời điểm check-in
                    c.CheckinTime.Hour, // Lấy giờ của thời điểm check-in để gom theo giờ
                    0, // Đặt phút bằng 0 để gom tất cả check-in trong cùng giờ vào một slot
                    0)) // Đặt giây bằng 0 để đơn giản hóa slot
                .Select(g => new CheckinTimeSlotDto // Ánh xạ từng nhóm sang DTO biểu diễn theo slot thời gian
                {
                    TimeSlotStart = g.Key, // Thời điểm bắt đầu slot chính là key của group (giờ tròn)
                    Count = g.Count() // Số lượng check-in trong slot tương ứng bằng số phần tử của group
                })
                .OrderBy(x => x.TimeSlotStart) // Sắp xếp các slot theo thứ tự thời gian tăng dần để dễ hiển thị
                .ToList(); // Chuyển về List để gán vào DTO báo cáo

            EventSummaryReportDto dto = new EventSummaryReportDto // Tạo đối tượng DTO chứa toàn bộ dữ liệu báo cáo cần trả về
            {
                EventId = ev.EventId, // Gán mã sự kiện lấy từ entity Event
                Title = ev.Title, // Gán tiêu đề sự kiện
                Date = ev.Date, // Gán ngày diễn ra sự kiện
                StartTime = ev.StartTime, // Gán thời gian bắt đầu sự kiện
                EndTime = ev.EndTime, // Gán thời gian kết thúc sự kiện
                HallName = ev.Hall?.Name, // Gán tên hội trường nếu tồn tại Hall, nếu null thì để null
                HallAddress = ev.Hall?.Address, // Gán địa chỉ hội trường nếu có thông tin
                OrganizerName = ev.Organizer?.Name ?? string.Empty, // Gán tên organizer, nếu null thì trả về chuỗi rỗng để tránh null reference
                TotalSeats = totalSeats, // Gán tổng số ghế đã tính từ entity Event
                Status = ev.Status, // Gán trạng thái sự kiện hiện tại

                RegisteredCount = registeredCount, // Gán số lượt đăng ký sau khi áp dụng filter trạng thái ticket (nếu có)
                SeatOccupancyPercent = seatOccupancyPercent, // Gán tỉ lệ lấp ghế đã tính toán
                CancelledCount = cancelledTickets, // Gán số lượng vé hủy để hiển thị trong báo cáo

                CheckedInCount = checkedInCount, // Gán tổng số người đã check-in (dựa trên số vé used)
                CheckInRatePercent = checkinRatePercent, // Gán tỉ lệ check-in của sự kiện
                FailedCheckinCount = failedCheckins, // Gán số lượng check-in thất bại

                ActiveTickets = activeTickets, // Gán số lượng vé đang active trong tập ticket đã filter
                UsedTickets = usedTickets, // Gán số lượng vé đã used trong tập ticket đã filter
                CancelledTickets = cancelledTickets, // Gán số lượng vé cancelled trong tập ticket đã filter
                ExpiredTickets = expiredTickets, // Gán số lượng vé expired trong tập ticket đã filter
                CheckinByTimeSlots = checkinByTimeSlots // Gán danh sách thống kê check-in theo từng slot thời gian
            };

            return ApiResponse<EventSummaryReportDto>.SuccessResponse(dto); // Trả về ApiResponse thành công chứa dữ liệu report của sự kiện
        }

        public async Task<ApiResponse<SystemLevelReportDto>> GetSystemLevelReportAsync(SystemLevelReportFilterRequest request)
        {
            DateTime? from = request.From; // nếu không truyền from → để null để lấy toàn bộ
            DateTime? to = request.To;     // nếu không truyền to → để null để lấy toàn bộ
            string? eventStatusFilter = request.EventStatus; // filter theo status (nếu có)


            List<Event> events = await _reportRepository.GetEventsForSystemReportAsync(from, to, eventStatusFilter); // Gọi repository để lấy danh sách event trong khoảng from - to kèm filter trạng thái event nếu có

            List<Ticket> tickets = await _reportRepository.GetTicketsForSystemReportAsync(from, to, eventStatusFilter); // Gọi repository để lấy danh sách ticket thuộc các event trong khoảng thời gian và trạng thái đã filter

            List<TicketCheckin> checkins = await _reportRepository.GetCheckinsForSystemReportAsync(from, to, eventStatusFilter); // Gọi repository để lấy danh sách log check-in thuộc những event trong khoảng thời gian và trạng thái đã filter

            int totalEvents = events.Count; // Tổng số sự kiện trong khoảng thời gian được tính bằng số phần tử trong danh sách events

            int totalTickets = tickets.Count; // Tổng số vé được tính bằng số phần tử trong danh sách tickets

            int totalCheckins = checkins // Bắt đầu từ danh sách log check-in
                .Count(c => c.Status == "success"); // Đếm số log check-in có trạng thái success để tính tổng lượt check-in thành công

            int totalStudentsParticipated = tickets // Bắt đầu từ danh sách ticket đã được filter theo khoảng thời gian và trạng thái event
                .Where(t => t.Status == "used") // Chỉ lấy những vé đã used tương ứng với người đã tham dự sự kiện
                .Select(t => t.StudentId) // Lấy StudentId từ từng vé used
                .Distinct() // Loại bỏ trùng StudentId để mỗi student chỉ được tính một lần
                .Count(); // Đếm tổng số student khác nhau đã tham dự trong khoảng thời gian và trạng thái event đã chọn

            List<MonthlyEventsDto> eventsByMonth = events // Bắt đầu từ danh sách event
                .GroupBy(e => new { e.Date.Year, e.Date.Month }) // Gom nhóm các event theo cặp Year - Month của ngày diễn ra sự kiện
                .Select(g => new MonthlyEventsDto // Ánh xạ từng group sang DTO thống kê theo tháng
                {
                    Year = g.Key.Year, // Gán năm tương ứng với group
                    Month = g.Key.Month, // Gán tháng tương ứng với group
                    EventCount = g.Count() // Gán số lượng event trong group cho EventCount
                })
                .OrderBy(x => x.Year) // Sắp xếp danh sách theo năm tăng dần
                .ThenBy(x => x.Month) // Với cùng năm thì sắp xếp theo tháng tăng dần
                .ToList(); // Chuyển kết quả về List để gán vào DTO trả về

            List<MonthlyAttendanceDto> attendanceByMonth = tickets // Bắt đầu từ danh sách ticket đã filter
                .Where(t => t.Status == "used" && t.CheckInTime.HasValue) // Chỉ lấy những vé used có CheckInTime khác null để thống kê người tham dự
                .GroupBy(t => new // Gom nhóm các vé theo tháng - năm dựa trên CheckInTime
                {
                    Year = t.CheckInTime!.Value.Year, // Lấy năm của thời điểm check-in
                    Month = t.CheckInTime!.Value.Month // Lấy tháng của thời điểm check-in
                })
                .Select(g => new MonthlyAttendanceDto // Ánh xạ từng nhóm sang DTO thống kê người tham dự theo tháng
                {
                    Year = g.Key.Year, // Gán năm của nhóm
                    Month = g.Key.Month, // Gán tháng của nhóm
                    ParticipantCount = g // Bắt đầu từ tập vé trong nhóm
                        .Select(t => t.StudentId) // Lấy StudentId của mỗi vé
                        .Distinct() // Loại bỏ những StudentId trùng nhau trong cùng tháng
                        .Count() // Đếm số lượng student khác nhau để ra số người tham dự trong tháng
                })
                .OrderBy(x => x.Year) // Sắp xếp kết quả theo năm tăng dần
                .ThenBy(x => x.Month) // Nếu cùng năm thì sắp xếp theo tháng tăng dần
                .ToList(); // Chuyển về List để gán vào DTO tổng hợp hệ thống

            SystemLevelReportDto dto = new SystemLevelReportDto // Tạo DTO chứa kết quả tổng hợp cho toàn hệ thống
            {
                TotalEvents = totalEvents, // Gán tổng số event trong khoảng from - to
                TotalStudentsParticipated = totalStudentsParticipated, // Gán tổng số sinh viên tham dự (distinct) trong khoảng thời gian
                TotalTickets = totalTickets, // Gán tổng số vé được tạo trong khoảng thời gian đã filter
                TotalCheckins = totalCheckins, // Gán tổng số lượt check-in thành công trong khoảng thời gian đã filter
                EventsByMonth = eventsByMonth, // Gán danh sách thống kê số event theo từng tháng
                AttendanceByMonth = attendanceByMonth // Gán danh sách thống kê số người tham dự theo từng tháng
            };

            return ApiResponse<SystemLevelReportDto>.SuccessResponse(dto); // Trả về ApiResponse thành công chứa dữ liệu báo cáo hệ thống
        }
    }
}
