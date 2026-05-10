namespace LoyaltySystem.Core.DataMonitoring
{
    public class OfferAnalyticsItem
    {
        public int PromotionId { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string PromotionType { get; set; } = string.Empty;

        public int AssignedCount { get; set; }

        public int UsedCount { get; set; }

        public int ExpiredCount { get; set; }

        public int CancelledCount { get; set; }

        public decimal UsagePercent
        {
            get
            {
                if (AssignedCount == 0)
                    return 0;

                return Math.Round((decimal)UsedCount / AssignedCount * 100, 2);
            }
        }
    }
}
