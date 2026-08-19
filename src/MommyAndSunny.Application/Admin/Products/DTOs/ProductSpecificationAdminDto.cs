namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class ProductSpecificationAdminDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
