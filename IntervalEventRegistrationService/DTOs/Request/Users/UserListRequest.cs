using IntervalEventRegistrationService.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntervalEventRegistrationService.DTOs.Request.Users
{
    public class UserListRequest : PaginationRequest
    {
        public string? RoleId { get; set; } // RoleId để filter theo role (admin/organizer/staff/student), null nếu không lọc

        public string? Status { get; set; } // Status để filter theo trạng thái (active/inactive/...), null nếu không lọc
    }
}
