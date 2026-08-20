using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Admin.Articles.DTOs;
using MommyAndSunny.Application.Admin.Articles.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/admin/articles")]
    [Authorize(Roles = "Admin")]
    public class AdminArticlesController : ControllerBase
    {
        private readonly IAdminArticleService _adminArticleService;

        public AdminArticlesController(IAdminArticleService adminArticleService)
        {
            _adminArticleService = adminArticleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetArticles([FromQuery] AdminArticleFilterRequest filter, CancellationToken cancellationToken)
        {
            var result = await _adminArticleService.GetArticlesAsync(filter, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticleById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _adminArticleService.GetArticleByIdAsync(id, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPost]
        public async Task<IActionResult> CreateArticle(CreateArticleRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminArticleService.CreateArticleAsync(request, cancellationToken);
            return StatusCode(201, new { success = true, data = result, message = (string?)null });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(Guid id, UpdateArticleRequest request, CancellationToken cancellationToken)
        {
            var result = await _adminArticleService.UpdateArticleAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateArticleStatus(Guid id, UpdateArticleStatusRequest request, CancellationToken cancellationToken)
        {
            await _adminArticleService.UpdateArticleStatusAsync(id, request, cancellationToken);
            return Ok(new { success = true, data = (object?)null, message = "Cập nhật trạng thái thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken cancellationToken)
        {
            await _adminArticleService.DeleteArticleAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
