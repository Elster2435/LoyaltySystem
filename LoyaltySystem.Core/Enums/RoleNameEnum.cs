using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum RoleNameEnum
    {
        [PgName("Администратор")]
        Administrator,

        [PgName("Менеджер")]
        Manager,

        [PgName("Аналитик")]
        Analyst
    }
}
