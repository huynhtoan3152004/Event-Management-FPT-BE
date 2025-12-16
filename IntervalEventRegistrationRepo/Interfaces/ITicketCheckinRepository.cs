using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ITicketCheckinRepository
{
    Task AddAsync(TicketCheckin checkin);
    Task<List<TicketCheckin>> GetByTicketIdAsync(string ticketId);
    Task<List<TicketCheckin>> GetByEventIdAsync(string eventId);
    Task SaveChangesAsync();
    
    /// <summary>
    /// Get latest active check-in record (checkout_time = NULL)
    /// </summary>
    Task<TicketCheckin?> GetActiveCheckinByTicketIdAsync(string ticketId);
    
    /// <summary>
    /// Update checkout time for a check-in record
    /// </summary>
    Task UpdateCheckoutTimeAsync(string checkinId, DateTime checkoutTime, string? notes = null);
    
    /// <summary>
    /// Count total check-ins for an event
    /// </summary>
    Task<int> CountByEventIdAsync(string eventId);
    
    /// <summary>
    /// Count check-outs (checkout_time != NULL) for an event
    /// </summary>
    Task<int> CountCheckedOutByEventIdAsync(string eventId);
}
