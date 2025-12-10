using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Users;
using IntervalEventRegistrationService.DTOs.Response.Users;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntervalEventRegistration.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")] // Định nghĩa action này phản hồi cho HTTP GET tới endpoint /api/users/me
        [Authorize] // Yêu cầu request phải có JWT hợp lệ (đã đăng nhập) mới được phép truy cập vào action này
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value // Lấy UserId từ claim NameIdentifier (cách map mặc định thường dùng)
                         ?? User.FindFirst("sub")?.Value                  // Nếu không có NameIdentifier thì thử lấy từ claim 'sub' (thường dùng trong JWT)
                         ?? User.FindFirst("user_id")?.Value;             // Nếu vẫn không có thì thử lấy từ claim 'user_id' (phòng trường hợp cấu hình custom)

            if (string.IsNullOrEmpty(userId)) // Kiểm tra nếu không lấy được UserId từ token (user chưa đăng nhập hoặc token không chuẩn)
            {
                var errorResponse = ApiResponse<MyProfileDto>.FailureResponse( // Tạo một đối tượng ApiResponse báo lỗi cho client
                    "Không xác định được tài khoản hiện tại.");                // Thông báo lỗi cụ thể để FE hiển thị cho người dùng

                return Unauthorized(errorResponse);                            // Trả về HTTP 401 kèm nội dung lỗi để báo client cần đăng nhập lại
            }

            try // Bọc phần logic chính trong khối try để bắt các lỗi từ tầng service
            {
                var profile = await _userService.GetMyProfileAsync(userId);    // Gọi service để lấy thông tin profile của user hiện tại dựa trên userId lấy từ token

                var response = ApiResponse<MyProfileDto>.SuccessResponse(      // Tạo response dạng ApiResponse với trạng thái thành công
                    profile,                                                   // Gán data là đối tượng MyProfileDto nhận được từ service
                    "Lấy thông tin profile thành công.");                      // Gán message mô tả ngắn gọn kết quả xử lý

                return Ok(response);                                           // Trả về HTTP 200 cùng với body là ApiResponse chứa dữ liệu profile
            }
            catch (KeyNotFoundException ex) // Bắt lỗi nếu service ném ra ngoại lệ KeyNotFoundException (ví dụ user đã bị xóa)
            {
                var response = ApiResponse<MyProfileDto>.FailureResponse(      // Tạo ApiResponse báo lỗi cho client
                    ex.Message);                                               // Dùng nội dung message từ exception để FE hiển thị chính xác lý do

                return NotFound(response);                                     // Trả về HTTP 404 kèm nội dung lỗi để thông báo không tìm thấy tài khoản
            }
        }

        [HttpPut("profile")] // Định nghĩa action này xử lý HTTP PUT tới endpoint /api/users/me để cập nhật profile của user hiện tại
        [Authorize] // Bắt buộc user phải đăng nhập (có JWT hợp lệ) mới được phép gọi API cập nhật profile
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value // Lấy UserId từ claim NameIdentifier giống như ở action GetMyProfile
                         ?? User.FindFirst("sub")?.Value                  // Nếu không có thì fallback sang claim 'sub'
                         ?? User.FindFirst("user_id")?.Value;             // Nếu vẫn không có thì fallback sang claim 'user_id'

            if (string.IsNullOrEmpty(userId)) // Kiểm tra nếu không lấy được UserId từ token
            {
                var errorResponse = ApiResponse<MyProfileDto>.FailureResponse( // Tạo ApiResponse báo lỗi cho client
                    "Không xác định được tài khoản hiện tại.");                // Thông điệp lỗi mô tả vấn đề lấy UserId từ token

                return Unauthorized(errorResponse);                            // Trả về HTTP 401 nếu không xác định được user hiện tại
            }

            if (!ModelState.IsValid) // Kiểm tra trạng thái model, nếu có lỗi validate từ data annotations thì ModelState sẽ không hợp lệ
            {
                var errors = ModelState.Values                                  // Lấy toàn bộ các lỗi từ ModelState
                    .SelectMany(v => v.Errors)                                  // Flatten tất cả error trong từng field
                    .Select(e => e.ErrorMessage)                                // Lấy ra thông điệp lỗi của từng error
                    .ToList();                                                  // Chuyển thành List<string> để đưa vào response

                var errorResponse = ApiResponse<MyProfileDto>.FailureResponse(  // Tạo ApiResponse báo lỗi do dữ liệu đầu vào không hợp lệ
                    "Dữ liệu cập nhật profile không hợp lệ.",                   // Thông điệp mô tả lỗi tổng quát
                    errors);                                                    // Danh sách chi tiết các lỗi từ ModelState

                return BadRequest(errorResponse);                               // Trả về HTTP 400 kèm theo danh sách lỗi để FE hiển thị cho user
            }

            try // Bọc phần xử lý chính trong try để bắt lỗi từ tầng service
            {
                var updatedProfile = await _userService.UpdateMyProfileAsync(   // Gọi service để cập nhật profile dựa trên userId và dữ liệu request
                    userId,                                                     // Truyền vào Id của user hiện tại
                    request);                                                   // Truyền vào DTO chứa thông tin profile mới từ client

                var response = ApiResponse<MyProfileDto>.SuccessResponse(       // Tạo ApiResponse báo thành công
                    updatedProfile,                                             // Gán Data là profile sau khi đã được cập nhật
                    "Cập nhật profile thành công.");                            // Thông điệp mô tả ngắn gọn kết quả

                return Ok(response);                                            // Trả về HTTP 200 kèm ApiResponse chứa dữ liệu profile mới
            }
            catch (KeyNotFoundException ex) // Bắt lỗi nếu service báo không tìm thấy user tương ứng
            {
                var response = ApiResponse<MyProfileDto>.FailureResponse(       // Tạo ApiResponse thông báo lỗi cho client
                    ex.Message);                                                // Dùng message từ exception để FE hiểu rõ nguyên nhân

                return NotFound(response);                                      // Trả về HTTP 404 khi không tìm thấy tài khoản để cập nhật
            }
        }
    }
}