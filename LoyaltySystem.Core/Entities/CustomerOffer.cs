using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class CustomerOffer
    {
        public int OfferId { get; set; }

        public int CustomerId { get; set; }

        public int PromotionId { get; set; }

        public DateTime AssignedAt { get; set; }

        public DateTime? ValidUntil { get; set; }

        public OfferStatusEnum OfferStatus { get; set; }

        public Customer? Customer { get; set; }

        public Promotion? Promotion { get; set; }

        public List<CustomerTransaction> Transactions { get; set; } = new();
    }
}
