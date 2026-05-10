namespace LoyaltySystem.Core.DTOs
{
    public class TransactionBonusConditionItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DisplayText => Name;
    }
}
