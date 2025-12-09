using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IntervalEventRegistrationService.DTOs.Request;

public class CreateEventRequest
{
    [Required(ErrorMessage = "Tiêu đề sự kiện là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Ngày diễn ra sự kiện là bắt buộc")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
    public TimeOnly EndTime { get; set; }

    [StringLength(500, ErrorMessage = "Địa điểm không được vượt quá 500 ký tự")]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? HallId { get; set; }

    [StringLength(50)]
    public string? ClubId { get; set; }

    [StringLength(200)]
    public string? ClubName { get; set; }

    [Required(ErrorMessage = "Tổng số ghế là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số ghế phải lớn hơn 0")]
    public int TotalSeats { get; set; }

    [Required(ErrorMessage = "Số hàng là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số hàng phải lớn hơn 0")]
    public int Rows { get; set; }

    [Required(ErrorMessage = "Số ghế mỗi hàng là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số ghế mỗi hàng phải lớn hơn 0")]
    public int SeatsPerRow { get; set; }

    public DateTime? RegistrationStart { get; set; }

    public DateTime? RegistrationEnd { get; set; }

    public string? Tags { get; set; }

    [Range(1, 10, ErrorMessage = "Số vé tối đa mỗi người phải từ 1-10")]
    public int MaxTicketsPerUser { get; set; } = 1;

    // File upload cho ảnh sự kiện
    public IFormFile? ImageFile { get; set; }

    // Danh sách Speaker IDs
    public List<string>? SpeakerIds { get; set; }
}

public class UpdateEventRequest
{
    [Required(ErrorMessage = "Tiêu đề sự kiện là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Ngày diễn ra sự kiện là bắt buộc")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
    public TimeOnly EndTime { get; set; }

    [StringLength(500, ErrorMessage = "Địa điểm không được vượt quá 500 ký tự")]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? HallId { get; set; }

    [StringLength(50)]
    public string? ClubId { get; set; }

    [StringLength(200)]
    public string? ClubName { get; set; }

    [Required(ErrorMessage = "Tổng số ghế là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số ghế phải lớn hơn 0")]
    public int TotalSeats { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số hàng không hợp lệ")]
    public int Rows { get; set; } = 0;

    [Range(0, int.MaxValue, ErrorMessage = "Số ghế mỗi hàng không hợp lệ")]
    public int SeatsPerRow { get; set; } = 0;

    public DateTime? RegistrationStart { get; set; }

    public DateTime? RegistrationEnd { get; set; }

    public string? Tags { get; set; }

    [Range(1, 10, ErrorMessage = "Số vé tối đa mỗi người phải từ 1-10")]
    public int MaxTicketsPerUser { get; set; } = 1;

    public IFormFile? ImageFile { get; set; }
}

public class EventFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? HallId { get; set; }
    public string? OrganizerId { get; set; }
}
