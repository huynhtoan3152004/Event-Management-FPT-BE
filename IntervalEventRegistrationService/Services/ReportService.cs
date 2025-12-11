using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Reports;
using IntervalEventRegistrationService.DTOs.Response.Reports;
using IntervalEventRegistrationService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
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
