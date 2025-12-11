using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public ReportRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

    }
}
