using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum TransactionChannelEnum
    {
        [PgName("Оффлайн")]
        Offline,

        [PgName("Онлайн")]
        Online
    }
}
