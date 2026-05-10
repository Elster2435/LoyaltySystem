namespace LoyaltySystem.Core.DTOs
{
    public class BonusTransactionListItem
    {
        public int BonusTransactionId { get; set; }

        public int AccountId { get; set; }

        public int? TransactionId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string BonusTransactionType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime BonusTransactionDatetime { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
