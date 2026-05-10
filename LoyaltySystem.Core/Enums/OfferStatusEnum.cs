using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum OfferStatusEnum
    {
        [PgName("Назначено")]
        Assigned,

        [PgName("Использовано")]
        Used,

        [PgName("Истекло")]
        Expired,

        [PgName("Отменено")]
        Cancelled
    }
}
