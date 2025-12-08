using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Auth
{
    /// <summary>
    /// DTO này đại diện cho request từ FE khi login bằng Google.
    /// FE sẽ gửi Google ID Token (JWT do Google cấp) lên API.
    /// </summary>
    public class GoogleLoginRequestDto
    {
        /// <summary>
        /// Google ID Token mà FE nhận được sau khi user đăng nhập Google.
        /// AuthService sẽ dùng token này để verify với Google.
        /// </summary>
        public string IdToken { get; set; } = string.Empty;
    }
}
