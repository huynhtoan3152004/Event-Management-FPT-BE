using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;

namespace IntervalEventRegistrationRepo.Repository;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;

    public EventRepository(ApplicationDbContext context)
    {
        _context = context;
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
            .Where(e => !e.IsDeleted)
            .Include(e => e.Hall)
            .Include(e => e.Organizer)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(search) ||
                (e.Description != null && e.Description.ToLower().Contains(search)) ||
                (e.ClubName != null && e.ClubName.ToLower().Contains(search)) ||
                (e.Location != null && e.Location.ToLower().Contains(search)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status.ToLower());
        }

        // Date range filter
        if (dateFrom.HasValue)
        {
            query = query.Where(e => e.Date >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(e => e.Date <= dateTo.Value);
        }

        // Hall filter
        if (!string.IsNullOrWhiteSpace(hallId))
        {
            query = query.Where(e => e.HallId == hallId);
        }

        // Organizer filter
        if (!string.IsNullOrWhiteSpace(organizerId))
        {
            query = query.Where(e => e.OrganizerId == organizerId);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<Event?> GetByIdAsync(string eventId, bool includeRelations = false)
    {
        var query = _context.Events.Where(e => !e.IsDeleted);

        if (includeRelations)
        {
            query = query
                .Include(e => e.Hall)
                .Include(e => e.Organizer)
                .Include(e => e.EventSpeakers)
                    .ThenInclude(es => es.Speaker);
        }

        return await query.FirstOrDefaultAsync(e => e.EventId == eventId);
    }

    public async Task<Event> CreateAsync(Event eventEntity)
    {
        await _context.Events.AddAsync(eventEntity);
        await _context.SaveChangesAsync();
        return eventEntity;
    }

    public async Task<Event> UpdateAsync(Event eventEntity)
    {
        eventEntity.UpdatedAt = DateTime.UtcNow;
        _context.Events.Update(eventEntity);
        await _context.SaveChangesAsync();
        return eventEntity;
    }

    public async Task<bool> DeleteAsync(string eventId)
    {
        var eventEntity = await GetByIdAsync(eventId);
        if (eventEntity == null)
            return false;

        eventEntity.IsDeleted = true;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await _context.Events.AnyAsync(e => e.EventId == eventId && !e.IsDeleted);
    }

    public async Task<int> GetRegisteredCountAsync(string eventId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId && t.Status == "active")
            .CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
