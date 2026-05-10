using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltySystem.Core.Entities
{
    public class LoyaltyLevel
    {
        public int LevelId { get; set; }

        public string LevelName { get; set; } = string.Empty;

        public decimal MinTotalSpent { get; set; }

        public decimal BonusPercent { get; set; }

        public List<CustomerLoyaltyAccount> LoyaltyAccounts { get; set; } = new();

        public List<Promotion> Promotions { get; set; } = new();
    }
}
