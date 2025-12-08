using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request.Hall;

public class GenerateSeatsRequestDto
{
    [Required]
    [Range(1, 26, ErrorMessage = "Số hàng phải từ 1-26 (A-Z)")]
    public int Rows { get; set; } // Số hàng ghế (A, B, C...)

    [Required]
    [Range(1, 100, ErrorMessage = "Số ghế mỗi hàng phải từ 1-100")]
    public int SeatsPerRow { get; set; } // Số ghế mỗi hàng (1, 2, 3...)

    [StringLength(10)]
    public string Prefix { get; set; } = ""; // Prefix cho seat code (optional: VIP-, REG-)

    [RegularExpression("^(regular|vip|wheelchair)$")]
    public string SeatType { get; set; } = "regular";
}
