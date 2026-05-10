using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum ActivityTypeEnum
    {
        [PgName("Регистрация")]
        Registration,

        [PgName("Покупка")]
        Purchase,

        [PgName("Возврат покупки")]
        PurchaseReturn,

        [PgName("Начисление бонусов")]
        BonusAccrual,

        [PgName("Списание бонусов")]
        BonusWriteOff,

        [PgName("Получено предложение")]
        OfferReceived,

        [PgName("Использовано предложение")]
        OfferUsed,

        [PgName("Истекло предложение")]
        OfferExpired,

        [PgName("Отменено предложение")]
        OfferCancelled,

        [PgName("Применена акция")]
        PromotionApplied,

        [PgName("Изменение профиля")]
        ProfileChanged
    }
}
