using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface IEventRepository
{
    Task<(IEnumerable<Event> Events, int TotalCount)> GetAllAsync(
        int pageNumber, 
        int pageSize, 
        string? search = null,
        string? status = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? hallId = null,
        string? organizerId = null);

    Task<Event?> GetByIdAsync(string eventId, bool includeRelations = false);
    Task<Event> CreateAsync(Event eventEntity);
    Task<Event> UpdateAsync(Event eventEntity);
    Task<bool> DeleteAsync(string eventId);
    Task<bool> ExistsAsync(string eventId);
    Task<int> GetRegisteredCountAsync(string eventId);
    Task SaveChangesAsync();
}