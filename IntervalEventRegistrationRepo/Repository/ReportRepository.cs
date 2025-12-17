using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationRepo.Models.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ReportRepository> _logger;

        public ReportRepository(ApplicationDbContext dbContext, ILogger<ReportRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Event?> GetEventWithDetailsAsync(string eventId)
        {
            // Hàm này dùng để lấy 1 sự kiện kèm thông tin Hall và Organizer để hiển thị báo cáo chi tiết
            return await _dbContext.Events // Truy vấn bảng Events từ DbContext
                .Include(e => e.Hall) // Include Hall để lấy thêm tên và địa chỉ hội trường phục vụ hiển thị
                .Include(e => e.Organizer) // Include Organizer để lấy tên người/CLB tổ chức sự kiện
                .FirstOrDefaultAsync(e => e.EventId == eventId); // Lọc theo EventId và trả về sự kiện đầu tiên hoặc null nếu không có
        }

        public async Task<List<Ticket>> GetTicketsByEventAsync(string eventId, string? ticketStatusFilter = null)
        {
            // Hàm này dùng để lấy danh sách ticket của một sự kiện, cho phép filter thêm theo status nếu truyền vào
            var query = _dbContext.Tickets // Bắt đầu truy vấn từ bảng Tickets
                .Where(t => t.EventId == eventId); // Chỉ lấy những ticket thuộc về event có EventId tương ứng

            if (!string.IsNullOrWhiteSpace(ticketStatusFilter)) // Nếu có truyền status filter từ phía service/API
            {
                query = query.Where(t => t.Status == ticketStatusFilter); // Lọc thêm theo trường Status của ticket
            }

            return await query.ToListAsync(); // Thực thi truy vấn và trả về danh sách ticket
        }

        public async Task<List<TicketCheckin>> GetTicketCheckinsByEventAsync(string eventId)
        {
            // Hàm này dùng để lấy tất cả log check-in của một event thông qua join Ticket -> TicketCheckin
            var query = _dbContext.TicketCheckins // Bắt đầu truy vấn từ bảng TicketCheckins
                .Where(tc => _dbContext.Tickets // Với mỗi log check-in, kiểm tra tồn tại ticket trong bảng Tickets
                    .Any(t => t.TicketId == tc.TicketId && t.EventId == eventId)); // Điều kiện: ticket thuộc về event có EventId tương ứng

            return await query.ToListAsync(); // Thực thi truy vấn và trả về danh sách log check-in
        }

        public async Task<List<Event>> GetEventsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter)
        {
            // Hàm này dùng để lấy danh sách event phục vụ báo cáo toàn hệ thống với filter from-to + status
            var query = _dbContext.Events.AsQueryable(); // Tạo IQueryable từ bảng Events để có thể build điều kiện linh hoạt

            if (from.HasValue) // Nếu có ngày bắt đầu filter
            {
                var fromDateOnly = DateOnly.FromDateTime(from.Value.Date); // Chuyển DateTime về DateOnly để so sánh với cột Date trong bảng events
                query = query.Where(e => e.Date >= fromDateOnly); // Lọc những event có ngày diễn ra lớn hơn hoặc bằng from
            }

            if (to.HasValue) // Nếu có ngày kết thúc filter
            {
                var toDateOnly = DateOnly.FromDateTime(to.Value.Date); // Chuyển DateTime về DateOnly để so sánh với cột Date
                query = query.Where(e => e.Date <= toDateOnly); // Lọc những event có ngày diễn ra nhỏ hơn hoặc bằng to
            }

            if (!string.IsNullOrWhiteSpace(eventStatusFilter)) // Nếu có truyền filter theo trạng thái event
            {
                query = query.Where(e => e.Status == eventStatusFilter); // Lọc những event có Status trùng với giá trị filter
            }

            return await query.ToListAsync(); // Thực thi truy vấn và trả về danh sách event
        }

        public async Task<List<Ticket>> GetTicketsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter)
        {
            // Hàm này dùng để lấy danh sách ticket trong khoảng thời gian và theo trạng thái của event cha
            var query = _dbContext.Tickets // Bắt đầu truy vấn từ bảng Tickets
                .Include(t => t.Event) // Include Event để có thể lọc theo ngày và status của event
                .Where(t => t.Event != null); // Đảm bảo chỉ lấy những ticket có event hợp lệ

            if (from.HasValue) // Nếu có filter ngày bắt đầu
            {
                var fromDateOnly = DateOnly.FromDateTime(from.Value.Date); // Chuyển DateTime sang DateOnly để so với Event.Date
                query = query.Where(t => t.Event!.Date >= fromDateOnly); // Lọc những ticket thuộc event có ngày lớn hơn hoặc bằng from
            }

            if (to.HasValue) // Nếu có filter ngày kết thúc
            {
                var toDateOnly = DateOnly.FromDateTime(to.Value.Date); // Chuyển DateTime sang DateOnly để so với Event.Date
                query = query.Where(t => t.Event!.Date <= toDateOnly); // Lọc những ticket thuộc event có ngày nhỏ hơn hoặc bằng to
            }

            if (!string.IsNullOrWhiteSpace(eventStatusFilter)) // Nếu có filter theo trạng thái event
            {
                query = query.Where(t => t.Event!.Status == eventStatusFilter); // Lọc những ticket thuộc event có Status trùng với filter
            }

            return await query.ToListAsync(); // Thực thi truy vấn và trả về danh sách ticket
        }

        public async Task<List<TicketCheckin>> GetCheckinsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter)
        {
            IQueryable<TicketCheckin> query = _dbContext.TicketCheckins // Khởi tạo query từ bảng TicketCheckins để có thể build thêm điều kiện một cách linh hoạt
                .Include(tc => tc.Ticket) // Include thêm Ticket để có thể truy cập sang thông tin vé tương ứng với từng log check-in
                .ThenInclude(t => t!.Event); // Include tiếp Event từ Ticket để có thể filter theo ngày diễn ra và trạng thái của sự kiện

            query = query.Where(tc => tc.Ticket != null && tc.Ticket.Event != null); // Chỉ giữ lại những bản ghi check-in có Ticket và Event hợp lệ, tránh lỗi null khi truy cập

            if (from.HasValue) // Nếu có truyền giá trị ngày bắt đầu filter từ tham số from
            {
                DateOnly fromDateOnly = DateOnly.FromDateTime(from.Value.Date); // Chuyển DateTime sang DateOnly để so sánh với cột Date (kiểu DateOnly) trong bảng events
                query = query.Where(tc => tc.Ticket!.Event!.Date >= fromDateOnly); // Lọc các check-in thuộc những event có ngày diễn ra lớn hơn hoặc bằng from
            }

            if (to.HasValue) // Nếu có truyền giá trị ngày kết thúc filter từ tham số to
            {
                DateOnly toDateOnly = DateOnly.FromDateTime(to.Value.Date); // Chuyển DateTime sang DateOnly để so sánh với cột Date trong bảng events
                query = query.Where(tc => tc.Ticket!.Event!.Date <= toDateOnly); // Lọc các check-in thuộc những event có ngày diễn ra nhỏ hơn hoặc bằng to
            }

            if (!string.IsNullOrWhiteSpace(eventStatusFilter)) // Nếu có truyền filter theo trạng thái sự kiện (ví dụ: approved, published, completed)
            {
                query = query.Where(tc => tc.Ticket!.Event!.Status == eventStatusFilter); // Lọc các check-in thuộc những event có trạng thái trùng với giá trị filter được truyền vào
            }

            List<TicketCheckin> result = await query.ToListAsync(); // Thực thi query trên database và lấy toàn bộ danh sách bản ghi check-in đã được filter

            return result; // Trả về danh sách TicketCheckin thỏa mãn các điều kiện filter để service dùng cho báo cáo
        }


        public async Task<Dictionary<string, int>> GetTicketStatusSummaryForSystemReportAsync(DateTime? fromUtc, DateTime? toUtc, string? eventStatus)
        {
            DateOnly? fromDate = fromUtc.HasValue ? DateOnly.FromDateTime(fromUtc.Value) : null; // convert fromUtc sang DateOnly để filter theo Event.Date
            DateOnly? toDate = toUtc.HasValue ? DateOnly.FromDateTime(toUtc.Value) : null; // convert toUtc sang DateOnly để filter theo Event.Date

            string? normalizedEventStatus = string.IsNullOrWhiteSpace(eventStatus) ? null : eventStatus.Trim().ToLower(); // chuẩn hoá eventStatus để so sánh ổn định

            IQueryable<IntervalEventRegistrationRepo.Entities.Ticket> query = _dbContext.Tickets // bắt đầu từ bảng tickets (đã auto lọc IsDeleted nhờ global query filter)
                .AsNoTracking() // tối ưu performance vì chỉ đọc dữ liệu
                .Where(t => t.Event != null) // loại ticket có event bị soft-delete (do global filter làm navigation null)
                .Where(t => t.Status != null && t.Status.Trim() != ""); // loại status null/empty để summary không bị rác

            if (fromDate.HasValue) // chỉ filter khi có from
            {
                query = query.Where(t => t.Event!.Date >= fromDate.Value); // lọc theo ngày diễn ra sự kiện từ fromDate
            }

            if (toDate.HasValue) // chỉ filter khi có to
            {
                query = query.Where(t => t.Event!.Date <= toDate.Value); // lọc theo ngày diễn ra sự kiện đến toDate
            }

            if (normalizedEventStatus != null) // chỉ filter khi có eventStatus
            {
                query = query.Where(t => t.Event!.Status != null && t.Event!.Status.Trim().ToLower() == normalizedEventStatus); // lọc theo status của event
            }

            var items = await query // bắt đầu query tổng hợp
                .GroupBy(t => t.Status.Trim().ToLower()) // group theo ticket status đã chuẩn hoá
                .Select(g => new { Status = g.Key, Total = g.Count() }) // project ra status + tổng số ticket
                .OrderBy(x => x.Status) // sắp xếp để output ổn định cho UI
                .ToListAsync(); // thực thi query và lấy về list

            Dictionary<string, int> result = items.ToDictionary(x => x.Status, x => x.Total); // convert list -> dictionary để trả về service

            return result; // trả về summary status động
        }

        public async Task<Dictionary<string, int>> GetTicketStatusSummaryForEventReportAsync(string eventId)
        {
            string normalizedEventId = eventId.Trim(); // chuẩn hoá eventId tránh lỗi do khoảng trắng

            IQueryable<IntervalEventRegistrationRepo.Entities.Ticket> query = _dbContext.Tickets // lấy tickets theo event
                .AsNoTracking() // tối ưu vì chỉ đọc
                .Where(t => t.EventId == normalizedEventId) // filter đúng eventId
                .Where(t => t.Status != null && t.Status.Trim() != ""); // loại status null/empty

            var items = await query // bắt đầu tổng hợp
                .GroupBy(t => t.Status.Trim().ToLower()) // group theo ticket status động
                .Select(g => new { Status = g.Key, Total = g.Count() }) // lấy status + số lượng
                .OrderBy(x => x.Status) // sắp xếp ổn định
                .ToListAsync(); // thực thi query

            Dictionary<string, int> result = items.ToDictionary(x => x.Status, x => x.Total); // chuyển về dictionary

            return result; // trả về summary status động cho 1 event
        }

        public async Task<int> CountEventsByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Đếm event theo khoảng ngày diễn ra
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> query = BuildEventsQuery(fromDate, toDate); // Tạo query event đã filter theo from/to (nếu có)

                int total = await query.CountAsync(cancellationToken); // Đếm số event thỏa điều kiện
                return total; // Trả về tổng số event
            }
            catch (Exception ex) // Bắt mọi exception để log
            {
                _logger.LogError(ex, "CountEventsByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm tham số
                throw; // Ném lỗi lên service để controller xử lý response
            }
        }

        public async Task<int> CountTicketsByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Đếm tổng ticket theo khoảng ngày event
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> eventsQuery = BuildEventsQuery(fromDate, toDate); // Lấy query event theo khoảng ngày

                int total = await (from t in _dbContext.Tickets.AsNoTracking() // Query ticket chỉ đọc để nhanh hơn
                                   join e in eventsQuery on t.EventId equals e.EventId // Join ticket với event đã filter
                                   select t.TicketId) // Chỉ select 1 field để count nhẹ hơn
                                  .CountAsync(cancellationToken); // Đếm số ticket

                return total; // Trả về tổng ticket (tổng lượt đăng ký)
            }
            catch (Exception ex) // Bắt exception để log
            {
                _logger.LogError(ex, "CountTicketsByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm input
                throw; // Ném lỗi lên service
            }
        }

        public async Task<int> CountParticipatedTicketsByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Đếm số ticket tham gia theo status fix cứng
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> eventsQuery = BuildEventsQuery(fromDate, toDate); // Lấy query event theo khoảng ngày

                int total = await (from t in _dbContext.Tickets.AsNoTracking() // Query ticket chỉ đọc
                                   join e in eventsQuery on t.EventId equals e.EventId // Join ticket với event đã filter
                                   where t.Status == "checked-in" // Fix cứng status tham gia 1
                                      || t.Status == "completed" // Fix cứng status tham gia 2
                                      || t.Status == "abandoned" // Fix cứng status tham gia 3
                                   select t.TicketId) // Select TicketId để count
                                  .CountAsync(cancellationToken); // Đếm số ticket tham gia

                return total; // Trả về số người tham gia
            }
            catch (Exception ex) // Bắt exception để log
            {
                _logger.LogError(ex, "CountParticipatedTicketsByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi chi tiết
                throw; // Ném lỗi lên service
            }
        }

        public async Task<List<MonthlyAttendanceRawData>> GetMonthlyAttendanceByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Lấy thống kê theo tháng
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> eventsQuery = BuildEventsQuery(fromDate, toDate); // Query event theo khoảng ngày

                List<MonthlyAttendanceRawData> data =
                    await (from t in _dbContext.Tickets.AsNoTracking() // Query ticket chỉ đọc
                           join e in eventsQuery on t.EventId equals e.EventId // Join ticket với event đã filter
                           select new // Project sang object trung gian để group theo month
                           {
                               EventYear = e.Date.Year, // Lấy năm từ ngày event
                               EventMonth = e.Date.Month, // Lấy tháng từ ngày event
                               TicketStatus = t.Status // Lấy status ticket để tính participated
                           })
                          .GroupBy(x => new { x.EventYear, x.EventMonth }) // Group theo năm-tháng
                          .Select(g => new MonthlyAttendanceRawData // Map sang DTO raw
                          {
                              Year = g.Key.EventYear, // Gán năm
                              Month = g.Key.EventMonth, // Gán tháng
                              TotalTickets = g.Count(), // Tổng ticket trong tháng
                              ParticipatedTickets = g.Count(x => x.TicketStatus == "checked-in" // Đếm status tham gia 1
                                                          || x.TicketStatus == "completed" // Đếm status tham gia 2
                                                          || x.TicketStatus == "abandoned"), // Đếm status tham gia 3
                              AbandonedTickets = g.Count(x => x.TicketStatus == "abandoned") // Đếm check-in chưa check-out trong tháng
                          })
                          .OrderBy(x => x.Year) // Sắp xếp theo năm tăng dần
                          .ThenBy(x => x.Month) // Sắp xếp theo tháng tăng dần
                          .ToListAsync(cancellationToken); // Execute query

                return data; // Trả về list thống kê theo tháng
            }
            catch (Exception ex) // Bắt exception để log
            {
                _logger.LogError(ex, "GetMonthlyAttendanceByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi
                throw; // Ném lỗi lên service
            }
        }

        public async Task<List<EventAttendanceRawData>> GetEventsAttendanceByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Lấy danh sách event + số liệu
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> eventsQuery = BuildEventsQuery(fromDate, toDate); // Query event theo khoảng ngày

                List<EventAttendanceRawData> data =
                    await (from e in eventsQuery.AsNoTracking() // Query event chỉ đọc
                           join t in _dbContext.Tickets.AsNoTracking() on e.EventId equals t.EventId into tg // Left join sang ticket để event không có ticket vẫn lên report
                           from t in tg.DefaultIfEmpty() // DefaultIfEmpty để thành left join
                           select new // Project sang object trung gian để group theo event
                           {
                               e.EventId, // Mang theo EventId
                               e.Title, // Mang theo Title
                               e.Date, // Mang theo Date
                               TicketId = t != null ? t.TicketId : null, // Nếu không có ticket thì null để count đúng
                               TicketStatus = t != null ? t.Status : null // Nếu không có ticket thì null để tránh crash
                           })
                          .GroupBy(x => new { x.EventId, x.Title, x.Date }) // Group theo từng event
                          .Select(g => new EventAttendanceRawData // Map sang DTO raw
                          {
                              EventId = g.Key.EventId, // Gán EventId
                              Title = g.Key.Title, // Gán tên event
                              Date = g.Key.Date, // Gán ngày event
                              TotalTickets = g.Count(x => x.TicketId != null), // Đếm ticket khác null
                              ParticipatedTickets = g.Count(x => x.TicketId != null // Chỉ count khi có ticket
                                                          && (x.TicketStatus == "checked-in" // Status tham gia 1
                                                              || x.TicketStatus == "completed" // Status tham gia 2
                                                              || x.TicketStatus == "abandoned")), // Status tham gia 3
                              AbandonedTickets = g.Count(x => x.TicketId != null // Chỉ count khi có ticket
                                                          && x.TicketStatus == "abandoned") // Đếm check-in chưa check-out theo event
                          })
                          .OrderBy(x => x.Date) // Sắp xếp theo ngày diễn ra tăng dần
                          .ToListAsync(cancellationToken); // Execute query

                return data; // Trả về list event + số liệu
            }
            catch (Exception ex) // Bắt exception để log
            {
                _logger.LogError(ex, "GetEventsAttendanceByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi
                throw; // Ném lỗi lên service
            }
        }

        private IQueryable<Event> BuildEventsQuery(DateOnly? fromDate, DateOnly? toDate) // Helper build query event theo from/to (nullable)
        {
            IQueryable<Event> query = _dbContext.Events.AsQueryable(); // Tạo queryable event (đã có global filter soft delete)

            if (fromDate.HasValue) // Nếu có fromDate thì mới lọc
            {
                query = query.Where(e => e.Date >= fromDate.Value); // Lọc event có ngày >= fromDate
            }

            if (toDate.HasValue) // Nếu có toDate thì mới lọc
            {
                query = query.Where(e => e.Date <= toDate.Value); // Lọc event có ngày <= toDate
            }

            return query; // Trả về query đã filter
        }

        public async Task<int> CountAbandonedTicketsByEventDateAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default) // Đếm số ticket abandoned theo khoảng ngày event
        {
            try // Bọc try để log lỗi khi query DB
            {
                IQueryable<Event> eventsQuery = BuildEventsQuery(fromDate, toDate); // Lấy query event theo khoảng ngày

                int total = await (from t in _dbContext.Tickets.AsNoTracking() // Query ticket chỉ đọc
                                   join e in eventsQuery on t.EventId equals e.EventId // Join ticket với event đã filter
                                   where t.Status == "abandoned" // Fix cứng abandoned = check-in chưa check-out
                                   select t.TicketId) // Select TicketId để count nhẹ
                                  .CountAsync(cancellationToken); // Đếm số abandoned

                return total; // Trả về số ticket abandoned
            }
            catch (Exception ex) // Bắt exception để log
            {
                _logger.LogError(ex, "CountAbandonedTicketsByEventDateAsync failed. fromDate={FromDate}, toDate={ToDate}", fromDate, toDate); // Log lỗi kèm input
                throw; // Ném lỗi lên service
            }
        }


    }
}
