using MommyAndSunny.Domain.Enums;

namespace MommyAndSunny.Application.Admin.Articles.DTOs
{
    public class UpdateArticleStatusRequest
    {
        public ArticleStatus Status { get; set; }
    }
}
