using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Users
{
    public class UpdateMyProfileRequest // DTO nhận dữ liệu từ client khi user tự cập nhật profile của mình
    {
        public string Name { get; set; } = string.Empty; // Họ tên hiển thị của user, bắt buộc, không được để trống

        public string? Phone { get; set; } // Số điện thoại liên hệ, có thể null nếu user chưa cung cấp

        public string? AvatarUrl { get; set; } // Đường dẫn ảnh đại diện, để FE hiển thị avatar của user

        public string? StudentCode { get; set; } // Mã sinh viên (chỉ meaningful nếu user là student, nhưng vẫn để dạng string tự do)

        public string? Organization { get; set; } // Tổ chức/đơn vị mà user đang thuộc về (dùng cho organizer/staff)

        public string? Department { get; set; } // Phòng ban cụ thể của user (dùng cho staff/organizer nếu có)
    }
}
