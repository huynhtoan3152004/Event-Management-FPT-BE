using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(string eventId, bool includeRelations = false);
    Task<(IEnumerable<Event> Events, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? status = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? hallId = null,
        string? organizerId = null);
    Task<List<Event>> GetActiveEventsAsync();
    Task<List<Event>> GetActiveEventsByHallIdAsync(string hallId);
    Task AddAsync(Event @event);
    Task<Event> UpdateAsync(Event @event);
    Task<bool> DeleteAsync(string eventId);
    Task<bool> ExistsAsync(string hallId);
    Task<int> GetRegisteredCountAsync(string eventId);
    Task SaveChangesAsync();
}