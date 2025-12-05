using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ISpeakerRepository
{
    Task<IEnumerable<Speaker>> GetAllAsync(int pageNumber, int pageSize, string? search = null);
    Task<int> GetTotalCountAsync(string? search = null);
    Task<Speaker?> GetByIdAsync(string speakerId);
    Task<Speaker?> GetByEmailAsync(string email);
    Task<Speaker> CreateAsync(Speaker speaker);
    Task<Speaker> UpdateAsync(Speaker speaker);
    Task<bool> DeleteAsync(string speakerId);
    Task<IEnumerable<Event>> GetEventsBySpeakerIdAsync(string speakerId, int pageNumber, int pageSize);
    Task<int> GetEventsCountBySpeakerIdAsync(string speakerId);
    Task<bool> ExistsAsync(string speakerId);
}
