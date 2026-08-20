using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.Articles.DTOs
{
    public class CreateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string? AuthorName { get; set; }
        public ArticleCategory Category { get; set; }
    }
}
