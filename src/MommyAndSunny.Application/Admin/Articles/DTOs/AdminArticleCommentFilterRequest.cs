namespace MommyAndSunny.Application.Admin.Articles.DTOs
{
    public class AdminArticleCommentFilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
