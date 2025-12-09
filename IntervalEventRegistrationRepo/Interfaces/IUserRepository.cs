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

        Task<(IReadOnlyList<User> Users, int TotalItems)> GetUsersForAdminAsync( // Khai báo method bất đồng bộ trả về tuple danh sách user + tổng số bản ghi
           string? keyword,    // Chuỗi keyword để search theo Name/Email; null nếu không search
           string? roleId,     // Role cần lọc (admin/organizer/staff/student); null nếu không lọc
           string? status,     // Trạng thái cần lọc (active/inactive/...); null nếu không lọc
           int pageNumber,     // Số trang hiện tại, bắt đầu từ 1
           int pageSize        // Số bản ghi trên mỗi trang
       );

        Task<User?> GetByIdAsync(string userId, bool includeDeleted = false); // Lấy một user theo Id, includeDeleted = true để cho phép Admin xem cả user đã bị soft delete


    }
}
