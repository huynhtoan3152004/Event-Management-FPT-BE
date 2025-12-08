using IntervalEventRegistrationService.DTOs.Request.Auth;
using IntervalEventRegistrationService.DTOs.Response.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Interfaces
{
    /// <summary>
    /// Interface định nghĩa các nghiệp vụ xác thực (Auth).
    /// Hiện tại gồm login với Google; sau này có thể thêm login local, refresh token, v.v.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Login bằng Google:
        /// - Nhận Google ID Token từ client
        /// - Verify với Google
        /// - Tạo / lấy user trong DB (role mặc định là "student" nếu user mới)
        /// - Tạo JWT của hệ thống và trả về cho client.
        /// </summary>
        Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginRequestDto request);

        /// <summary>
        /// Login bằng email & password:
        /// - Tìm user theo email
        /// - Kiểm tra password (BCrypt)
        /// - Kiểm tra trạng thái tài khoản
        /// - Sinh JWT và trả về AuthResponseDto.
        /// </summary>
        Task<AuthResponseDto> LoginWithEmailPasswordAsync(EmailPasswordLoginRequestDto request);


        /// <summary>
        /// Đăng ký tài khoản mới (Student, Staff, Organizer)
        /// </summary>
        Task RegisterAsync(RegisterRequestDto request);

    }
}
