namespace LoyaltySystem.Core.DataMonitoring
{
    public class LoyaltyLevelStatisticsItem
    {
        public string LevelName { get; set; } = string.Empty;

        public int CustomerCount { get; set; }

        public decimal TotalSpent { get; set; }

        public decimal AverageBonusBalance { get; set; }
    }
}
