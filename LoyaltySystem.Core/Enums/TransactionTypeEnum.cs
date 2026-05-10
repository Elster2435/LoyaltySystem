using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum TransactionTypeEnum
    {
        [PgName("Покупка")]
        Purchase,

        [PgName("Возврат")]
        Return
    }
}
