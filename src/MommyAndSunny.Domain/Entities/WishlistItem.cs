using MommyAndSunny.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Domain.Entities
{
    public class WishlistItem : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
