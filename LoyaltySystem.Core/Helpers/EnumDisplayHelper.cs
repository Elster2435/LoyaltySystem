using NpgsqlTypes;
using System.Reflection;

namespace LoyaltySystem.Core.Helpers
{
    public static class EnumDisplayHelper
    {
        public static string GetPgName<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            var member = typeof(TEnum)
                .GetMember(value.ToString())
                .FirstOrDefault();

            var attribute = member?
                .GetCustomAttribute<PgNameAttribute>();

            return attribute?.PgName ?? value.ToString();
        }
    }
}
