using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class BonusTransactionService
    {
        public List<BonusTransactionListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.BonusTransactions
                .AsNoTracking()
                .Include(x => x.Account)
                    .ThenInclude(x => x!.Customer)
                .OrderByDescending(x => x.BonusTransactionDatetime)
                .AsEnumerable()
                .Select(x => new BonusTransactionListItem
                {
                    BonusTransactionId = x.BonusTransactionId,
                    AccountId = x.AccountId,
                    TransactionId = x.TransactionId,
                    CustomerFullName = x.Account?.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(
                            x.Account.Customer.LastName,
                            x.Account.Customer.FirstName,
                            x.Account.Customer.MiddleName),
                    BonusTransactionType = EnumDisplayHelper.GetPgName(x.BonusTransactionType),
                    Amount = x.Amount,
                    BonusTransactionDatetime = x.BonusTransactionDatetime,
                    Description = x.Description ?? string.Empty
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
