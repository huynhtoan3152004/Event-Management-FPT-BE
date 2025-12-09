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

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
