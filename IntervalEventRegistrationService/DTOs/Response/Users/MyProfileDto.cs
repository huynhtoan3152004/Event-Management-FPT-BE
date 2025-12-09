using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Response.Users
{
    public class MyProfileDto // DTO dùng để trả về thông tin profile của user đang đăng nhập
    {
        public string UserId { get; set; } = string.Empty; // Id của user hiện tại, giúp FE lưu trữ hoặc debug nếu cần

        public string? RoleId { get; set; } // Mã role hiện tại của user (admin/organizer/staff/student)

        public string? RoleName { get; set; } // Tên role tương ứng để hiển thị cho người dùng (ví dụ: "Sinh viên")

        public string? Name { get; set; } // Tên hiển thị của user

        public string? Email { get; set; } // Email đăng nhập của user

        public string? Phone { get; set; } // Số điện thoại liên hệ

        public string? AvatarUrl { get; set; } // Đường dẫn ảnh avatar cho profile

        public string Status { get; set; } = "active"; // Trạng thái hiện tại của account, để user thấy nếu account bị khóa

        public string? StudentCode { get; set; } // Mã sinh viên nếu user là student

        public string? Organization { get; set; } // Tổ chức/đơn vị mà user đang thuộc về

        public string? Department { get; set; } // Phòng ban của user

        public bool EmailVerified { get; set; } // Cho biết email đã xác thực hay chưa, tiện show nhắc nhở user

        public DateTime? LastLoginAt { get; set; } // Lần đăng nhập gần nhất của user

        public DateTime? CreatedAt { get; set; } // Thời điểm tài khoản được tạo

        public DateTime? UpdatedAt { get; set; } // Thời điểm profile được cập nhật gần nhất
    }
}
