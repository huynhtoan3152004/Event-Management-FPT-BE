using IntervalEventRegistrationRepo.Entities;

namespace IntervalEventRegistrationRepo.Interfaces;

public interface ISeatRepository
{
    Task<List<Seat>> GetSeatsByHallIdAsync(string hallId, string? seatType = null, bool? isActive = null);
    Task<Seat?> GetByIdAsync(string seatId);
    Task<List<Seat>> CreateBulkAsync(List<Seat> seats);
    Task<bool> DeleteSeatsByHallIdAsync(string hallId);
    Task<int> GetTotalSeatsCountAsync(string hallId);
}
