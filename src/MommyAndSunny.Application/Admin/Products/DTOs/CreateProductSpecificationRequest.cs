namespace MommyAndSunny.Application.Admin.Products.DTOs
{
    public class CreateProductSpecificationRequest
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0;
    }
}
