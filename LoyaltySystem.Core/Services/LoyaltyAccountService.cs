using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class LoyaltyAccountService
    {
        public List<LoyaltyAccountListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.CustomerLoyaltyAccounts
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Level)
                .OrderBy(x => x.Customer!.LastName)
                .ThenBy(x => x.Customer!.FirstName)
                .AsEnumerable()
                .Select(x => new LoyaltyAccountListItem
                {
                    AccountId = x.AccountId,
                    CustomerId = x.CustomerId,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    Phone = x.Customer?.Phone ?? string.Empty,
                    LevelName = x.Level?.LevelName ?? "Не указан",
                    BonusBalance = x.BonusBalance,
                    TotalSpent = x.TotalSpent,
                    AccountStatus = EnumDisplayHelper.GetPgName(x.AccountStatus),
                    CreatedAt = x.CreatedAt
                })
                .ToList();
        }

        private static string BuildFullName(string lastName, string firstName, string? middleName)
        {
            return string.IsNullOrWhiteSpace(middleName)
                ? $"{lastName} {firstName}"
                : $"{lastName} {firstName} {middleName}";
        }
    }
}
