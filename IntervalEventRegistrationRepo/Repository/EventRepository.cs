using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntervalEventRegistrationRepo.Repository;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(string eventId, bool includeRelations = false)
    {
        var query = _context.Events.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(e => e.Hall)
                .Include(e => e.EventSpeakers)
                    .ThenInclude(es => es.Speaker);
        }

        return await query.FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
    }

    public async Task<(IEnumerable<Event> Events, int TotalCount)> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        string? status = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string? hallId = null,
        string? organizerId = null)
    {
        var query = _context.Events
            .Include(e => e.Hall)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => 
                e.Title.Contains(search) || 
                (e.Description != null && e.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(e => e.Date >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(e => e.Date <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(hallId))
        {
            query = query.Where(e => e.HallId == hallId);
        }

        if (!string.IsNullOrWhiteSpace(organizerId))
        {
            query = query.Where(e => e.OrganizerId == organizerId);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<List<Event>> GetActiveEventsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Events
            .Include(e => e.Hall)
            .Where(e => !e.IsDeleted 
                && e.Status == "published"
                && e.Date >= DateOnly.FromDateTime(now))
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<List<Event>> GetActiveEventsByHallIdAsync(string hallId)
    {
        var now = DateTime.UtcNow;
        return await _context.Events
            .Where(e => e.HallId == hallId
                && !e.IsDeleted
                && e.Status == "published"
                && e.Date >= DateOnly.FromDateTime(now))
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task AddAsync(Event @event)
    {
        await _context.Events.AddAsync(@event);
    }

    public async Task<Event> UpdateAsync(Event @event)
    {
        @event.UpdatedAt = DateTime.UtcNow;
        _context.Events.Update(@event);
        return @event;
    }

    public async Task<bool> DeleteAsync(string eventId)
    {
        var @event = await GetByIdAsync(eventId);
        if (@event != null)
        {
            @event.IsDeleted = true;
            @event.UpdatedAt = DateTime.UtcNow;
            _context.Events.Update(@event);
            return true;
        }
        return false;
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await _context.Events
            .AnyAsync(e => e.EventId == eventId && !e.IsDeleted);
    }

    public async Task<int> GetRegisteredCountAsync(string eventId)
    {
        var @event = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        return @event?.RegisteredCount ?? 0;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
