namespace LoyaltySystem.Core.DTOs
{
    public class TransactionListItem
    {
        public int TransactionId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public int? OriginalTransactionId { get; set; }

        public int? CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public DateTime TransactionDatetime { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal BonusUsed { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BonusAccrued { get; set; }

        public string TransactionChannel { get; set; } = string.Empty;

        public int? PromotionId { get; set; }

        public int? OfferId { get; set; }

        public string BonusConditionName { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;
    }
}
