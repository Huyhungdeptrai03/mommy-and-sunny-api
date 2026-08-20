using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Articles.DTOs
{
    public class ArticleFilterRequest
    {
        public ArticleCategory? Category { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
