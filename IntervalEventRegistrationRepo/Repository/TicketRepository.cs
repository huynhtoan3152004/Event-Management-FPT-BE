using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntervalEventRegistrationRepo.Repository;

public class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByTicketCodeAsync(string ticketCode)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.TicketCode == ticketCode && !t.IsDeleted);
    }

    public async Task<Ticket?> GetByIdAsync(string ticketId)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.Seat)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId && !t.IsDeleted);
    }

    public async Task<Ticket?> GetActiveByEventAndStudentAsync(string eventId, string studentId)
    {
        return await _context.Tickets
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.StudentId == studentId && t.Status == "active" && !t.IsDeleted);
    }

    public async Task AddAsync(Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        ticket.UpdatedAt = DateTime.UtcNow;
        _context.Tickets.Update(ticket);
        await Task.CompletedTask;
    }

    public async Task<List<Ticket>> GetByEventIdAsync(string eventId)
    {
        return await _context.Tickets
            .Include(t => t.Student)  // ✅ Include Student for name
            .Include(t => t.Seat)     // ✅ Include Seat for seat number
            .Where(t => t.EventId == eventId && !t.IsDeleted)
            .OrderBy(t => t.RegisteredAt)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetByStudentIdAsync(string studentId)
    {
        return await _context.Tickets
            .Where(t => t.StudentId == studentId && !t.IsDeleted)
            .OrderByDescending(t => t.RegisteredAt)
            .ToListAsync();
    }

    public async Task<int> CountActiveByEventAsync(string eventId)
    {
        return await _context.Tickets.CountAsync(t => t.EventId == eventId && t.Status == "active" && !t.IsDeleted);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
