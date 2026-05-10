namespace LoyaltySystem.Core.DTOs
{
    public class CustomerComboBoxItem
    {
        public int CustomerId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string DisplayText => $"{FullName} — {Phone}";
    }
}
