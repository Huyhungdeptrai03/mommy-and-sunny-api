using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly IWebHostEnvironment _environment;

        public UploadsController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("image")]
        [EnableRateLimiting("uploads")]
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

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new DomainException("Chỉ chấp nhận ảnh định dạng JPG, PNG hoặc WEBP");
            }

            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var input = file.OpenReadStream();
            var header = new byte[12];
            var bytesRead = await input.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
            if (!HasValidImageSignature(extension, header.AsSpan(0, bytesRead)))
            {
                throw new DomainException("Nội dung file không khớp với định dạng ảnh");
            }

            input.Position = 0;
            await using var output = new FileStream(
                filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            await input.CopyToAsync(output, cancellationToken);

            var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            return StatusCode(201, new { success = true, data = new { url }, message = (string?)null });
        }

        private static bool HasValidImageSignature(string extension, ReadOnlySpan<byte> header)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => header.Length >= 3 &&
                    header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => header.Length >= 12 &&
                    header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8),
                _ => false
            };
        }
    }
}
