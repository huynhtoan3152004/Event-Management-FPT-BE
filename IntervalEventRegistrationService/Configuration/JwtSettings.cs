using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Configuration
{
    /// <summary>
    /// Class này bind với section "Jwt" trong appsettings.json.
    /// Dùng để cấu hình việc tạo + validate JWT trong hệ thống.
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Issuer (người phát hành token) - sẽ dùng cho cả tạo token & validate
        /// Ví dụ: "IntervalEventRegistration"
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Audience (đối tượng token dành cho) - thường trùng Issuer
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Secret key để ký token (HS256).
        /// Lưu ý: ở production phải là chuỗi dài, random, không public.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian sống (phút) của Access Token.
        /// Ví dụ: 60 phút.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 240;
    }
}
