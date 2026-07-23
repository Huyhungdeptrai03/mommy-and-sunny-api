using MommyAndSunny.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Lưu HASH của token, không lưu token thô — nếu database bị lộ,
        // kẻ tấn công vẫn không thể dùng hash để giả mạo refresh token thật
        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        // Computed property — không map vào cột database (Ignore ở Configuration)
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
