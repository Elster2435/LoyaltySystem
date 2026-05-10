using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class CustomerActivity
    {
        public int ActivityId { get; set; }

        public int? CustomerId { get; set; }

        public ActivityTypeEnum ActivityType { get; set; }

        public DateTime ActivityDatetime { get; set; }

        public string? Description { get; set; }

        public Customer? Customer { get; set; }
    }
}
