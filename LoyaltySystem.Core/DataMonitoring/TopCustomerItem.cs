namespace LoyaltySystem.Core.DataMonitoring
{
    public class TopCustomerItem
    {
        public int CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string LevelName { get; set; } = string.Empty;

        public decimal TotalSpent { get; set; }

        public decimal BonusBalance { get; set; }

        public int PurchaseCount { get; set; }

        public int ReturnCount { get; set; }

        public DateTime? LastPurchaseDate { get; set; }
    }
}
