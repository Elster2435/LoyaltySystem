using LoyaltySystem.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltySystem.Core.Entities
{
    public class CustomerLoyaltyAccount
    {
        public int AccountId { get; set; }

        public int CustomerId { get; set; }

        public int LevelId { get; set; }

        public decimal BonusBalance { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime CreatedAt { get; set; }

        public StatusEnum AccountStatus { get; set; }

        public Customer? Customer { get; set; }

        public LoyaltyLevel? Level { get; set; }

        public List<BonusTransaction> BonusTransactions { get; set; } = new();
    }
}
