namespace MommyAndSunny.Application.PersonalImportRequests.DTOs
{
    public class PersonalImportRequestDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
