using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Articles.DTOs;
using MommyAndSunny.Application.Articles.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;

        public ArticlesController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetArticles([FromQuery] ArticleFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _articleService.GetArticlesAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetArticleBySlug(string slug, CancellationToken cancellationToken)
        {
            var result = await _articleService.GetArticleBySlugAsync(slug, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
