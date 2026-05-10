namespace LoyaltySystem.Core.DTOs
{
    public class ReturnableTransactionItem
    {
        public int TransactionId { get; set; }

        public int CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public DateTime TransactionDatetime { get; set; }

        public decimal TransactionAmount { get; set; }

        public decimal BonusUsed { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BonusAccrued { get; set; }

        public string DisplayText =>
            $"№{TransactionId} — {CustomerFullName} — {TransactionDatetime:dd.MM.yyyy HH:mm} — оплачено {PaidAmount:N2}";
    }
}
