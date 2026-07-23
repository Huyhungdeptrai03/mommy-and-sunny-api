using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        // Null nếu request không có JWT hợp lệ (endpoint public không bị chặn bởi [Authorize])
        Guid? UserId { get; }
    }
}
