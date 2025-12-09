using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Users
{
    public class UserDetailDto // DTO dùng để hiển thị chi tiết đầy đủ của một user cho Admin
    {
        public string UserId { get; set; } = string.Empty; // Id duy nhất của user trong hệ thống

        public string? RoleId { get; set; } // Mã role hiện tại của user (admin/organizer/staff/student)

        public string? RoleName { get; set; } // Tên role tương ứng để Admin đọc dễ hiểu hơn

        public string? Name { get; set; } // Tên hiển thị đầy đủ của user

        public string? Email { get; set; } // Email đăng nhập của user

        public string? Phone { get; set; } // Số điện thoại liên lạc của user

        public string? AvatarUrl { get; set; } // Link ảnh avatar của user (nếu có)

        public string Status { get; set; } = "active"; // Trạng thái hoạt động hiện tại của user

        public bool IsDeleted { get; set; } // Cờ cho biết user đã bị soft delete hay chưa

        public string? StudentCode { get; set; } // Mã sinh viên nếu user là student

        public string? Organization { get; set; } // Tổ chức/đơn vị mà user thuộc về

        public string? Department { get; set; } // Phòng ban cụ thể của user (nếu có)

        public bool EmailVerified { get; set; } // Cờ cho biết email đã được verify hay chưa

        public DateTime? LastLoginAt { get; set; } // Thời điểm user đăng nhập gần nhất

        public DateTime? CreatedAt { get; set; } // Thời điểm tạo user trong hệ thống

        public DateTime? UpdatedAt { get; set; } // Thời điểm user được cập nhật thông tin gần nhất
    }

}
