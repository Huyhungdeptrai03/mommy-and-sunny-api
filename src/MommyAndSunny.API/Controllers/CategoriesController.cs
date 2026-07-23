using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Application.Categories.Services;

namespace MommyAndSunny.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var result = await _categoryService.GetCategoryTreeAsync(cancellationToken);
            return Ok(new { success = true, data = result, message = (string?)null });
        }
    }
}
