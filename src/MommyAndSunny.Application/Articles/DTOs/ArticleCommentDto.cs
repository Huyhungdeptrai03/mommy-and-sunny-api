namespace MommyAndSunny.Application.Articles.DTOs
{
    public class ArticleCommentDto
    {
        public Guid Id { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
