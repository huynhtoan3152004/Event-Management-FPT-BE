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

        // Hàm lấy danh sách user cho Admin, bao gồm cả user đã bị soft delete, có search + filter + paging
        public async Task<(IReadOnlyList<User> Users, int TotalItems)> GetUsersForAdminAsync( // Triển khai method bất đồng bộ trả về tuple danh sách user + tổng số bản ghi
            string? keyword,    // Nhận keyword để search theo Name/Email; null nếu không search
            string? roleId,     // Nhận roleId để filter theo vai trò; null nếu không filter
            string? status,     // Nhận status để filter theo trạng thái; null nếu không filter
            int pageNumber,     // Nhận số trang hiện tại (pageNumber bắt đầu từ 1)
            int pageSize        // Nhận số bản ghi trên mỗi trang
        )
        {
            // Khởi tạo query từ DbSet Users, dùng IgnoreQueryFilters để lấy cả user đã bị soft delete
            var query = _dbContext.Users
                .IgnoreQueryFilters()      // Bỏ Global Query Filter trên User để bao gồm luôn các bản ghi IsDeleted = true
                .Include(u => u.Role)      // Include Role để có sẵn thông tin Role và RoleName cho tầng service/API
                .AsQueryable();            // Chuyển sang IQueryable để có thể chain thêm các điều kiện động

            // Kiểm tra nếu keyword có giá trị (không null, không toàn khoảng trắng) thì áp dụng filter search
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                // Lọc những user có Name chứa keyword hoặc Email chứa keyword (có check null để tránh lỗi)
                query = query.Where(u =>
                    (u.Name != null && u.Name.Contains(keyword)) ||   // Điều kiện Name chứa keyword
                    (u.Email != null && u.Email.Contains(keyword)));  // Điều kiện Email chứa keyword
            }

            // Nếu roleId được truyền lên thì filter theo RoleId
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                // Giữ lại những user có RoleId trùng với roleId cần lọc
                query = query.Where(u => u.RoleId == roleId);
            }

            // Nếu status được truyền lên thì filter theo Status
            if (!string.IsNullOrWhiteSpace(status))
            {
                // Giữ lại những user có Status trùng với trạng thái cần lọc
                query = query.Where(u => u.Status == status);
            }

            // Lấy tổng số bản ghi sau khi đã áp dụng tất cả filter để phục vụ tính tổng số trang
            var totalItems = await query.CountAsync();

            // Tính số bản ghi cần bỏ qua dựa vào trang hiện tại (pageNumber) và kích thước trang (pageSize)
            var skip = (pageNumber - 1) * pageSize;

            // Sắp xếp mặc định theo CreatedAt mới nhất trước, sau đó sắp theo Name để danh sách ổn định
            query = query
                .OrderByDescending(u => u.CreatedAt)  // Order bản ghi mới tạo gần đây đứng trước
                .ThenBy(u => u.Name);                 // Thêm thứ tự theo Name để cùng dữ liệu thì sort ổn định

            // Lấy danh sách user cho trang hiện tại bằng cách Skip và Take rồi thực thi query
            var users = await query
                .Skip(skip)               // Bỏ qua skip bản ghi đầu để đến đúng trang cần lấy
                .Take(pageSize)           // Chỉ lấy đúng pageSize bản ghi cho trang này
                .ToListAsync();           // Thực thi query và materialize thành List<User>

            // Trả về tuple gồm danh sách user hiện tại và tổng số bản ghi để tầng service/API sử dụng
            return (users, totalItems);
        }

        public async Task<User?> GetByIdAsync(string userId, bool includeDeleted = false) // Triển khai hàm lấy chi tiết một user theo Id, có option includeDeleted
        {
            IQueryable<User> query = _dbContext.Users; // Khởi tạo query mặc định là DbSet Users (có áp dụng Global Query Filter với IsDeleted)

            if (includeDeleted) // Nếu cần lấy cả user đã bị soft delete (thường dùng ở màn Admin)
            {
                query = query.IgnoreQueryFilters(); // Bỏ Global Query Filter để không tự loại user có IsDeleted = true
            }

            var user = await query
                .Include(u => u.Role)              // Include Role để có thêm thông tin về vai trò của user
                .FirstOrDefaultAsync(u => u.UserId == userId); // Lấy bản ghi đầu tiên có UserId trùng với tham số userId

            return user; // Trả về user nếu tìm thấy, hoặc null nếu không tồn tại user với Id tương ứng
        }
    }
}
