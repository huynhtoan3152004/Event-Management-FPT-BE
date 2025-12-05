using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Configuration
{
    /// <summary>
    /// Class này dùng để bind section "GoogleAuth" trong appsettings.json.
    /// Mục tiêu: truyền các cấu hình Google OAuth (ClientId, Issuer, Audience)
    /// vào Service thông qua DI (IOptions<GoogleAuthSettings>).
    /// </summary>
    public class GoogleAuthSettings
    {
        // ClientId ứng với OAuth Client trên Google Cloud
        public string ClientId { get; set; } = string.Empty;

        // Issuer hợp lệ của ID token, thường là "https://accounts.google.com"
        public string ValidIssuer { get; set; } = string.Empty;

        // Audience kỳ vọng trong ID token (thường trùng ClientId)
        public string Audience { get; set; } = string.Empty;
    }
}
