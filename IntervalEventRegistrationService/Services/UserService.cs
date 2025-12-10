using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Users;
using IntervalEventRegistrationService.DTOs.Response.Users;
using IntervalEventRegistrationService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<PagedResponse<UserListItemDto>> GetUsersForAdminAsync(UserListRequest request)
        {
            var pageNumber = request.PageNumber; // Lấy số trang từ request (kế thừa từ PaginationRequest, mặc định đã >= 1)

            var pageSize = request.PageSize == 0 ? 10 : request.PageSize; // Lấy kích thước trang, nếu client không truyền thì mặc định là 10 user mỗi trang

            var (users, totalItems) = await _userRepository.GetUsersForAdminAsync( // Gọi xuống repository để lấy danh sách user và tổng số record
                request.Search,                                                  // Truyền từ khóa search để repository lọc theo Name/Email
                request.RoleId,                                                  // Truyền RoleId nếu cần filter theo role
                request.Status,                                                  // Truyền Status nếu cần filter theo trạng thái
                pageNumber,                                                      // Truyền số trang hiện tại
                pageSize                                                         // Truyền số bản ghi mỗi trang
            );

            var items = users                                                   // Bắt đầu từ danh sách entity User lấy từ repository
                .Select(MapToUserListItemDto)                                   // Map từng User sang UserListItemDto bằng hàm helper
                .ToList();                                                      // Thực thi select và chuyển sang List<UserListItemDto>

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);  // Tính tổng số trang dựa trên tổng record và kích thước trang

            var response = new PagedResponse<UserListItemDto>                   // Tạo object PagedResponse để trả về cho tầng Controller
            {
                Success = true,                                                 // Đánh dấu gọi service thành công
                Message = "Lấy danh sách users thành công",                     // Thông báo ngắn gọn nội dung kết quả
                Data = items,                                                   // Gán danh sách DTO user cho thuộc tính Data
                Pagination = new PaginationMeta                                 // Khởi tạo meta phân trang cho FE sử dụng
                {
                    CurrentPage = pageNumber,                                   // Gán trang hiện tại cho meta
                    PageSize = pageSize,                                       // Gán kích thước trang cho meta
                    TotalItems = totalItems,                                   // Gán tổng số bản ghi cho meta
                    TotalPages = totalPages                                    // Gán tổng số trang cho meta
                }
            };

            return response;                                                   // Trả về đối tượng PagedResponse<UserListItemDto> cho Controller
        }

        public async Task<UserDetailDto> GetUserDetailForAdminAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId, includeDeleted: true); // Gọi repository để lấy user theo Id, bao gồm cả user đã bị soft delete

            if (user == null)                                                  // Kiểm tra nếu không tìm thấy user trong database
            {
                throw new KeyNotFoundException("Không tìm thấy user với Id được cung cấp."); // Ném exception để Controller xử lý trả về 404
            }

            var dto = MapToUserDetailDto(user);                                // Map entity User sang DTO chi tiết cho Admin

            return dto;                                                        // Trả về DTO chi tiết user
        }

        public async Task<MyProfileDto> GetMyProfileAsync(string currentUserId)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId, includeDeleted: false); // Lấy thông tin user hiện tại, không bao gồm user đã bị soft delete

            if (user == null)                                                  // Nếu không tìm thấy user tương ứng với currentUserId
            {
                throw new KeyNotFoundException("Tài khoản hiện tại không tồn tại hoặc đã bị xóa."); // Ném lỗi để Controller xử lý thích hợp
            }

            var dto = MapToMyProfileDto(user);                                 // Map entity User sang DTO MyProfileDto

            return dto;                                                        // Trả về thông tin profile của user hiện tại
        }

        public async Task<MyProfileDto> UpdateMyProfileAsync(string currentUserId, UpdateMyProfileRequest request) // Triển khai hàm cập nhật profile cho user hiện tại dựa trên dữ liệu client gửi lên
        {
            var user = await _userRepository.GetByIdAsync(currentUserId, includeDeleted: false); // Gọi repository để lấy entity user tương ứng với currentUserId và bỏ qua user đã bị soft delete

            if (user == null) // Kiểm tra nếu không tìm thấy user trong database (có thể do đã bị xóa hoặc id không hợp lệ)
            {
                throw new KeyNotFoundException("Tài khoản hiện tại không tồn tại hoặc đã bị xóa."); // Ném exception để Controller xử lý trả về 404 cho client
            }

            if (request.Name != null) // Nếu client có gửi field Name (không null) thì mới thực hiện cập nhật tên
            {
                user.Name = request.Name; // Gán Name mới từ request vào entity user để lưu xuống database
            }

            if (request.Phone != null) // Nếu client có gửi field Phone (không null) thì mới cập nhật số điện thoại
            {
                user.Phone = request.Phone; // Gán số điện thoại mới từ request cho entity user
            }

            if (request.AvatarUrl != null) // Nếu client có gửi field AvatarUrl (không null) thì mới cập nhật avatar
            {
                user.AvatarUrl = request.AvatarUrl; // Gán đường dẫn avatar mới cho entity user
            }

            if (request.StudentCode != null) // Nếu client có gửi field StudentCode (không null) thì mới cập nhật mã sinh viên
            {
                user.StudentCode = request.StudentCode; // Gán mã sinh viên mới cho entity user
            }

            if (request.Organization != null) // Nếu client có gửi field Organization (không null) thì mới cập nhật tổ chức/đơn vị
            {
                user.Organization = request.Organization; // Gán tổ chức/đơn vị mới cho entity user
            }

            if (request.Department != null) // Nếu client có gửi field Department (không null) thì mới cập nhật phòng ban
            {
                user.Department = request.Department; // Gán phòng ban mới cho entity user
            }

            user.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian chỉnh sửa cuối cùng là thời điểm hiện tại theo chuẩn UTC để tiện audit

            _userRepository.Update(user); // Đánh dấu entity user đã bị thay đổi để EF Core sinh câu lệnh UPDATE tương ứng khi lưu

            await _userRepository.SaveChangesAsync(); // Ghi toàn bộ thay đổi của entity user xuống database một cách bất đồng bộ

            var dto = MapToMyProfileDto(user); // Map lại entity user sau cập nhật sang DTO MyProfileDto để trả về cho client

            return dto; // Trả về thông tin profile mới sau khi đã được cập nhật các trường cần thiết
        }

        public async Task<UserDetailDto> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId, includeDeleted: true); // Lấy user theo Id, cho phép lấy cả user đã bị soft delete để Admin vẫn xem được

            if (user == null)                                                  // Nếu không tìm thấy user tương ứng trong database
            {
                throw new KeyNotFoundException("Không tìm thấy user để cập nhật trạng thái."); // Ném exception để Controller xử lý
            }

            user.Status = request.Status;                                      // Gán trạng thái mới cho user dựa trên dữ liệu request

            user.UpdatedAt = DateTime.UtcNow;                                  // Cập nhật thời gian sửa gần nhất là thời điểm hiện tại (UTC)

            _userRepository.Update(user);                                      // Đánh dấu entity user là modified cho EF Core

            await _userRepository.SaveChangesAsync();                          // Commit thay đổi xuống database

            var dto = MapToUserDetailDto(user);                                // Map entity user sau khi cập nhật sang DTO chi tiết

            return dto;                                                        // Trả về thông tin chi tiết user sau khi đổi trạng thái
        }

        public async Task<UserDetailDto> UpdateUserRoleAsync(string userId, UpdateUserRoleRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId, includeDeleted: true); // Lấy user theo Id, cho phép thấy cả user đã bị soft delete

            if (user == null)                                                  // Nếu user không tồn tại trong database
            {
                throw new KeyNotFoundException("Không tìm thấy user để cập nhật vai trò."); // Ném exception để Controller trả về lỗi phù hợp
            }

            user.RoleId = request.RoleId;                                      // Gán RoleId mới cho user dựa trên giá trị gửi từ Admin

            user.UpdatedAt = DateTime.UtcNow;                                  // Cập nhật thời điểm sửa gần nhất sang thời điểm hiện tại (UTC)

            _userRepository.Update(user);                                      // Đánh dấu entity user cần được update

            await _userRepository.SaveChangesAsync();                          // Lưu thay đổi role xuống database

            var dto = MapToUserDetailDto(user);                                // Map lại entity user sang DTO chi tiết để trả về cho Admin

            return dto;                                                        // Trả về DTO thông tin user với role mới
        }

        private UserListItemDto MapToUserListItemDto(User user)
        {
            var dto = new UserListItemDto                                      // Khởi tạo một instance DTO dùng cho danh sách Users
            {
                UserId = user.UserId,                                          // Gán UserId từ entity sang DTO
                Name = user.Name,                                              // Gán Name từ entity sang DTO
                Email = user.Email,                                            // Gán Email từ entity sang DTO
                Phone = user.Phone,                                            // Gán Phone từ entity sang DTO
                RoleId = user.RoleId,                                          // Gán RoleId từ entity sang DTO
                RoleName = user.Role?.RoleName,                                    // Gán RoleName nếu navigation property Role không null
                Status = user.Status,                                          // Gán Status từ entity sang DTO
                IsDeleted = user.IsDeleted,                                    // Gán IsDeleted để Admin biết user đã bị soft delete chưa
                CreatedAt = user.CreatedAt                                     // Gán CreatedAt để hiển thị hoặc sort theo ngày tạo
            };

            return dto;                                                        // Trả về DTO đã map xong
        }

        private UserDetailDto MapToUserDetailDto(User user)
        {
            var dto = new UserDetailDto                                        // Khởi tạo DTO chi tiết user cho Admin
            {
                UserId = user.UserId,                                          // Gán Id của user
                RoleId = user.RoleId,                                          // Gán RoleId hiện tại
                RoleName = user.Role?.RoleName,                                    // Gán RoleName nếu có
                Name = user.Name,                                              // Gán tên hiển thị
                Email = user.Email,                                            // Gán email đăng nhập
                Phone = user.Phone,                                            // Gán số điện thoại
                AvatarUrl = user.AvatarUrl,                                    // Gán đường dẫn avatar
                Status = user.Status,                                          // Gán trạng thái hiện tại
                IsDeleted = user.IsDeleted,                                    // Gán cờ IsDeleted
                StudentCode = user.StudentCode,                                // Gán mã sinh viên nếu có
                Organization = user.Organization,                              // Gán tổ chức/đơn vị
                Department = user.Department,                                  // Gán phòng ban
                EmailVerified = user.EmailVerified,                            // Gán trạng thái đã verify email hay chưa
                LastLoginAt = user.LastLoginAt,                                // Gán lần đăng nhập gần nhất
                CreatedAt = user.CreatedAt,                                    // Gán thời điểm tạo account
                UpdatedAt = user.UpdatedAt                                     // Gán thời điểm cập nhật gần nhất
            };

            return dto;                                                        // Trả về DTO chi tiết đã được ánh xạ từ entity
        }

        private MyProfileDto MapToMyProfileDto(User user)
        {
            var dto = new MyProfileDto                                         // Khởi tạo DTO profile cho user hiện tại
            {
                UserId = user.UserId,                                          // Gán Id của user
                RoleId = user.RoleId,                                          // Gán RoleId hiện tại
                RoleName = user.Role?.RoleName,                                    // Gán tên role nếu có
                Name = user.Name,                                              // Gán tên hiển thị
                Email = user.Email,                                            // Gán email đăng nhập
                Phone = user.Phone,                                            // Gán số điện thoại
                AvatarUrl = user.AvatarUrl,                                    // Gán đường dẫn avatar
                Status = user.Status,                                          // Gán trạng thái hiện tại của tài khoản
                StudentCode = user.StudentCode,                                // Gán mã sinh viên nếu có
                Organization = user.Organization,                              // Gán tổ chức/đơn vị
                Department = user.Department,                                  // Gán phòng ban
                EmailVerified = user.EmailVerified,                            // Gán trạng thái đã verify email hay chưa
                LastLoginAt = user.LastLoginAt,                                // Gán lần đăng nhập gần nhất
                CreatedAt = user.CreatedAt,                                    // Gán thời điểm tạo tài khoản
                UpdatedAt = user.UpdatedAt                                     // Gán thời điểm profile được cập nhật gần nhất
            };

            return dto;                                                        // Trả về DTO profile cho user hiện tại
        }
        public async Task<UserDetailDto> CreateUserAsync(CreateUserRequest request)
        {
            if (request.RoleId != "organizer" && request.RoleId != "staff")     // Kiểm tra roleId, chỉ cho phép tạo user với role organizer hoặc staff
            {
                throw new ArgumentException("Role của user phải là 'organizer' hoặc 'staff'."); // Ném exception nếu role không hợp lệ
            }

            var emailNormalized = request.Email.Trim().ToLower();               // Chuẩn hóa email: cắt khoảng trắng và chuyển về chữ thường để check trùng

            var existingUser = await _userRepository.GetByEmailAsync(emailNormalized); // Gọi repository tìm xem email đã tồn tại chưa

            if (existingUser != null)                                           // Nếu đã tồn tại user với email này
            {
                throw new ArgumentException("Email đã tồn tại trong hệ thống."); // Ném exception báo lỗi trùng email
            }

            var passwordHash = HashPassword(request.Password);                      // Hash mật khẩu plaintext thành chuỗi passwordHash để lưu vào DB

            var user = new User                                                 // Tạo mới một entity User
            {
                UserId = Guid.NewGuid().ToString(),                             // Sinh một Guid mới làm UserId
                RoleId = request.RoleId,                                        // Gán RoleId cho user theo request
                Name = request.Name,                                            // Gán Name cho user theo request
                Email = emailNormalized,                                        // Gán Email đã chuẩn hóa chữ thường cho user
                PasswordHash = passwordHash,                                    // Gán PasswordHash đã hash bằng SHA256
                Status = "active",                                              // Mặc định trạng thái user mới là active
                Organization = request.Organization,                            // Gán Organization nếu Admin cung cấp
                Department = request.Department,                                // Gán Department nếu Admin cung cấp
                EmailVerified = false,                                          // Mặc định email chưa được xác thực
                IsDeleted = false,                                              // Đảm bảo user mới không bị đánh dấu xóa
                CreatedAt = DateTime.UtcNow,                                    // Gán thời điểm tạo là hiện tại (UTC)
                UpdatedAt = null                                                // Chưa có lần cập nhật nào nên để null
            };

            await _userRepository.AddAsync(user);                               // Thêm entity user mới vào DbContext thông qua repository

            await _userRepository.SaveChangesAsync();                           // Lưu thay đổi xuống database để thực sự tạo user mới

            var dto = MapToUserDetailDto(user);                                 // Map entity user mới sang DTO UserDetailDto

            return dto;                                                         // Trả về DTO chi tiết user vừa được tạo cho controller admin
        }

        private string HashPassword(string password)                               // Hàm helper nhận mật khẩu dạng plaintext và trả về chuỗi hash an toàn để lưu DB
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);                  // Dùng thư viện BCrypt.Net-Next tạo password hash kèm salt ngẫu nhiên, chống rainbow-table
            return hash;                                                          // Trả về chuỗi hash đã được BCrypt xử lý để gán vào PasswordHash của User
        }

    }
}
