using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Security
{
    public class AuthUserDto
    {
        public int UserId { get; set; }

        public string Login { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public RoleNameEnum RoleName { get; set; }

        public string RoleDisplayName { get; set; } = string.Empty;

        public string FullName
        {
            get
            {
                var parts = new List<string>
            {
                LastName,
                FirstName
            };

                if (!string.IsNullOrWhiteSpace(MiddleName))
                    parts.Add(MiddleName);

                return string.Join(" ", parts);
            }
        }
    }
}
