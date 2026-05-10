namespace LoyaltySystem.Core.Entities
{
    public class User
    {
        public int UserId { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string Login { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Role? Role { get; set; }
    }
}
