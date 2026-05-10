namespace LoyaltySystem.Core.DTOs
{
    public class PromotionListItem
    {
        public int PromotionId { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string PromotionType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal BonusMultiplier { get; set; }

        public decimal ExtraBonus { get; set; }

        public string RequiredLevelName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string ActivityStatus => IsActive ? "Активна" : "Отключена";
    }
}
