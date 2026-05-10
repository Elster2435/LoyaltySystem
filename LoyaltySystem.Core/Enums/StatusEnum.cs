using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum StatusEnum
    {
        [PgName("Активный")]
        Active,

        [PgName("Неактивный")]
        Inactive,

        [PgName("Заблокирован")]
        Blocked
    }
}
