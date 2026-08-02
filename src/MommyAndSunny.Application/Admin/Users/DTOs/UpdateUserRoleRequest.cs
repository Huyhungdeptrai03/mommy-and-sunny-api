using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Users.DTOs
{
    public class UpdateUserRoleRequest
    {
        // "Admin" hoặc "Customer"
        public string Role { get; set; } = string.Empty;
    }
}
