using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Users;
using IntervalEventRegistrationService.DTOs.Response.Users;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntervalEventRegistration.Controllers
{
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "organizer")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminUsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet] // Ánh xạ action này với HTTP GET tới endpoint /api/admin/users
        public async Task<IActionResult> GetUsersForAdmin(
            [FromQuery] string? keyword, // Nhận tham số keyword từ query string để search theo Name/Email, có thể null nếu không search
            [FromQuery] string? roleId,  // Nhận tham số roleId từ query string để filter theo vai trò (admin/organizer/staff/student)
            [FromQuery] string? status,  // Nhận tham số status từ query string để filter theo trạng thái (active/inactive/...)
            [FromQuery] int pageNumber = 1, // Nhận số trang hiện tại từ query string, mặc định là 1 nếu client không truyền
            [FromQuery] int pageSize = 10   // Nhận kích thước trang từ query string, mặc định là 10 user mỗi trang để đúng yêu cầu đề bài
        )
        {
            var request = new UserListRequest                      // Tạo một DTO UserListRequest để gom tất cả thông tin filter/paging vào một object
            {
                Search = keyword,                                  // Gán từ khóa search cho thuộc tính Search để service dùng lọc Name/Email
                RoleId = roleId,                                   // Gán RoleId để service lọc user theo vai trò nếu có
                Status = status,                                   // Gán Status để service lọc theo trạng thái nếu có
                PageNumber = pageNumber,                           // Gán số trang hiện tại cho request
                PageSize = pageSize                                // Gán kích thước trang cho request, mặc định là 10 nếu client không truyền
            };

            var result = await _userService.GetUsersForAdminAsync( // Gọi service để lấy danh sách user phù hợp với filter và paging
                request);                                          // Truyền vào object request chứa toàn bộ tham số lọc và phân trang

            return Ok(result);                                     // Trả về HTTP 200 cùng với body là PagedResponse<UserListItemDto> đã được service chuẩn hóa
        }

        [HttpGet("{userId}")] // Ánh xạ action này với HTTP GET tới endpoint /api/admin/users/{userId} để lấy chi tiết một user
        public async Task<IActionResult> GetUserDetail(string userId)
        {
            try // Dùng try/catch để bắt các lỗi từ tầng service
            {
                var userDetail = await _userService.GetUserDetailForAdminAsync( // Gọi service để lấy thông tin chi tiết của user theo Id
                    userId);                                                    // Truyền userId nhận từ route vào service

                var response = ApiResponse<UserDetailDto>.SuccessResponse(      // Tạo ApiResponse với trạng thái thành công
                    userDetail,                                                 // Gán data là DTO chi tiết user nhận được từ service
                    "Lấy thông tin chi tiết user thành công.");                 // Thông điệp mô tả kết quả xử lý

                return Ok(response);                                            // Trả về HTTP 200 cùng với ApiResponse chứa thông tin chi tiết user
            }
            catch (KeyNotFoundException ex) // Bắt lỗi khi service không tìm thấy user tương ứng với userId đã truyền
            {
                var response = ApiResponse<UserDetailDto>.FailureResponse(      // Tạo ApiResponse báo lỗi cho client
                    ex.Message);                                                // Dùng message từ exception để mô tả rõ nguyên nhân

                return NotFound(response);                                      // Trả về HTTP 404 nếu không tìm thấy user
            }
        }

        [HttpPatch("{userId}/status")] // Ánh xạ action này với HTTP PATCH tới endpoint /api/admin/users/{userId}/status để cập nhật trạng thái user
        public async Task<IActionResult> UpdateUserStatus(
            string userId,                                                     // Nhận userId từ route để xác định user cần cập nhật
            [FromBody] UpdateUserStatusRequest request                         // Nhận DTO từ body request chứa trạng thái mới
        )
        {
            if (string.IsNullOrWhiteSpace(request.Status))                     // Kiểm tra nếu Status trong request null, rỗng hoặc toàn khoảng trắng
            {
                var errorResponse = ApiResponse<UserDetailDto>.FailureResponse(// Tạo ApiResponse báo lỗi cho client
                    "Trạng thái mới của user không được để trống.");          // Thông điệp mô tả lý do không hợp lệ

                return BadRequest(errorResponse);                             // Trả về HTTP 400 nếu trạng thái gửi lên không hợp lệ
            }

            try // Dùng try/catch để bắt lỗi từ tầng service
            {
                var updatedUser = await _userService.UpdateUserStatusAsync(   // Gọi service để cập nhật trạng thái cho user
                    userId,                                                   // Truyền vào Id của user cần cập nhật
                    request);                                                 // Truyền vào DTO chứa trạng thái mới

                var response = ApiResponse<UserDetailDto>.SuccessResponse(    // Tạo ApiResponse báo thành công
                    updatedUser,                                              // Gán data là user sau khi đã cập nhật trạng thái
                    "Cập nhật trạng thái user thành công.");                  // Thông điệp mô tả kết quả xử lý

                return Ok(response);                                          // Trả về HTTP 200 kèm ApiResponse chứa thông tin user đã được cập nhật
            }
            catch (KeyNotFoundException ex)                                   // Bắt lỗi nếu service không tìm thấy user để update
            {
                var response = ApiResponse<UserDetailDto>.FailureResponse(    // Tạo ApiResponse báo lỗi
                    ex.Message);                                              // Dùng message từ exception để mô tả nguyên nhân lỗi

                return NotFound(response);                                    // Trả về HTTP 404 nếu user không tồn tại
            }
        }

        [HttpPatch("{userId}/role")] // Ánh xạ action này với HTTP PATCH tới endpoint /api/admin/users/{userId}/role để đổi role của user
        public async Task<IActionResult> UpdateUserRole(
            string userId,                                                   // Nhận userId từ route để xác định user cần đổi role
            [FromBody] UpdateUserRoleRequest request                        // Nhận DTO chứa RoleId mới từ body request
        )
        {
            if (string.IsNullOrWhiteSpace(request.RoleId))                  // Kiểm tra nếu RoleId trong request null, rỗng hoặc toàn khoảng trắng
            {
                var errorResponse = ApiResponse<UserDetailDto>.FailureResponse( // Tạo ApiResponse báo lỗi cho client
                    "Role mới của user không được để trống.");              // Thông điệp mô tả lý do request không hợp lệ

                return BadRequest(errorResponse);                           // Trả về HTTP 400 khi dữ liệu RoleId không hợp lệ
            }

            try // Dùng try/catch để bắt lỗi từ tầng service
            {
                var updatedUser = await _userService.UpdateUserRoleAsync(   // Gọi service để cập nhật RoleId mới cho user
                    userId,                                                 // Truyền Id của user cần đổi role
                    request);                                               // Truyền DTO chứa RoleId mới

                var response = ApiResponse<UserDetailDto>.SuccessResponse(  // Tạo ApiResponse báo thành công
                    updatedUser,                                            // Gán data là user sau khi đã đổi role
                    "Cập nhật vai trò (role) user thành công.");            // Thông điệp mô tả ngắn gọn kết quả cập nhật

                return Ok(response);                                        // Trả về HTTP 200 kèm ApiResponse chứa thông tin user mới
            }
            catch (KeyNotFoundException ex)                                 // Bắt lỗi nếu service báo user không tồn tại
            {
                var response = ApiResponse<UserDetailDto>.FailureResponse(  // Tạo ApiResponse báo lỗi cho client
                    ex.Message);                                            // Dùng message từ exception để mô tả nguyên nhân lỗi

                return NotFound(response);                                  // Trả về HTTP 404 nếu không tìm thấy user để đổi role
            }
        }
    }
}
