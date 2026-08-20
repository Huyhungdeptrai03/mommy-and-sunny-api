using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Articles.DTOs;
using MommyAndSunny.Application.Articles.Services;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/articles")]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ICurrentUserService _currentUserService;

        public ArticlesController(IArticleService articleService, ICurrentUserService currentUserService)
        {
            _articleService = articleService;
            _currentUserService = currentUserService;
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

        [HttpGet("{articleId}/comments")]
        public async Task<IActionResult> GetComments(Guid articleId, CancellationToken cancellationToken)
        {
            var result = await _articleService.GetCommentsAsync(articleId, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost("{articleId}/comments")]
        [Authorize]
        public async Task<IActionResult> CreateComment(Guid articleId, CreateArticleCommentRequest request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Yêu cầu đăng nhập");
            var result = await _articleService.CreateCommentAsync(articleId, userId, request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = "Đã gửi bình luận" });
        }
    }
}
