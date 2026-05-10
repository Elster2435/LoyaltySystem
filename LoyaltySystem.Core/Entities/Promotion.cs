using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class Promotion
    {
        public int PromotionId { get; set; }

        public PromotionTypeEnum PromotionType { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal BonusMultiplier { get; set; }

        public decimal ExtraBonus { get; set; }

        public int? RequiredLevelId { get; set; }

        public bool IsActive { get; set; }

        public LoyaltyLevel? RequiredLevel { get; set; }

        public List<CustomerOffer> Offers { get; set; } = new();

        public List<CustomerTransaction> Transactions { get; set; } = new();
    }
}
