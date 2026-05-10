using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum BonusTransactionTypeEnum
    {
        [PgName("Начисление")]
        Accrual,

        [PgName("Списание")]
        WriteOff,

        [PgName("Корректировка")]
        Correction,

        [PgName("Сгорание")]
        Expiration
    }
}
