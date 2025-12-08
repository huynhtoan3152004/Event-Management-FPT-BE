using IntervalEventRegistrationRepo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationRepo.Interfaces
{
    /// <summary>
    /// Repository làm việc với bảng users:
    /// - Tìm user theo Id / Email
    /// - Thêm / cập nhật user
    /// - Lưu thay đổi xuống database
    /// </summary>
    public interface IUserRepository
    {
        // Lấy user theo id (dùng cho các chức năng khác nếu cần)
        Task<User?> GetByIdAsync(string userId);

        // Lấy user theo email (dùng cho flow login Google)
        Task<User?> GetByEmailAsync(string email);

        // Thêm user mới
        Task AddAsync(User user);

        // Cập nhật user (EF sẽ track entity, mình chỉ cần mark là modified nếu cần)
        void Update(User user);

        // Lưu thay đổi xuống database
        Task SaveChangesAsync();
    }
}
