namespace TourOperator.Domain.Entities
{
    public class PricingRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TourOperatorId { get; set; }
        public string RouteCode { get; set; } = null!;
        public string SeasonCode { get; set; } = null!;
        public DateTime Date { get; set; }
        public int EconomySeats { get; set; }
        public int BusinessSeats { get; set; }
        public decimal EconomyPrice { get; set; }
        public decimal BusinessPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
