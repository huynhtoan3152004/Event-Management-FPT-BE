using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Users
{
    public class CreateUserRequest // DTO dùng để nhận dữ liệu tạo tài khoản mới (organizer/staff) từ phía Admin
    {
        [Required] // Bắt buộc Admin phải nhập Email khi tạo tài khoản
        [EmailAddress] // Validate Email đúng định dạng email
        [StringLength(100)] // Giới hạn độ dài tối đa của Email là 100 ký tự
        public string Email { get; set; } = string.Empty; // Thuộc tính Email lưu email đăng nhập của user mới

        [Required] // Bắt buộc phải nhập Name cho user
        [StringLength(100)] // Giới hạn độ dài tên tối đa 100 ký tự
        public string Name { get; set; } = string.Empty; // Thuộc tính Name lưu tên hiển thị của user

        [Required] // Bắt buộc phải nhập Password
        [StringLength(100, MinimumLength = 6)] // Yêu cầu password tối thiểu 6 ký tự, tối đa 100 ký tự
        public string Password { get; set; } = string.Empty; // Thuộc tính Password dùng để nhận mật khẩu plaintext từ client

        [Required] // Bắt buộc phải nhập ConfirmPassword
        [Compare("Password")] // Yêu cầu ConfirmPassword phải trùng với Password
        public string ConfirmPassword { get; set; } = string.Empty; // Thuộc tính ConfirmPassword dùng để xác nhận lại mật khẩu người dùng nhập

        [Required] // Bắt buộc phải chọn Role khi tạo tài khoản
        [StringLength(50)] // Giới hạn độ dài RoleId tối đa 50 ký tự
        public string RoleId { get; set; } = string.Empty; // Thuộc tính RoleId lưu role của user (chỉ cho phép organizer hoặc staff)

        [StringLength(200)] // Giới hạn độ dài Organization tối đa 200 ký tự
        public string? Organization { get; set; } // Thuộc tính Organization lưu tên tổ chức/đơn vị user thuộc về (nếu có)

        [StringLength(100)] // Giới hạn độ dài Department tối đa 100 ký tự
        public string? Department { get; set; } // Thuộc tính Department lưu tên phòng ban cụ thể (nếu có)
    }
}
