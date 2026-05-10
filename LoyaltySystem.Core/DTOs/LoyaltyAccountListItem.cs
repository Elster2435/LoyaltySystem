namespace LoyaltySystem.Core.DTOs
{
    public class LoyaltyAccountListItem
    {
        public int AccountId { get; set; }

        public int CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string LevelName { get; set; } = string.Empty;

        public decimal BonusBalance { get; set; }

        public decimal TotalSpent { get; set; }

        public string AccountStatus { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
