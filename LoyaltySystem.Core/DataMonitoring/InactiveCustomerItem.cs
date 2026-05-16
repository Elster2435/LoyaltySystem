namespace LoyaltySystem.Core.DataMonitoring
{
    public class InactiveCustomerItem
    {
        public int CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string LevelName { get; set; } = string.Empty;

        public decimal BonusBalance { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? LastPurchaseDate { get; set; }

        public int DaysSinceLastPurchase { get; set; }

        public bool HasPurchases { get; set; }
    }
}
