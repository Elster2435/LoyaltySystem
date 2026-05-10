using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class BonusTransaction
    {
        public int BonusTransactionId { get; set; }

        public int AccountId { get; set; }

        public int? TransactionId { get; set; }

        public BonusTransactionTypeEnum BonusTransactionType { get; set; }

        public decimal Amount { get; set; }

        public DateTime BonusTransactionDatetime { get; set; }

        public string? Description { get; set; }

        public CustomerLoyaltyAccount? Account { get; set; }

        public CustomerTransaction? Transaction { get; set; }
    }
}
