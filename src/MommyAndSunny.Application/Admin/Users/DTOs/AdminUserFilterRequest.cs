using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Admin.Users.DTOs
{
    public class AdminUserFilterRequest
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
