using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntervalEventRegistrationRepo.Repository;

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _context;

    public SeatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Seat?> GetByIdAsync(string seatId)
    {
        return await _context.Seats
            .Include(s => s.Hall)
            .FirstOrDefaultAsync(s => s.SeatId == seatId && !s.IsDeleted);
    }

    public async Task<List<Seat>> GetByHallIdAsync(string hallId)
    {
        return await _context.Seats
            .Where(s => s.HallId == hallId && !s.IsDeleted)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<List<Seat>> GetByEventIdAsync(string eventId)
    {
        return await _context.Seats
            .Where(s => s.EventId == eventId && !s.IsDeleted)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<int> CountByHallIdAsync(string hallId)
    {
        return await _context.Seats
            .CountAsync(s => s.HallId == hallId && !s.IsDeleted);
    }

    public async Task<int> CountAvailableSeatsAsync(string hallId)
    {
        return await _context.Seats
            .CountAsync(s => s.HallId == hallId 
                && !s.IsDeleted 
                && s.Status == "available");
    }

    public async Task AddAsync(Seat seat)
    {
        await _context.Seats.AddAsync(seat);
    }

    public async Task AddRangeAsync(List<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
    }

    public async Task UpdateAsync(Seat seat)
    {
        seat.UpdatedAt = DateTime.UtcNow;
        _context.Seats.Update(seat);
    }

    public async Task DeleteAsync(string seatId)
    {
        var seat = await GetByIdAsync(seatId);
        if (seat != null)
        {
            seat.IsDeleted = true;
            seat.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(seat);
        }
    }

    public async Task DeleteByEventIdAsync(string eventId)
    {
        var seats = await _context.Seats
            .Where(s => s.EventId == eventId && !s.IsDeleted)
            .ToListAsync();
        
        foreach (var seat in seats)
        {
            seat.IsDeleted = true;
            seat.UpdatedAt = DateTime.UtcNow;
        }
        
        _context.Seats.UpdateRange(seats);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Seat>> GetSeatsByHallIdAsync(string hallId, bool includeDeleted = false)
    {
        var query = _context.Seats
            .Where(s => s.HallId == hallId);
        
        if (!includeDeleted)
        {
            query = query.Where(s => !s.IsDeleted);
        }
        
        return await query
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Seat>> GetEventSeatMapAsync(string eventId, bool includeOccupant = false)
    {
        var query = _context.Seats
            .Where(s => s.EventId == eventId && !s.IsDeleted);
        
        if (includeOccupant)
        {
            query = query
                .Include(s => s.Event)
                .Include(s => s.Hall);
        }
        
        return await query
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetSeatStatisticsByEventAsync(string eventId)
    {
        var seats = await _context.Seats
            .Where(s => s.EventId == eventId && !s.IsDeleted)
            .ToListAsync();
        
        return new Dictionary<string, int>
        {
            ["total"] = seats.Count,
            ["available"] = seats.Count(s => s.Status == "available"),
            ["reserved"] = seats.Count(s => s.Status == "reserved"),
            ["occupied"] = seats.Count(s => s.Status == "occupied")
        };
    }

    public async Task<bool> IsSeatAvailableAsync(string seatId)
    {
        var seat = await _context.Seats
            .Where(s => s.SeatId == seatId && !s.IsDeleted)
            .Select(s => new { s.Status })
            .FirstOrDefaultAsync();
        
        return seat?.Status == "available";
    }

    public async Task<Dictionary<int, List<Seat>>> GetSeatsGroupedByRowAsync(string eventId)
    {
        var seats = await _context.Seats
            .Where(s => s.EventId == eventId && !s.IsDeleted)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
        
        // Parse row label to get row number (A=1, B=2, etc.)
        return seats
            .GroupBy(s => s.RowLabel)
            .ToDictionary(
                g => g.Key.Length == 1 ? g.Key[0] - 'A' + 1 : 0,
                g => g.ToList()
            );
    }

    public async Task<Seat?> GetSeatDetailAsync(string seatId)
    {
        return await _context.Seats
            .Include(s => s.Tickets.Where(t => t.Status != "cancelled"))
                .ThenInclude(t => t.Student)
            .Include(s => s.Tickets)
                .ThenInclude(t => t.TicketCheckins)
                    .ThenInclude(tc => tc.Staff) // Staff who checked in
            .Where(s => s.SeatId == seatId && !s.IsDeleted)
            .FirstOrDefaultAsync();
    }
}
