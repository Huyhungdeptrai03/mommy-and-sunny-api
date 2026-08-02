using MommyAndSunny.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public int Rating { get; set; } // 1-5
        public string? Comment { get; set; }
    }

}
