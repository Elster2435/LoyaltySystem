using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class PromotionService
    {
        public List<PromotionListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.Promotions
                .AsNoTracking()
                .Include(x => x.RequiredLevel)
                .OrderByDescending(x => x.StartDate)
                .AsEnumerable()
                .Select(x => new PromotionListItem
                {
                    PromotionId = x.PromotionId,
                    PromotionName = x.PromotionName,
                    PromotionType = EnumDisplayHelper.GetPgName(x.PromotionType),
                    Description = x.Description,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    BonusMultiplier = x.BonusMultiplier,
                    ExtraBonus = x.ExtraBonus,
                    RequiredLevelName = x.RequiredLevel?.LevelName ?? "Без ограничения",
                    IsActive = x.IsActive
                })
                .ToList();
        }

        public Promotion? GetById(int promotionId)
        {
            using var db = DbContextFactory.Create();

            return db.Promotions
                .FirstOrDefault(x => x.PromotionId == promotionId);
        }

        public void Add(Promotion promotion)
        {
            using var db = DbContextFactory.Create();

            db.Promotions.Add(promotion);
            db.SaveChanges();
        }

        public void Update(Promotion promotion)
        {
            using var db = DbContextFactory.Create();

            var existingPromotion = db.Promotions
                .FirstOrDefault(x => x.PromotionId == promotion.PromotionId);

            if (existingPromotion == null)
                throw new InvalidOperationException("Акция не найдена.");

            existingPromotion.PromotionType = promotion.PromotionType;
            existingPromotion.PromotionName = promotion.PromotionName;
            existingPromotion.Description = promotion.Description;
            existingPromotion.StartDate = promotion.StartDate;
            existingPromotion.EndDate = promotion.EndDate;
            existingPromotion.BonusMultiplier = promotion.BonusMultiplier;
            existingPromotion.ExtraBonus = promotion.ExtraBonus;
            existingPromotion.RequiredLevelId = promotion.RequiredLevelId;
            existingPromotion.IsActive = promotion.IsActive;

            db.SaveChanges();
        }

        public void Enable(int promotionId)
        {
            SetActive(promotionId, true);
        }

        public void Disable(int promotionId)
        {
            SetActive(promotionId, false);
        }

        public void SetActive(int promotionId, bool isActive)
        {
            using var db = DbContextFactory.Create();

            var promotion = db.Promotions
                .FirstOrDefault(x => x.PromotionId == promotionId);

            if (promotion == null)
                throw new Exception("Акция не найдена.");

            if (promotion.IsActive == isActive)
            {
                var statusText = isActive ? "активна" : "отключена";
                throw new Exception($"Акция уже {statusText}.");
            }

            promotion.IsActive = isActive;

            db.SaveChanges();
        }

        public List<PromotionComboBoxItem> GetActiveComboBoxItems()
        {
            using var db = DbContextFactory.Create();

            var today = DateTime.Today;

            return db.Promotions
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.StartDate <= today &&
                    x.EndDate >= today)
                .OrderBy(x => x.PromotionName)
                .Select(x => new PromotionComboBoxItem
                {
                    PromotionId = x.PromotionId,
                    PromotionName = x.PromotionName
                })
                .ToList();
        }

        public List<PromotionComboBoxItem> GetActivePersonalOfferPromotionItems()
        {
            using var db = DbContextFactory.Create();

            var today = DateTime.Today;

            return db.Promotions
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.StartDate <= today &&
                    x.EndDate >= today &&
                    (
                        x.PromotionType == PromotionTypeEnum.Personal ||
                        x.PromotionType == PromotionTypeEnum.Birthday ||
                        x.PromotionType == PromotionTypeEnum.CustomerReturn ||
                        x.PromotionType == PromotionTypeEnum.PurchaseReturn
                    ))
                .OrderBy(x => x.PromotionName)
                .Select(x => new PromotionComboBoxItem
                {
                    PromotionId = x.PromotionId,
                    PromotionName = x.PromotionName
                })
                .ToList();
        }

        public List<TransactionBonusConditionItem> GetActiveGeneralPromotionsForPurchase(int customerId)
        {
            using var db = DbContextFactory.Create();

            var today = DateTime.Today;

            var customerLevelMinTotalSpent = db.CustomerLoyaltyAccounts
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .Select(x => x.Level!.MinTotalSpent)
                .FirstOrDefault();

            return db.Promotions
                .AsNoTracking()
                .Include(x => x.RequiredLevel)
                .Where(x =>
                    x.IsActive &&
                    x.PromotionType == PromotionTypeEnum.General &&
                    x.StartDate <= today &&
                    x.EndDate >= today &&
                    (
                        x.RequiredLevelId == null ||
                        x.RequiredLevel!.MinTotalSpent <= customerLevelMinTotalSpent
                    ))
                .OrderBy(x => x.PromotionName)
                .Select(x => new TransactionBonusConditionItem
                {
                    Id = x.PromotionId,
                    Name = x.PromotionName
                })
                .ToList();
        }
    }
}
