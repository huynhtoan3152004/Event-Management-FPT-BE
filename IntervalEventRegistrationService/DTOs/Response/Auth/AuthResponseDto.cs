using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Auth
{
    /// <summary>
    /// DTO trả về sau khi login thành công (Google / sau này Local).
    /// Dùng để trả JWT và một số thông tin cơ bản của user.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// Access Token (JWT) mà client sẽ dùng cho các request tiếp theo.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Thời điểm token hết hạn (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Id của user trong hệ thống (UserId).
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Email đã xác thực (lấy từ Google / trong DB).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Tên hiển thị của user.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Id của role hiện tại (ví dụ: "student").
        /// </summary>
       public string? RoleId { get; set; }

        /// <summary>
        /// Tên role (ví dụ: "Student").
        /// </summary>
        public string? RoleName { get; set; }

        /// <summary>
        /// Cờ đánh dấu user này mới tạo lần đầu (true) hay user cũ (false).
        /// Hữu ích nếu FE muốn hiện onboarding.
        /// </summary>
        public bool IsNewUser { get; set; }
    }
}
