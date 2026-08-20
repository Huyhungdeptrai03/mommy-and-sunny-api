namespace MommyAndSunny.Application.Articles.DTOs
{
    public class ArticleListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? AuthorName { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
    }
}
