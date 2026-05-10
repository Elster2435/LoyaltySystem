namespace LoyaltySystem.Core.DTOs
{
    public class CustomerOfferListItem
    {
        public int OfferId { get; set; }

        public int CustomerId { get; set; }

        public int PromotionId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string PromotionName { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string OfferStatus { get; set; } = string.Empty;
    }
}
