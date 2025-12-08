using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Auth
{
    /// <summary>
    /// DTO request cho login local bằng email và password.
    /// Client sẽ gửi email + password plaintext lên API.
    /// API sẽ kiểm tra với PasswordHash trong DB.
    /// </summary>
    public class EmailPasswordLoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
