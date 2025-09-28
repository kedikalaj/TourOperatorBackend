namespace TourOperator.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Role { get; set; } = "TourOperator";
        public Guid? TourOperatorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
