using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;

namespace IntervalEventRegistrationRepo.Repository;

public class HallRepository : IHallRepository
{
    private readonly ApplicationDbContext _context;

    public HallRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Hall> Halls, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? status = null,
        int? minCapacity = null,
        int? maxCapacity = null)
    {
        var query = _context.Halls
            .Include(h => h.Seats)
            .Where(h => !h.IsDeleted)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(h =>
                h.Name.ToLower().Contains(search) ||
                (h.Address != null && h.Address.ToLower().Contains(search)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(h => h.Status == status.ToLower());
        }

        // Capacity filter
        if (minCapacity.HasValue)
        {
            query = query.Where(h => h.Capacity >= minCapacity.Value);
        }

        if (maxCapacity.HasValue)
        {
            query = query.Where(h => h.Capacity <= maxCapacity.Value);
        }

        var totalCount = await query.CountAsync();

        var halls = await query
            .OrderBy(h => h.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (halls, totalCount);
    }

    public async Task<Hall?> GetByIdAsync(string hallId, bool includeRelations = false)
    {
        var query = _context.Halls
            .Where(h => !h.IsDeleted)
            .AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(h => h.Seats)
                .Include(h => h.Events.Where(e => !e.IsDeleted));
        }

        return await query.FirstOrDefaultAsync(h => h.HallId == hallId);
    }

    public async Task<Hall> CreateAsync(Hall hall)
    {
        hall.CreatedAt = DateTime.UtcNow;
        hall.IsDeleted = false;
        hall.Status = "active";

        await _context.Halls.AddAsync(hall);
        await _context.SaveChangesAsync();
        return hall;
    }

    public async Task<Hall> UpdateAsync(Hall hall)
    {
        hall.UpdatedAt = DateTime.UtcNow;
        _context.Halls.Update(hall);
        await _context.SaveChangesAsync();
        return hall;
    }

    public async Task<bool> DeleteAsync(string hallId)
    {
        var hall = await GetByIdAsync(hallId);
        if (hall == null)
            return false;

        // Check if hall has active events
        var hasActiveEvents = await _context.Events
            .AnyAsync(e => e.HallId == hallId && !e.IsDeleted && e.Status != "cancelled");

        if (hasActiveEvents)
            return false; // Cannot delete hall with active events

        hall.IsDeleted = true;
        hall.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string hallId)
    {
        return await _context.Halls.AnyAsync(h => h.HallId == hallId && !h.IsDeleted);
    }

    public async Task<int> GetActiveEventsCountAsync(string hallId)
    {
        return await _context.Events
            .Where(e => e.HallId == hallId && !e.IsDeleted && e.Status != "cancelled")
            .CountAsync();
    }

    public async Task<bool> IsHallAvailableAsync(string hallId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var conflictingEvents = await GetConflictingEventsAsync(hallId, date, startTime, endTime);
        return !conflictingEvents.Any();
    }

    public async Task<List<Event>> GetConflictingEventsAsync(string hallId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        return await _context.Events
            .Where(e => e.HallId == hallId &&
                        !e.IsDeleted &&
                        e.Status != "cancelled" &&
                        e.Date == date &&
                        ((e.StartTime < endTime && e.EndTime > startTime))) // Overlap condition
            .ToListAsync();
    }
}
