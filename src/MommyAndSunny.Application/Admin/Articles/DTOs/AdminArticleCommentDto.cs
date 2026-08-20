namespace MommyAndSunny.Application.Admin.Articles.DTOs
{
    public class AdminArticleCommentDto
    {
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public string ArticleTitle { get; set; } = string.Empty;
        public string ArticleSlug { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
