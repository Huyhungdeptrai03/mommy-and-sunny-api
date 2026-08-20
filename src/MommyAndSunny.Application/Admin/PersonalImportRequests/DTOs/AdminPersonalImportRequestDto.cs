namespace MommyAndSunny.Application.Admin.PersonalImportRequests.DTOs
{
    public class AdminPersonalImportRequestDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductLink { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public string? ImageUrl { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }

        public Guid? UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
