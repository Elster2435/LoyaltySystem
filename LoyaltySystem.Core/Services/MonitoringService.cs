using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DataMonitoring;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class MonitoringService
    {
        public MonitoringSummary GetSummary()
        {
            using var db = DbContextFactory.Create();

            var purchases = db.Transactions
                .AsNoTracking()
                .Where(x => x.TransactionType == TransactionTypeEnum.Purchase);

            var returns = db.Transactions
                .AsNoTracking()
                .Where(x => x.TransactionType == TransactionTypeEnum.Return);

            return new MonitoringSummary
            {
                TotalCustomers = db.Customers.Count(),

                ActiveCustomers = db.Customers
                    .Count(x => x.Status == StatusEnum.Active),

                TotalPurchases = purchases.Count(),

                TotalReturns = returns.Count(),

                GrossPurchaseAmount = purchases
                    .Sum(x => (decimal?)x.TransactionAmount) ?? 0,

                NetRevenue =
                    (purchases.Sum(x => (decimal?)x.PaidAmount) ?? 0)
                    -
                    (returns.Sum(x => (decimal?)x.PaidAmount) ?? 0),

                TotalBonusAccrued = purchases
                    .Sum(x => (decimal?)x.BonusAccrued) ?? 0,

                TotalBonusUsed = purchases
                    .Sum(x => (decimal?)x.BonusUsed) ?? 0,

                TotalBonusReturned = returns
                    .Sum(x => (decimal?)x.BonusUsed) ?? 0,

                TotalBonusCancelled = returns
                    .Sum(x => (decimal?)x.BonusAccrued) ?? 0,

                TotalBonusCompensationAmount = returns
                    .Sum(x => (decimal?)x.BonusCompensationAmount) ?? 0,

                ActiveOffers = db.CustomerOffers
                    .Count(x => x.OfferStatus == OfferStatusEnum.Assigned),

                ActivePromotions = db.Promotions
                    .Count(x => x.IsActive)
            };
        }

        public MonitoringPeriodSummary GetPeriodSummary(DateTime? startDate, DateTime? endDate)
        {
            using var db = DbContextFactory.Create();

            var query = db.Transactions
                .AsNoTracking()
                .AsQueryable();

            if (startDate != null)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified);

                query = query.Where(x => x.TransactionDatetime >= start);
            }

            if (endDate != null)
            {
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified);

                query = query.Where(x => x.TransactionDatetime < end);
            }

            var purchases = query
                .Where(x => x.TransactionType == TransactionTypeEnum.Purchase);

            var returns = query
                .Where(x => x.TransactionType == TransactionTypeEnum.Return);

            return new MonitoringPeriodSummary
            {
                TotalPurchases = purchases.Count(),

                TotalReturns = returns.Count(),

                GrossPurchaseAmount = purchases
                    .Sum(x => (decimal?)x.TransactionAmount) ?? 0,

                NetRevenue =
                    (purchases.Sum(x => (decimal?)x.PaidAmount) ?? 0)
                    -
                    (returns.Sum(x => (decimal?)x.PaidAmount) ?? 0),

                TotalBonusAccrued = purchases
                    .Sum(x => (decimal?)x.BonusAccrued) ?? 0,

                TotalBonusUsed = purchases
                    .Sum(x => (decimal?)x.BonusUsed) ?? 0,

                TotalBonusReturned = returns
                    .Sum(x => (decimal?)x.BonusUsed) ?? 0,

                TotalBonusCancelled = returns
                    .Sum(x => (decimal?)x.BonusAccrued) ?? 0,

                TotalBonusCompensationAmount = returns
                    .Sum(x => (decimal?)x.BonusCompensationAmount) ?? 0
            };
        }

        public List<LoyaltyLevelStatisticsItem> GetLoyaltyLevelStatistics()
        {
            using var db = DbContextFactory.Create();

            return db.CustomerLoyaltyAccounts
                .AsNoTracking()
                .Include(x => x.Level)
                .AsEnumerable()
                .GroupBy(x => x.Level?.LevelName ?? "Не указан")
                .Select(g => new LoyaltyLevelStatisticsItem
                {
                    LevelName = g.Key,
                    CustomerCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalSpent),
                    AverageBonusBalance = g.Any()
                        ? g.Average(x => x.BonusBalance)
                        : 0
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();
        }

        public List<TopCustomerItem> GetTopCustomers(int count = 10)
        {
            using var db = DbContextFactory.Create();

            var accounts = db.CustomerLoyaltyAccounts
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Level)
                .OrderByDescending(x => x.TotalSpent)
                .Take(count)
                .ToList();

            var customerIds = accounts
                .Select(x => x.CustomerId)
                .ToList();

            var transactionStats = db.Transactions
                .AsNoTracking()
                .Where(x => x.CustomerId != null &&
                            customerIds.Contains(x.CustomerId.Value))
                .GroupBy(x => x.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,

                    PurchaseCount = g.Count(x =>
                        x.TransactionType == TransactionTypeEnum.Purchase),

                    ReturnCount = g.Count(x =>
                        x.TransactionType == TransactionTypeEnum.Return),

                    LastPurchaseDate = g
                        .Where(x => x.TransactionType == TransactionTypeEnum.Purchase)
                        .OrderByDescending(x => x.TransactionDatetime)
                        .Select(x => (DateTime?)x.TransactionDatetime)
                        .FirstOrDefault()
                })
                .ToList();

            return accounts
                .Select(x =>
                {
                    var stats = transactionStats
                        .FirstOrDefault(s => s.CustomerId == x.CustomerId);

                    return new TopCustomerItem
                    {
                        CustomerId = x.CustomerId,

                        CustomerFullName = x.Customer == null
                            ? "Клиент удален"
                            : BuildFullName(
                                x.Customer.LastName,
                                x.Customer.FirstName,
                                x.Customer.MiddleName),

                        LevelName = x.Level?.LevelName ?? "Не указан",

                        TotalSpent = x.TotalSpent,

                        BonusBalance = x.BonusBalance,

                        PurchaseCount = stats?.PurchaseCount ?? 0,

                        ReturnCount = stats?.ReturnCount ?? 0,

                        LastPurchaseDate = stats?.LastPurchaseDate
                    };
                })
                .ToList();
        }

        public List<InactiveCustomerItem> GetInactiveCustomers(int daysWithoutPurchases = 30)
        {
            using var db = DbContextFactory.Create();

            var currentDate = DateTime.Today;

            var accounts = db.CustomerLoyaltyAccounts
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Level)
                .Where(x => x.Customer != null &&
                            x.Customer.Status == StatusEnum.Active)
                .ToList();

            var customerIds = accounts
                .Select(x => x.CustomerId)
                .ToList();

            var lastPurchaseDates = db.Transactions
                .AsNoTracking()
                .Where(x =>
                    x.CustomerId != null &&
                    customerIds.Contains(x.CustomerId.Value) &&
                    x.TransactionType == TransactionTypeEnum.Purchase)
                .GroupBy(x => x.CustomerId!.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    LastPurchaseDate = g.Max(x => x.TransactionDatetime)
                })
                .ToList();

            return accounts
                .Select(x =>
                {
                    var lastPurchaseDate = lastPurchaseDates
                        .FirstOrDefault(p => p.CustomerId == x.CustomerId)
                        ?.LastPurchaseDate;

                    var daysSinceLastPurchase = lastPurchaseDate == null
                        ? int.MaxValue
                        : (currentDate - lastPurchaseDate.Value.Date).Days;

                    return new InactiveCustomerItem
                    {
                        CustomerId = x.CustomerId,

                        CustomerFullName = x.Customer == null
                            ? "Клиент удален"
                            : BuildFullName(
                                x.Customer.LastName,
                                x.Customer.FirstName,
                                x.Customer.MiddleName),

                        Phone = x.Customer?.Phone ?? string.Empty,

                        LevelName = x.Level?.LevelName ?? "Не указан",

                        BonusBalance = x.BonusBalance,

                        TotalSpent = x.TotalSpent,

                        LastPurchaseDate = lastPurchaseDate,

                        DaysSinceLastPurchase = daysSinceLastPurchase
                    };
                })
                .Where(x => x.DaysSinceLastPurchase >= daysWithoutPurchases)
                .OrderByDescending(x => x.DaysSinceLastPurchase)
                .ToList();
        }

        public List<PromotionAnalyticsItem> GetPromotionAnalytics(DateTime? startDate = null, DateTime? endDate = null)
        {
            using var db = DbContextFactory.Create();

            var transactions = db.Transactions
                .AsNoTracking()
                .Include(x => x.Promotion)
                .Where(x =>
                    x.TransactionType == TransactionTypeEnum.Purchase &&
                    x.PromotionId != null);

            if (startDate != null)
            {
                var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Unspecified);

                transactions = transactions.Where(x => x.TransactionDatetime >= start);
            }

            if (endDate != null)
            {
                var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Unspecified);

                transactions = transactions.Where(x => x.TransactionDatetime < end);
            }

            return transactions
                .AsEnumerable()
                .GroupBy(x => new
                {
                    x.PromotionId,
                    x.Promotion!.PromotionName,
                    x.Promotion.PromotionType
                })
                .Select(g => new PromotionAnalyticsItem
                {
                    PromotionId = g.Key.PromotionId ?? 0,
                    PromotionName = g.Key.PromotionName,
                    PromotionType = EnumDisplayHelper.GetPgName(g.Key.PromotionType),
                    UsageCount = g.Count(),
                    GrossPurchaseAmount = g.Sum(x => x.TransactionAmount),
                    NetRevenue = g.Sum(x => x.PaidAmount),
                    BonusAccrued = g.Sum(x => x.BonusAccrued),
                    BonusUsed = g.Sum(x => x.BonusUsed)
                })
                .OrderByDescending(x => x.UsageCount)
                .ToList();
        }

        public List<OfferAnalyticsItem> GetOfferAnalytics()
        {
            using var db = DbContextFactory.Create();

            return db.CustomerOffers
                .AsNoTracking()
                .Include(x => x.Promotion)
                .AsEnumerable()
                .GroupBy(x => new
                {
                    x.PromotionId,
                    PromotionName = x.Promotion?.PromotionName ?? "Акция удалена",
                    PromotionType = x.Promotion == null
                        ? string.Empty
                        : EnumDisplayHelper.GetPgName(x.Promotion.PromotionType)
                })
                .Select(g => new OfferAnalyticsItem
                {
                    PromotionId = g.Key.PromotionId,
                    PromotionName = g.Key.PromotionName,
                    PromotionType = g.Key.PromotionType,

                    AssignedCount = g.Count(),

                    UsedCount = g.Count(x =>
                        x.OfferStatus == OfferStatusEnum.Used),

                    ExpiredCount = g.Count(x =>
                        x.OfferStatus == OfferStatusEnum.Expired),

                    CancelledCount = g.Count(x =>
                        x.OfferStatus == OfferStatusEnum.Cancelled)
                })
                .OrderByDescending(x => x.AssignedCount)
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
