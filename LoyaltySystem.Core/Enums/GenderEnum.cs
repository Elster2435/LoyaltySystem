using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum GenderEnum
    {
        [PgName("Мужской")]
        Male,

        [PgName("Женский")]
        Female
    }
}
