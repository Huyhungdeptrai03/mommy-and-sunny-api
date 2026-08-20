using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.Articles.DTOs
{
    public class AdminArticleFilterRequest
    {
        public ArticleStatus? Status { get; set; }
        public ArticleCategory? Category { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
