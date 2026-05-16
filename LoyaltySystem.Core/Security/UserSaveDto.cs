namespace LoyaltySystem.Core.Security
{
    public class UserSaveDto
    {
        public int? UserId { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string Login { get; set; } = string.Empty;

        public string? CurrentPassword { get; set; }

        public string? Password { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CurrentUserId { get; set; }
    }
}
