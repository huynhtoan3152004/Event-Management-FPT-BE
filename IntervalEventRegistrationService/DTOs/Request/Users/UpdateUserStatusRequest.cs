using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Users
{
    public class UpdateUserStatusRequest // DTO dùng cho Admin khi muốn thay đổi trạng thái của một user
    {
        public string Status { get; set; } = "active"; // Trạng thái mới của user (ví dụ: active, inactive, banned...), mặc định là active
    }
}
