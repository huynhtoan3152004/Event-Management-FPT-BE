using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ITicketCheckinRepository
{
    Task AddAsync(TicketCheckin checkin);
    Task<List<TicketCheckin>> GetByTicketIdAsync(string ticketId);
    Task<List<TicketCheckin>> GetByEventIdAsync(string eventId);
    Task SaveChangesAsync();
}
