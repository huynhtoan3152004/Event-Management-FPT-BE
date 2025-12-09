using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request.Hall;

public class GenerateSeatsRequestDto
{
    [Required(ErrorMessage = "Số hàng ghế là bắt buộc")]
    [Range(1, 50, ErrorMessage = "Số hàng phải từ 1-50")]
    public int Rows { get; set; }

    [Required(ErrorMessage = "Số ghế mỗi hàng là bắt buộc")]
    [Range(1, 100, ErrorMessage = "Số ghế mỗi hàng phải từ 1-100")]
    public int SeatsPerRow { get; set; }
}
