using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntervalEventRegistrationRepo.Repository;

public class TicketCheckinRepository : ITicketCheckinRepository
{
    private readonly ApplicationDbContext _context;

    public TicketCheckinRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TicketCheckin checkin)
    {
        await _context.TicketCheckins.AddAsync(checkin);
    }

    public async Task<List<TicketCheckin>> GetByTicketIdAsync(string ticketId)
    {
        return await _context.TicketCheckins
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CheckinTime)
            .ToListAsync();
    }

    public async Task<List<TicketCheckin>> GetByEventIdAsync(string eventId)
    {
        return await _context.TicketCheckins
            .Include(tc => tc.Ticket!)
                .ThenInclude(t => t.Student)
            .Include(tc => tc.Ticket!)
                .ThenInclude(t => t.Seat)
            .Where(tc => tc.Ticket != null && tc.Ticket.EventId == eventId)
            .OrderByDescending(tc => tc.CheckinTime)
            .ToListAsync();
    }

    public async Task<TicketCheckin?> GetActiveCheckinByTicketIdAsync(string ticketId)
    {
        return await _context.TicketCheckins
            .Include(tc => tc.Ticket)
                .ThenInclude(t => t!.Student)
            .Include(tc => tc.Ticket)
                .ThenInclude(t => t!.Event)
            .Include(tc => tc.Ticket)
                .ThenInclude(t => t!.Seat)
            .Include(tc => tc.Staff)
            .Where(tc => tc.TicketId == ticketId && tc.CheckoutTime == null)
            .OrderByDescending(tc => tc.CheckinTime)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateCheckoutTimeAsync(string checkinId, DateTime checkoutTime, string? notes = null)
    {
        var checkin = await _context.TicketCheckins.FindAsync(checkinId);
        
        if (checkin != null)
        {
            checkin.CheckoutTime = checkoutTime;
            checkin.Status = "checked-out";
            if (!string.IsNullOrEmpty(notes))
            {
                checkin.Notes = notes;
            }
            
            _context.TicketCheckins.Update(checkin);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountByEventIdAsync(string eventId)
    {
        return await _context.TicketCheckins
            .Where(tc => tc.Ticket != null && tc.Ticket.EventId == eventId)
            .CountAsync();
    }

    public async Task<int> CountCheckedOutByEventIdAsync(string eventId)
    {
        return await _context.TicketCheckins
            .Where(tc => tc.Ticket != null && 
                         tc.Ticket.EventId == eventId && 
                         tc.CheckoutTime != null)
            .CountAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
