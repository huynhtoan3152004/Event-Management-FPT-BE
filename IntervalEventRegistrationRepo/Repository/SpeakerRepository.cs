using Microsoft.EntityFrameworkCore;
using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;

namespace IntervalEventRegistrationRepo.Repository;

public class SpeakerRepository : ISpeakerRepository
{
    private readonly ApplicationDbContext _context;

    public SpeakerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Speaker>> GetAllAsync(int pageNumber, int pageSize, string? search = null)
    {
        var query = _context.Speakers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(s => 
                s.Name.ToLower().Contains(search) ||
                (s.Title != null && s.Title.ToLower().Contains(search)) ||
                (s.Company != null && s.Company.ToLower().Contains(search)) ||
                (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null)
    {
        var query = _context.Speakers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(s => 
                s.Name.ToLower().Contains(search) ||
                (s.Title != null && s.Title.ToLower().Contains(search)) ||
                (s.Company != null && s.Company.ToLower().Contains(search)) ||
                (s.Email != null && s.Email.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<Speaker?> GetByIdAsync(string speakerId)
    {
        return await _context.Speakers
            .Include(s => s.EventSpeakers)
            .FirstOrDefaultAsync(s => s.SpeakerId == speakerId);
    }

    public async Task<Speaker?> GetByEmailAsync(string email)
    {
        return await _context.Speakers
            .FirstOrDefaultAsync(s => s.Email != null && s.Email.ToLower() == email.ToLower());
    }

    public async Task<Speaker> CreateAsync(Speaker speaker)
    {
        speaker.SpeakerId = Guid.NewGuid().ToString();
        speaker.CreatedAt = DateTime.UtcNow;
        
        _context.Speakers.Add(speaker);
        await _context.SaveChangesAsync();
        
        return speaker;
    }

    public async Task<Speaker> UpdateAsync(Speaker speaker)
    {
        speaker.UpdatedAt = DateTime.UtcNow;
        
        _context.Speakers.Update(speaker);
        await _context.SaveChangesAsync();
        
        return speaker;
    }

    public async Task<bool> DeleteAsync(string speakerId)
    {
        var speaker = await _context.Speakers.FindAsync(speakerId);
        
        if (speaker == null)
            return false;

        // Soft delete
        speaker.IsDeleted = true;
        speaker.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Event>> GetEventsBySpeakerIdAsync(string speakerId, int pageNumber, int pageSize)
    {
        return await _context.EventSpeakers
            .Where(es => es.SpeakerId == speakerId)
            .Include(es => es.Event)
            .Select(es => es.Event)
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetEventsCountBySpeakerIdAsync(string speakerId)
    {
        return await _context.EventSpeakers
            .Where(es => es.SpeakerId == speakerId)
            .Include(es => es.Event)
            .Where(es => !es.Event.IsDeleted)
            .CountAsync();
    }

    public async Task<bool> ExistsAsync(string speakerId)
    {
        return await _context.Speakers.AnyAsync(s => s.SpeakerId == speakerId);
    }
}
