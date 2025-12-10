using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Users;
using IntervalEventRegistrationService.DTOs.Response.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.Interfaces
{
    public interface IUserService
    {
        Task<PagedResponse<UserListItemDto>> GetUsersForAdminAsync(UserListRequest request); // Lấy danh sách users cho Admin với search + filter + paging

        Task<UserDetailDto> GetUserDetailForAdminAsync(string userId); // Lấy chi tiết một user bất kỳ cho Admin (kể cả user đã soft delete)

        Task<MyProfileDto> GetMyProfileAsync(string currentUserId); // Lấy thông tin profile của user đang đăng nhập dựa trên currentUserId

        Task<MyProfileDto> UpdateMyProfileAsync(string currentUserId, UpdateMyProfileRequest request); // Cập nhật profile của user hiện tại và trả lại thông tin profile mới

        Task<UserDetailDto> UpdateUserStatusAsync(string userId, UpdateUserStatusRequest request); // Admin cập nhật trạng thái (status) cho một user và trả lại thông tin chi tiết

        Task<UserDetailDto> UpdateUserRoleAsync(string userId, UpdateUserRoleRequest request); // Admin đổi role cho một user và trả lại thông tin chi tiết
        Task<UserDetailDto> CreateUserAsync(CreateUserRequest request);
    }
}
