using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class CustomerTransaction
    {
        public int TransactionId { get; set; }

        public TransactionTypeEnum TransactionType { get; set; }

        public int? OriginalTransactionId { get; set; }

        public int? CustomerId { get; set; }

        public DateTime TransactionDatetime { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal BonusUsed { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BonusAccrued { get; set; }

        public decimal BonusCompensationAmount { get; set; }

        public TransactionChannelEnum TransactionChannel { get; set; }

        public int? PromotionId { get; set; }

        public int? OfferId { get; set; }

        public string? Comment { get; set; }

        public Customer? Customer { get; set; }

        public Promotion? Promotion { get; set; }

        public CustomerOffer? Offer { get; set; }

        public CustomerTransaction? OriginalTransaction { get; set; }

        public List<CustomerTransaction> ReturnTransactions { get; set; } = new();

        public List<BonusTransaction> BonusTransactions { get; set; } = new();
    }
}
