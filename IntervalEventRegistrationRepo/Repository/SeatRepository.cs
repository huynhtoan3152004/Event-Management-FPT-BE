using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;

namespace IntervalEventRegistrationRepo.Repository;

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _context;

    public SeatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetSeatsByHallIdAsync(string hallId, string? seatType = null, bool? isActive = null)
    {
        var query = _context.Seats
            .Where(s => s.HallId == hallId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(seatType))
        {
            query = query.Where(s => s.Section == seatType.ToLower());
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
                query = query.Where(s => !s.IsDeleted && s.Status == "available");
            else
                query = query.Where(s => s.IsDeleted || s.Status != "available");
        }

        return await query
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<Seat?> GetByIdAsync(string seatId)
    {
        return await _context.Seats.FindAsync(seatId);
    }

    public async Task<List<Seat>> CreateBulkAsync(List<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
        await _context.SaveChangesAsync();
        return seats;
    }

    public async Task<bool> DeleteSeatsByHallIdAsync(string hallId)
    {
        var seats = await _context.Seats.Where(s => s.HallId == hallId).ToListAsync();
        _context.Seats.RemoveRange(seats);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalSeatsCountAsync(string hallId)
    {
        return await _context.Seats.CountAsync(s => s.HallId == hallId && !s.IsDeleted && s.Status == "available");
    }
}
