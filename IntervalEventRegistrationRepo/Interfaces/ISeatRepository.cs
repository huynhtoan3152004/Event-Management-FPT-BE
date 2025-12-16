using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(string seatId);
    Task<List<Seat>> GetByHallIdAsync(string hallId);
    Task<List<Seat>> GetByEventIdAsync(string eventId);
    Task<int> CountByHallIdAsync(string hallId);
    Task<int> CountAvailableSeatsAsync(string hallId);
    Task AddAsync(Seat seat);
    Task AddRangeAsync(List<Seat> seats);
    Task UpdateAsync(Seat seat);
    Task DeleteAsync(string seatId);
    Task DeleteByEventIdAsync(string eventId);
    Task SaveChangesAsync();
    
    /// <summary>
    /// Get all seats for a Hall (template view)
    /// </summary>
    Task<IEnumerable<Seat>> GetSeatsByHallIdAsync(string hallId, bool includeDeleted = false);
    
    /// <summary>
    /// Get seat map for an Event with status and occupant info
    /// </summary>
    Task<IEnumerable<Seat>> GetEventSeatMapAsync(string eventId, bool includeOccupant = false);
    
    /// <summary>
    /// Get seat statistics by status for an Event
    /// </summary>
    Task<Dictionary<string, int>> GetSeatStatisticsByEventAsync(string eventId);
    
    /// <summary>
    /// Check seat availability in real-time
    /// </summary>
    Task<bool> IsSeatAvailableAsync(string seatId);
    
    /// <summary>
    /// Get seats grouped by rows for an Event
    /// </summary>
    Task<Dictionary<int, List<Seat>>> GetSeatsGroupedByRowAsync(string eventId);
    
    /// <summary>
    /// Get seat detail with occupant information by seat ID
    /// </summary>
    Task<Seat?> GetSeatDetailAsync(string seatId);
}
