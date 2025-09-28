namespace TourOperator.Application.DTOs
{
    public class PricingDataDto
    {
        public string RouteCode { get; set; } = string.Empty;
        public string SeasonCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int EconomySeats { get; set; }
        public int BusinessSeats { get; set; }
        public decimal EconomyPrice { get; set; }
        public decimal BusinessPrice { get; set; }
    }
}
