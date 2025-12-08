using IntervalEventRegistrationRepo.Data;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Repository
{
    public class UserAuthProviderRepository : IUserAuthProviderRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public UserAuthProviderRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Tìm bản ghi user_auth_providers theo provider_id (ví dụ 'google')
        /// và provider_user_id (sub trong Google ID token).
        /// Include luôn User + Role để có đủ thông tin khi login.
        /// </summary>
        public async Task<UserAuthProvider?> GetByProviderAndProviderUserIdAsync(string providerId, string providerUserId)
        {
            return await _dbContext.UserAuthProviders
                .Include(uap => uap.User)
                    .ThenInclude(u => u.Role)
                .Include(uap => uap.AuthProvider)
                .FirstOrDefaultAsync(uap =>
                    uap.ProviderId == providerId &&
                    uap.ProviderUserId == providerUserId);
        }

        /// <summary>
        /// Thêm mới liên kết giữa user và provider (google).
        /// </summary>
        public async Task AddAsync(UserAuthProvider userAuthProvider)
        {
            await _dbContext.UserAuthProviders.AddAsync(userAuthProvider);
        }

        /// <summary>
        /// Lưu tất cả thay đổi (bao gồm cả thay đổi trên User vì dùng chung DbContext).
        /// </summary>
        public Task SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}
