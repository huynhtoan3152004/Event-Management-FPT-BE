using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request.Hall;

public class CreateHallRequestDto
{
    [Required(ErrorMessage = "Tên hội trường là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên không được vượt quá 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Địa điểm không được vượt quá 500 ký tự")]
    public string? Location { get; set; }

    [Required(ErrorMessage = "Sức chứa là bắt buộc")]
    [Range(1, 10000, ErrorMessage = "Sức chứa phải từ 1-10000")]
    public int Capacity { get; set; }

    public string? Facilities { get; set; } // JSON string: {"projector":true,"mic":true,"wifi":true}
}
