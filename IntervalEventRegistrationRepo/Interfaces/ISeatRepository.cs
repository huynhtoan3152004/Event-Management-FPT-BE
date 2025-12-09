using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(string seatId);
    Task<List<Seat>> GetByHallIdAsync(string hallId);
    Task<int> CountByHallIdAsync(string hallId);
    Task<int> CountAvailableSeatsAsync(string hallId);
    Task AddAsync(Seat seat);
    Task AddRangeAsync(List<Seat> seats);
    Task UpdateAsync(Seat seat);
    Task DeleteAsync(string seatId);
    Task SaveChangesAsync();
}
