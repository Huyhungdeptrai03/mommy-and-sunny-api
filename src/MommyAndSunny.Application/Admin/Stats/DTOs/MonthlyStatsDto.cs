namespace MommyAndSunny.Application.Admin.Stats.DTOs
{
    public class MonthlyStatsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
