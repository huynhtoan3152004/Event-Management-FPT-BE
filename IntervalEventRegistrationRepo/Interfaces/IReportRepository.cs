using IntervalEventRegistrationRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Interfaces
{
    public interface IReportRepository
    {
        // Hàm này dùng để lấy 1 sự kiện kèm theo Hall và Organizer phục vụ báo cáo chi tiết 1 event
        Task<Event?> GetEventWithDetailsAsync(string eventId);

        // Hàm này dùng để lấy toàn bộ ticket của một event, có thể filter thêm theo status nếu cần
        Task<List<Ticket>> GetTicketsByEventAsync(string eventId, string? ticketStatusFilter = null);

        // Hàm này dùng để lấy tất cả log check-in của một event (dùng cho biểu đồ, đếm failed/success)
        Task<List<TicketCheckin>> GetTicketCheckinsByEventAsync(string eventId);

        // Hàm này dùng cho báo cáo toàn hệ thống: lấy danh sách event theo khoảng ngày + status
        Task<List<Event>> GetEventsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter);

        // Hàm này dùng cho báo cáo toàn hệ thống: lấy ticket theo khoảng ngày + status event
        Task<List<Ticket>> GetTicketsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter);

        // Hàm này dùng cho báo cáo toàn hệ thống: lấy check-in theo khoảng ngày + status event
        Task<List<TicketCheckin>> GetCheckinsForSystemReportAsync(DateTime? from, DateTime? to, string? eventStatusFilter);
    }
}
