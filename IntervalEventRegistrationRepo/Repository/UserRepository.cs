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
    public class UserRepository : IUserRepository
    {
        // DbContext dùng để truy vấn / ghi dữ liệu
        private readonly ApplicationDbContext _dbContext;

        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Lấy user theo UserId, include luôn Role nếu cần dùng.
        /// </summary>
        public async Task<User?> GetByIdAsync(string userId)
        {
            return await _dbContext.Users
                .Include(u => u.Role) // include Role để sau này dùng RoleName
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
        }

        /// <summary>
        /// Lấy user theo email (không lấy user đã bị đánh dấu xóa).
        /// Dùng cho case login Google: tìm xem email này đã có account chưa.
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        /// <summary>
        /// Thêm user mới vào DbSet. Chưa lưu xuống DB cho đến khi gọi SaveChangesAsync.
        /// </summary>
        public async Task AddAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
        }

        /// <summary>
        /// Đánh dấu user là modified để EF Core biết cần update khi SaveChangesAsync.
        /// (Nếu entity đang được track sẵn thì có thể không cần gọi hàm này,
        /// nhưng viết ra cho rõ intent.)
        /// </summary>
        public void Update(User user)
        {
            _dbContext.Users.Update(user);
        }

        /// <summary>
        /// Lưu tất cả thay đổi (add/update/delete) đang tracked bởi DbContext xuống database.
        /// </summary>
        public Task SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}
