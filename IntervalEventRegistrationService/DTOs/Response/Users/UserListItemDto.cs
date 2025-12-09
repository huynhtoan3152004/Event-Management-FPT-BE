using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Users
{
    public class UserListItemDto // DTO dùng để hiển thị từng dòng trong danh sách Users của Admin
    {
        public string UserId { get; set; } = string.Empty; // Id của user, dùng để thao tác chi tiết (xem chi tiết, đổi role, đổi status...)

        public string? Name { get; set; } // Tên hiển thị của user, có thể null nếu user chưa cập nhật

        public string? Email { get; set; } // Email đăng nhập / liên hệ của user

        public string? Phone { get; set; } // Số điện thoại của user (nếu có)

        public string? RoleId { get; set; } // Mã role hiện tại của user (admin/organizer/staff/student)

        public string? RoleName { get; set; } // Tên role để hiển thị dễ đọc hơn (Admin, Organizer, Staff, Student)

        public string Status { get; set; } = "active"; // Trạng thái hiện tại của user (active/inactive/...)

        public bool IsDeleted { get; set; } // Cờ đánh dấu user đã bị soft delete hay chưa, Admin vẫn nhìn thấy

        public DateTime? CreatedAt { get; set; } // Thời gian user được tạo, dùng để sort hoặc hiển thị thông tin thêm
    }
}
