using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request.Hall;

public class CheckAvailabilityRequestDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
