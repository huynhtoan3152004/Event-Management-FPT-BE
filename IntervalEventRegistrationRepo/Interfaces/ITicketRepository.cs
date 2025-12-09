using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByTicketCodeAsync(string ticketCode);
    Task<Ticket?> GetByIdAsync(string ticketId);
    Task<Ticket?> GetActiveByEventAndStudentAsync(string eventId, string studentId);
    Task AddAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task<List<Ticket>> GetByEventIdAsync(string eventId);
    Task<List<Ticket>> GetByStudentIdAsync(string studentId);
    Task<int> CountActiveByEventAsync(string eventId);
    Task SaveChangesAsync();
}
