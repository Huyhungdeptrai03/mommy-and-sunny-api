namespace MommyAndSunny.Application.Admin.Stats.DTOs
{
    public class CustomerStatsFilterRequest
    {
        public string? Search { get; set; } // Tìm theo tên hoặc email khách hàng
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
