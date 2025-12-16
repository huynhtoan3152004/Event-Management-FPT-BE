using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request.Ticket;

/// <summary>
/// Request để check-out ticket (student leaving event)
/// </summary>
public class CheckoutTicketRequest
{
    [Required(ErrorMessage = "Mã QR code là bắt buộc")]
    public string QrCode { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
