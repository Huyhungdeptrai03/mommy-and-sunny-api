using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.Domain.Exceptions;

namespace MommyAndSunny.API.Controllers
{
    // Public — dùng cho các form không bắt buộc đăng nhập (VD: yêu cầu đặt hàng xách tay)
    // cần đính kèm ảnh. Lưu trực tiếp vào wwwroot/uploads vì dự án chưa cấu hình storage
    // ngoài (S3/Cloudinary) — đủ dùng cho quy mô hiện tại, không cần thêm dependency mới.
    [ApiController]
    [Route("api/uploads")]
    public class UploadsController : ControllerBase
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IWebHostEnvironment _environment;

        public UploadsController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("image")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                throw new DomainException("Vui lòng chọn một ảnh để tải lên");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new DomainException("Kích thước ảnh tối đa 5MB");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                throw new DomainException("Chỉ chấp nhận ảnh định dạng JPG, PNG, WEBP hoặc GIF");
            }

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            return StatusCode(201, new { success = true, data = new { url }, message = (string?)null });
        }
    }
}
