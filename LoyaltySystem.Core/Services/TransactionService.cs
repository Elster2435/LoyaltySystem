using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class TransactionService
    {
        public List<TransactionListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.Transactions
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Promotion)
                .Include(x => x.Offer)
                    .ThenInclude(x => x!.Promotion)
                .OrderByDescending(x => x.TransactionDatetime)
                .AsEnumerable()
                .Select(x => new TransactionListItem
                {
                    TransactionId = x.TransactionId,
                    TransactionType = EnumDisplayHelper.GetPgName(x.TransactionType),
                    OriginalTransactionId = x.OriginalTransactionId,
                    CustomerId = x.CustomerId,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    TransactionDatetime = x.TransactionDatetime,
                    TransactionAmount = x.TransactionAmount,
                    BonusUsed = x.BonusUsed,
                    PaidAmount = x.PaidAmount,
                    BonusAccrued = x.BonusAccrued,
                    TransactionChannel = EnumDisplayHelper.GetPgName(x.TransactionChannel),
                    PromotionId = x.PromotionId,
                    OfferId = x.OfferId,
                    BonusConditionName = GetBonusConditionName(x),
                    Comment = x.Comment ?? string.Empty
                })
                .ToList();
        }

        public CustomerTransaction? GetById(int transactionId)
        {
            using var db = DbContextFactory.Create();

            return db.Transactions
                .Include(x => x.Customer)
                .Include(x => x.Promotion)
                .Include(x => x.Offer)
                    .ThenInclude(x => x!.Promotion)
                .FirstOrDefault(x => x.TransactionId == transactionId);
        }

        public void AddPurchase(CustomerTransaction transaction)
        {
            using var db = DbContextFactory.Create();

            transaction.TransactionType = TransactionTypeEnum.Purchase;
            transaction.OriginalTransactionId = null;
            transaction.PaidAmount = 0;
            transaction.BonusAccrued = 0;

            db.Transactions.Add(transaction);
            db.SaveChanges();
        }

        public void AddReturn(int originalTransactionId, string? comment = null)
        {
            using var db = DbContextFactory.Create();

            var transaction = new CustomerTransaction
            {
                TransactionType = TransactionTypeEnum.Return,
                OriginalTransactionId = originalTransactionId,
                TransactionAmount = 0,
                BonusUsed = 0,
                PaidAmount = 0,
                BonusAccrued = 0,
                TransactionChannel = TransactionChannelEnum.Offline,
                Comment = string.IsNullOrWhiteSpace(comment)
                    ? "Возврат покупки"
                    : comment.Trim()
            };

            db.Transactions.Add(transaction);
            db.SaveChanges();
        }

        public List<ReturnableTransactionItem> GetReturnableTransactions()
        {
            using var db = DbContextFactory.Create();

            var returnedTransactionIds = db.Transactions
                .AsNoTracking()
                .Where(x => x.TransactionType == TransactionTypeEnum.Return &&
                            x.OriginalTransactionId != null)
                .Select(x => x.OriginalTransactionId!.Value);

            return db.Transactions
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x =>
                    x.TransactionType == TransactionTypeEnum.Purchase &&
                    x.CustomerId != null &&
                    !returnedTransactionIds.Contains(x.TransactionId))
                .OrderByDescending(x => x.TransactionDatetime)
                .AsEnumerable()
                .Select(x => new ReturnableTransactionItem
                {
                    TransactionId = x.TransactionId,
                    CustomerId = x.CustomerId!.Value,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    TransactionDatetime = x.TransactionDatetime,
                    TransactionAmount = x.TransactionAmount,
                    BonusUsed = x.BonusUsed,
                    PaidAmount = x.PaidAmount,
                    BonusAccrued = x.BonusAccrued
                })
                .ToList();
        }

        public List<ReturnableTransactionItem> GetReturnableTransactionsByCustomer(int customerId)
        {
            using var db = DbContextFactory.Create();

            var returnedTransactionIds = db.Transactions
                .AsNoTracking()
                .Where(x => x.TransactionType == TransactionTypeEnum.Return &&
                            x.OriginalTransactionId != null)
                .Select(x => x.OriginalTransactionId!.Value);

            return db.Transactions
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x =>
                    x.TransactionType == TransactionTypeEnum.Purchase &&
                    x.CustomerId == customerId &&
                    !returnedTransactionIds.Contains(x.TransactionId))
                .OrderByDescending(x => x.TransactionDatetime)
                .AsEnumerable()
                .Select(x => new ReturnableTransactionItem
                {
                    TransactionId = x.TransactionId,
                    CustomerId = x.CustomerId!.Value,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    TransactionDatetime = x.TransactionDatetime,
                    TransactionAmount = x.TransactionAmount,
                    BonusUsed = x.BonusUsed,
                    PaidAmount = x.PaidAmount,
                    BonusAccrued = x.BonusAccrued
                })
                .ToList();
        }

        private static string GetBonusConditionName(CustomerTransaction transaction)
        {
            if (transaction.Offer?.Promotion != null)
                return "Предложение: " + transaction.Offer.Promotion.PromotionName;

            if (transaction.Promotion != null)
                return "Акция: " + transaction.Promotion.PromotionName;

            return "Не применялось";
        }

        private static string BuildFullName(string lastName, string firstName, string? middleName)
        {
            return string.IsNullOrWhiteSpace(middleName)
                ? $"{lastName} {firstName}"
                : $"{lastName} {firstName} {middleName}";
        }
    }
}
