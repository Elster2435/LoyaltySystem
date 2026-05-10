namespace LoyaltySystem.Core.DataMonitoring
{
    public class MonitoringSummary
    {
        public int TotalCustomers { get; set; }

        public int ActiveCustomers { get; set; }

        public int TotalPurchases { get; set; }

        public int TotalReturns { get; set; }

        public decimal GrossPurchaseAmount { get; set; }

        public decimal NetRevenue { get; set; }

        public decimal TotalBonusAccrued { get; set; }

        public decimal TotalBonusUsed { get; set; }

        public decimal TotalBonusReturned { get; set; }

        public decimal TotalBonusCancelled { get; set; }

        public int ActiveOffers { get; set; }

        public int ActivePromotions { get; set; }
    }
}
