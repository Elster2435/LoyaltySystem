namespace LoyaltySystem.Core.DTOs
{
    public class CustomerActivityListItem
    {
        public int ActivityId { get; set; }

        public int? CustomerId { get; set; }

        public string CustomerFullName { get; set; } = string.Empty;

        public string ActivityType { get; set; } = string.Empty;

        public DateTime ActivityDatetime { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
