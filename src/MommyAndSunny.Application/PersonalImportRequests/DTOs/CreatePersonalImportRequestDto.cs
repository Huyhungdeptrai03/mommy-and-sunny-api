namespace MommyAndSunny.Application.PersonalImportRequests.DTOs
{
    public class CreatePersonalImportRequestDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ProductLink { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Notes { get; set; }
        public string? ImageUrl { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
    }
}
