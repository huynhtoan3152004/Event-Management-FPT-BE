using IntervalEventRegistrationRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Interfaces
{
    /// <summary>
    /// Repository làm việc với bảng user_auth_providers:
    /// - Tìm liên kết giữa user và provider (google)
    /// - Thêm mới liên kết
    /// - Lưu thay đổi xuống database
    /// </summary>
    public interface IUserAuthProviderRepository
    {
        // Tìm bản ghi user_auth_providers theo provider + provider_user_id (sub trong Google)
        Task<UserAuthProvider?> GetByProviderAndProviderUserIdAsync(string providerId, string providerUserId);

        // Thêm liên kết mới giữa user và provider
        Task AddAsync(UserAuthProvider userAuthProvider);

        // Lưu thay đổi xuống database
        Task SaveChangesAsync();
    }
}
