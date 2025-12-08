using System.ComponentModel.DataAnnotations;

namespace IntervalEventRegistrationService.DTOs.Request;

public class CreateSpeakerRequest
{
    [Required(ErrorMessage = "Tên speaker là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Chức danh không được vượt quá 100 ký tự")]
    public string? Title { get; set; }

    [StringLength(200, ErrorMessage = "Công ty không được vượt quá 200 ký tự")]
    public string? Company { get; set; }

    public string? Bio { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? Phone { get; set; }

    [Url(ErrorMessage = "URL LinkedIn không hợp lệ")]
    [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
    public string? LinkedinUrl { get; set; }

    [Url(ErrorMessage = "URL Avatar không hợp lệ")]
    [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
    public string? AvatarUrl { get; set; }
}

public class UpdateSpeakerRequest
{
    [Required(ErrorMessage = "Tên speaker là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Chức danh không được vượt quá 100 ký tự")]
    public string? Title { get; set; }

    [StringLength(200, ErrorMessage = "Công ty không được vượt quá 200 ký tự")]
    public string? Company { get; set; }

    public string? Bio { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    public string? Phone { get; set; }

    [Url(ErrorMessage = "URL LinkedIn không hợp lệ")]
    [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
    public string? LinkedinUrl { get; set; }

    [Url(ErrorMessage = "URL Avatar không hợp lệ")]
    [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
    public string? AvatarUrl { get; set; }
}
