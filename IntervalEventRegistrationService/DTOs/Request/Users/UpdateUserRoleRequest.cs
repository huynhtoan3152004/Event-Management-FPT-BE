using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Users
{
    public class UpdateUserRoleRequest // DTO dùng cho Admin khi muốn đổi vai trò (role) của một user
    {
        public string RoleId { get; set; } = string.Empty; // RoleId mới cần gán cho user (ví dụ: admin, organizer, staff, student)
    }
}
