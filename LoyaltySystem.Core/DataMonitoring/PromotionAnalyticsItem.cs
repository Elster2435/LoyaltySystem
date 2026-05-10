namespace LoyaltySystem.Core.DataMonitoring
{
    public class PromotionAnalyticsItem
    {
        public int PromotionId { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string PromotionType { get; set; } = string.Empty;

        public int UsageCount { get; set; }

        public decimal GrossPurchaseAmount { get; set; }

        public decimal NetRevenue { get; set; }

        public decimal BonusAccrued { get; set; }

        public decimal BonusUsed { get; set; }
    }
}
