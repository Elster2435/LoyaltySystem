namespace LoyaltySystem.Core.DTOs
{
    public class PromotionComboBoxItem
    {
        public int PromotionId { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string DisplayText => PromotionName;
    }
}
