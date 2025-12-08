using IntervalEventRegistrationService.DTOs.Common;

namespace IntervalEventRegistrationService.DTOs.Request.Hall;

public class HallFilterRequest : PaginationRequest
{
    public string? Status { get; set; } // active, maintenance, unavailable
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }
}
