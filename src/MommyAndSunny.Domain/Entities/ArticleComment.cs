using MommyAndSunny.Domain.Common;

namespace MommyAndSunny.Domain.Entities;

public class ArticleComment : BaseEntity
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
}
