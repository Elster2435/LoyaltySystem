using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class LoyaltyLevelService
    {
        public List<LoyaltyLevel> GetComboBoxItems()
        {
            using var db = DbContextFactory.Create();

            return db.LoyaltyLevels
                .AsNoTracking()
                .OrderBy(x => x.MinTotalSpent)
                .ToList();
        }
    }
}
