using Google.Apis.Auth;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.Configuration;
using IntervalEventRegistrationService.DTOs.Request.Auth;
using IntervalEventRegistrationService.DTOs.Response.Auth;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCryptNet = BCrypt.Net.BCrypt;

namespace IntervalEventRegistrationService.Services
{
    /// <summary>
    /// AuthService xử lý nghiệp vụ liên quan đến xác thực:
    /// - Login bằng Google ID Token
    /// - Tạo / lấy user trong DB (mặc định role "student" cho user mới)
    /// - Sinh JWT của hệ thống để client dùng cho các request tiếp theo.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserAuthProviderRepository _userAuthProviderRepository;
        private readonly GoogleAuthSettings _googleAuthSettings;
        private readonly JwtSettings _jwtSettings;

        // Id của provider google (đã được seed trong bảng auth_providers với id 'google')
        private const string GoogleProviderId = "google";

        // Role mặc định cho sinh viên (đã seed trong bảng roles với RoleId = "student")
        private const string DefaultStudentRoleId = "student";


        public AuthService(
            IUserRepository userRepository,
            IUserAuthProviderRepository userAuthProviderRepository,
            IOptions<GoogleAuthSettings> googleAuthOptions,
            IOptions<JwtSettings> jwtOptions)
        {
            _userRepository = userRepository;
            _userAuthProviderRepository = userAuthProviderRepository;
            _googleAuthSettings = googleAuthOptions.Value;
            _jwtSettings = jwtOptions.Value;
        }

        /// <summary>
        /// Login bằng Google:
        /// 1. Verify Google ID Token với Google
        /// 2. Tìm hoặc tạo User trong DB (mặc định role student nếu user mới)
        /// 3. Tạo JWT của hệ thống trả về client.
        /// </summary>
        public async Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginRequestDto request)
        {
            // 1. Kiểm tra đầu vào: IdToken không được rỗng
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                throw new ArgumentException("Google ID token is required.", nameof(request.IdToken));
            }

            // 2. Verify Google ID Token với Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                // Thiết lập rule validate: Audience phải trùng ClientId của mình
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleAuthSettings.ClientId }
                };

                // ValidateAsync sẽ:
                // - Kiểm tra chữ ký token
                // - Kiểm tra issuer, audience, thời hạn
                // - Trả về payload nếu hợp lệ
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
            }
            catch (InvalidJwtException ex)
            {
                // Nếu token không hợp lệ hoặc hết hạn -> không cho login
                throw new UnauthorizedAccessException("Invalid Google ID token.", ex);
            }

            // 3. Lấy email từ payload để map với User trong hệ thống
            var email = payload.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                // Một số trường hợp hiếm: Google không trả email
                throw new UnauthorizedAccessException("Google account does not provide an email address.");
            }

            // 3.x. (ĐANG COMMENT): Chỉ cho phép login bằng mail FPTU (@fpt.edu.vn)
            // ---------------------------------------------------------------
            // TODO: Nếu sau này bạn muốn CHỈ cho phép đăng nhập bằng email FPTU,
            // hãy bỏ comment đoạn code dưới đây.
            //
            // if (!email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
            // {
            //     // Nếu email không phải @fpt.edu.vn thì chặn
            //     throw new UnauthorizedAccessException("Only FPTU (@fpt.edu.vn) email addresses are allowed to login.");
            // }
            // ---------------------------------------------------------------

            // 4. Thử tìm liên kết user_auth_providers theo provider = google + provider_user_id = sub
            var existingLink = await _userAuthProviderRepository
                .GetByProviderAndProviderUserIdAsync(GoogleProviderId, payload.Subject);

            User user;
            var isNewUser = false;

            if (existingLink != null && existingLink.User != null)
            {
                // 4.1. Nếu đã có liên kết => lấy user tương ứng
                user = existingLink.User;

                // Cập nhật thông tin đăng nhập gần nhất + email verified
                user.LastLoginAt = DateTime.UtcNow;
                user.EmailVerified = payload.EmailVerified;

                _userRepository.Update(user);
            }
            else
            {
                // 4.2. Chưa có liên kết => kiểm tra xem có user nào dùng email này chưa
                user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    // 4.2.1. Nếu email chưa tồn tại => tạo user mới với role student
                    user = new User
                    {
                        // Role mặc định: student (đã seed trong DB)
                        RoleId = DefaultStudentRoleId,

                        // Nếu Google không trả Name, dùng email làm Name cho đỡ trống
                        Name = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name,

                        Email = email,
                        AvatarUrl = payload.Picture,
                        EmailVerified = payload.EmailVerified,

                        Status = "active",
                        IsDeleted = false,
                        LastLoginAt = DateTime.UtcNow
                    };

                    await _userRepository.AddAsync(user);
                    isNewUser = true;
                }
                else
                {
                    // 4.2.2. Nếu user đã tồn tại theo email:
                    // Cập nhật vài thông tin profile cơ bản (nếu thiếu)
                    if (string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(payload.Name))
                    {
                        user.Name = payload.Name;
                    }

                    if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(payload.Picture))
                    {
                        user.AvatarUrl = payload.Picture;
                    }

                    user.EmailVerified = payload.EmailVerified;
                    user.LastLoginAt = DateTime.UtcNow;

                    _userRepository.Update(user);
                }

                // 4.3. Tạo mới bản ghi user_auth_providers để link user với Google
                var link = new UserAuthProvider
                {
                    UserId = user.UserId,
                    ProviderId = GoogleProviderId,
                    ProviderUserId = payload.Subject, // sub trong Google ID token
                    Email = email,
                    DisplayName = payload.Name,
                    AvatarUrl = payload.Picture,

                    // Lưu lại IdToken & thời điểm hết hạn (nếu muốn tham khảo)
                    IdToken = request.IdToken,
                    TokenExpiry = payload.ExpirationTimeSeconds.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(payload.ExpirationTimeSeconds.Value).UtcDateTime
                        : null
                };

                await _userAuthProviderRepository.AddAsync(link);
            }

            // 5. Lưu tất cả thay đổi xuống DB
            // Vì cả 2 repository dùng chung ApplicationDbContext (Scoped)
            // nên SaveChangesAsync ở một repo cũng commit hết.
            await _userAuthProviderRepository.SaveChangesAsync();
            // 5.1. Đảm bảo navigation Role đã được load (đặc biệt với user mới tạo)
            if (user.Role == null && !string.IsNullOrWhiteSpace(user.RoleId))
            {
                // Gọi lại repo để load user kèm Role từ DB
                var reloadedUser = await _userRepository.GetByIdAsync(user.UserId);
                if (reloadedUser != null)
                {
                    user = reloadedUser;
                }
            }
            // 6. Tạo JWT của hệ thống cho user này
            var (jwt, expiresAt) = GenerateJwtForUser(user);

            // 7. Trả response cho client
            return new AuthResponseDto
            {
                AccessToken = jwt,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                Email = user.Email,
                Name = user.Name,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName,
                IsNewUser = isNewUser
            };
        }

        /// <summary>
        /// Hàm private sinh JWT dựa trên thông tin user + cấu hình trong JwtSettings.
        /// </summary>
        private (string token, DateTime expiresAt) GenerateJwtForUser(User user)
        {
            // Chuẩn bị key và các thông tin cơ bản
            var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);

            // Tập claims của user (những gì mình muốn embed vào JWT)
            var claims = new List<Claim>
            {
                // sub: chuẩn của JWT, thường là id của user
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),

                // email: dùng để client đọc nhanh
                new Claim(JwtRegisteredClaimNames.Email, user.Email),

                // NameIdentifier & Name theo chuẩn ClaimTypes (dùng cho [Authorize])
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty)
            };

            // Nếu user có role, thêm vào claim role
            if (!string.IsNullOrWhiteSpace(user.RoleId))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.RoleId));
            }


            // Thời điểm hết hạn của token
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

            // Mô tả token (issuer, audience, signing credentials,...)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(securityToken);

            return (jwt, expiresAt);
        }

        public async Task<AuthResponseDto> LoginWithEmailPasswordAsync(EmailPasswordLoginRequestDto request)
        {
            // 1. Validate input cơ bản (phòng trường hợp ModelState không check)
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Email and password are required.");
            }

            // Chuẩn hóa email: trim + lower-case
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // 2. Tìm user theo email
            //    Hàm GetByEmailAsync trong UserRepository đã include Role sẵn
            var user = await _userRepository.GetByEmailAsync(normalizedEmail);

            if (user == null || user.IsDeleted)
            {
                // Không tìm thấy user hoặc user đã bị đánh dấu xóa
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // 3. Kiểm tra trạng thái tài khoản (ví dụ: chỉ cho 'active')
            if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Your account is not active.");
            }

            // 4. Kiểm tra PasswordHash: nếu null/empty => chưa thiết lập mật khẩu
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Password is not set for this account.");
            }

            // 5. Verify mật khẩu bằng BCrypt:
            //    - request.Password: plaintext user nhập
            //    - user.PasswordHash: chuỗi hash lưu trong DB
            var passwordMatches = BCryptNet.Verify(request.Password, user.PasswordHash);

            if (!passwordMatches)
            {
                // Nếu mật khẩu sai => không tiết lộ thông tin (trả generic message)
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // 6. Update các thông tin đăng nhập (last_login_at,...)
            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // 7. Sinh JWT cho user (tái sử dụng hàm GenerateJwtForUser đã viết)
            var (jwt, expiresAt) = GenerateJwtForUser(user);

            // 8. Trả về AuthResponseDto giống Google login cho FE dùng chung
            return new AuthResponseDto
            {
                AccessToken = jwt,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                Email = user.Email,
                Name = user.Name,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName,
                IsNewUser = false // login local thường là user đã có sẵn
            };
        }

        public async Task RegisterAsync(RegisterRequestDto request)
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email này đã được sử dụng.");
            }

            // 2. Hash mật khẩu
            string passwordHash = BCryptNet.HashPassword(request.Password);

            // 3. Tạo Entity User mới
            var newUser = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = passwordHash,

                // Set cứng Role là Student
                RoleId = DefaultStudentRoleId,

                // Các thông tin khác
                Status = "active",
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,

                // Student không cần thông tin tổ chức/phòng ban
                Organization = null,
                Department = null
            };

            // 4. Lưu xuống Database
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Kết thúc, không sinh Token
        }
    }
}
