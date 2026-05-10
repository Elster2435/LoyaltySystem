using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class CustomerOfferService
    {
        public List<CustomerOfferListItem> GetListItems()
        {
            ExpireOverdueOffers();

            using var db = DbContextFactory.Create();

            return db.CustomerOffers
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Promotion)
                .OrderByDescending(x => x.AssignedAt)
                .AsEnumerable()
                .Select(x => new CustomerOfferListItem
                {
                    OfferId = x.OfferId,
                    CustomerId = x.CustomerId,
                    PromotionId = x.PromotionId,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    PromotionName = x.Promotion?.PromotionName ?? "Акция удалена",
                    AssignedAt = x.AssignedAt,
                    ValidUntil = x.ValidUntil,
                    OfferStatus = EnumDisplayHelper.GetPgName(x.OfferStatus)
                })
                .ToList();
        }

        public CustomerOffer? GetById(int offerId)
        {
            using var db = DbContextFactory.Create();

            return db.CustomerOffers
                .Include(x => x.Customer)
                .Include(x => x.Promotion)
                .FirstOrDefault(x => x.OfferId == offerId);
        }

        public void Add(CustomerOffer offer)
        {
            using var db = DbContextFactory.Create();

            offer.AssignedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            offer.OfferStatus = OfferStatusEnum.Assigned;

            db.CustomerOffers.Add(offer);
            db.SaveChanges();

            var promotionName = db.Promotions
                .Where(x => x.PromotionId == offer.PromotionId)
                .Select(x => x.PromotionName)
                .FirstOrDefault() ?? "Без названия";

            db.CustomerActivities.Add(new CustomerActivity
            {
                CustomerId = offer.CustomerId,
                ActivityType = ActivityTypeEnum.OfferReceived,
                ActivityDatetime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Description = $"Клиенту назначено персональное предложение: {promotionName}"
            });

            db.SaveChanges();
        }

        public void UpdateStatus(int offerId, OfferStatusEnum status)
        {
            using var db = DbContextFactory.Create();

            var offer = db.CustomerOffers
                .Include(x => x.Promotion)
                .FirstOrDefault(x => x.OfferId == offerId);

            if (offer == null)
                throw new InvalidOperationException("Персональное предложение не найдено.");

            if (offer.OfferStatus == status)
                return;

            offer.OfferStatus = status;

            var activityType = status switch
            {
                OfferStatusEnum.Used => ActivityTypeEnum.OfferUsed,
                OfferStatusEnum.Expired => ActivityTypeEnum.OfferExpired,
                OfferStatusEnum.Cancelled => ActivityTypeEnum.OfferCancelled,
                _ => (ActivityTypeEnum?)null
            };

            if (activityType != null)
            {
                db.CustomerActivities.Add(new CustomerActivity
                {
                    CustomerId = offer.CustomerId,
                    ActivityType = activityType.Value,
                    ActivityDatetime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                    Description = $"Статус персонального предложения \"{offer.Promotion?.PromotionName}\" изменен на \"{EnumDisplayHelper.GetPgName(status)}\""
                });
            }

            db.SaveChanges();
        }

        public void Cancel(int offerId)
        {
            UpdateStatus(offerId, OfferStatusEnum.Cancelled);
        }

        public int ExpireOverdueOffers()
        {
            using var db = DbContextFactory.Create();

            return db.Database
                .SqlQueryRaw<int>("select fn_expire_overdue_customer_offers()")
                .AsEnumerable()
                .FirstOrDefault();
        }

        public List<TransactionBonusConditionItem> GetAvailableOffersForPurchase(int customerId)
        {
            using var db = DbContextFactory.Create();

            ExpireOverdueOffers();

            var today = DateTime.Today;

            return db.CustomerOffers
                .AsNoTracking()
                .Include(x => x.Promotion)
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.OfferStatus == OfferStatusEnum.Assigned &&
                    (x.ValidUntil == null || x.ValidUntil.Value.Date >= today) &&
                    x.Promotion != null &&
                    x.Promotion.IsActive &&
                    x.Promotion.StartDate <= today &&
                    x.Promotion.EndDate >= today)
                .OrderBy(x => x.Promotion!.PromotionName)
                .Select(x => new TransactionBonusConditionItem
                {
                    Id = x.OfferId,
                    Name = x.Promotion!.PromotionName
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
