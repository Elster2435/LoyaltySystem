namespace LoyaltySystem.Core.Security
{
    public class UserSaveDto
    {
        public int? UserId { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string Login { get; set; } = string.Empty;

        public string? Password { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
