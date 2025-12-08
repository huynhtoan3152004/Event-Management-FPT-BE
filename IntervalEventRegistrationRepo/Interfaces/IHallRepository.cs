using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface IHallRepository
{
    Task<(IEnumerable<Hall> Halls, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? status = null,
        int? minCapacity = null,
        int? maxCapacity = null);

    Task<Hall?> GetByIdAsync(string hallId, bool includeRelations = false);
    Task<Hall> CreateAsync(Hall hall);
    Task<Hall> UpdateAsync(Hall hall);
    Task<bool> DeleteAsync(string hallId);
    Task<bool> ExistsAsync(string hallId);
    Task<int> GetActiveEventsCountAsync(string hallId);
    Task<bool> IsHallAvailableAsync(string hallId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
    Task<List<Event>> GetConflictingEventsAsync(string hallId, DateOnly date, TimeOnly startTime, TimeOnly endTime);
}
