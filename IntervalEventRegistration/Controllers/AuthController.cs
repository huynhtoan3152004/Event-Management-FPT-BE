using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Auth;
using IntervalEventRegistrationService.DTOs.Response.Auth;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntervalEventRegistration.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Endpoint đăng nhập bằng Google.
        /// FE sẽ gửi Google ID Token (IdToken) vào body.
        /// Hệ thống verify với Google + tạo user (role mặc định student) + sinh JWT trả về.
        /// </summary>
        /// <param name="request">Chứa Google IdToken từ FE</param>
        /// <returns>AuthResponseDto (AccessToken + thông tin user)</returns>
        [HttpPost("google")]                 // => POST /api/auth/google
        [AllowAnonymous]                    // Cho phép gọi mà không cần JWT (vì đây là login)
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> LoginWithGoogle([FromBody] GoogleLoginRequestDto request)
        {
            // 1. Kiểm tra model state (trường hợp body không đúng, thiếu IdToken v.v.)
            if (!ModelState.IsValid)
            {
                // Trả về 400 kèm chi tiết lỗi validate
                return BadRequest(ModelState);
            }

            try
            {
                // 2. Gọi xuống tầng Service để xử lý toàn bộ logic:
                //    - Verify IdToken với Google
                //    - Tìm / tạo User + UserAuthProvider
                //    - Sinh JWT + trả AuthResponseDto
                var result = await _authService.LoginWithGoogleAsync(request);

                // 3. Trả về 200 OK với AuthResponseDto (AccessToken + User info)
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Trường hợp request không hợp lệ (ví dụ: IdToken rỗng) => 400
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Trường hợp IdToken sai, hết hạn, hoặc (sau này) email không phải @fpt.edu.vn => 401
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                // Lỗi không mong muốn (server error) => 500
                // (Thực tế nên log exception lại, nhưng ở đây để đơn giản cho project môn học)
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while logging in with Google."
                });
            }
        }

        /// <summary>
        /// Endpoint test nhanh JWT: trả về thông tin user hiện tại đang đăng nhập.
        /// Dùng để kiểm tra xem token ở Swagger / FE có decode đúng không.
        /// </summary>
        [HttpGet("me")]              // => GET /api/auth/me
        [Authorize]                  // Bắt buộc phải gửi JWT kèm header Authorization
        public ActionResult GetCurrentUser()
        {
            // Lấy các claim đã được gắn vào JWT khi tạo token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirst("email")?.Value
                        ?? User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirstValue(ClaimTypes.Role);

            // Trả về thông tin cơ bản, chỉ để test.
            return Ok(new
            {
                userId,
                email,
                role
            });
        }

        /// <summary>
        /// Đăng nhập bằng email & password (local login).
        /// Sau khi xác thực thành công, hệ thống trả về JWT giống như login Google.
        /// </summary>
        [HttpPost("login")]                // => POST /api/auth/login
        [AllowAnonymous]                   // đây là endpoint login nên không cần JWT
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> LoginWithEmailPassword(
            [FromBody] EmailPasswordLoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Gọi xuống Service xử lý:
                // - Tìm user
                // - Verify password (BCrypt)
                // - Sinh JWT
                var result = await _authService.LoginWithEmailPasswordAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while logging in with email and password."
                });
            }
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            // Kiểm tra tính hợp lệ của dữ liệu (Confirm password, email format...)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.FailureResponse("Dữ liệu đăng ký không hợp lệ", errors));
            }

            try
            {
                await _authService.RegisterAsync(request);

                // Trả về thành công, không kèm Token
                return Ok(ApiResponse<object>.SuccessResponse(null, "Đăng ký thành công. Vui lòng đăng nhập để tiếp tục."));
            }
            catch (ArgumentException ex) // Bắt lỗi nghiệp vụ (ví dụ: trùng email)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.FailureResponse("Lỗi hệ thống: " + ex.Message));
            }
        }

    }
}
