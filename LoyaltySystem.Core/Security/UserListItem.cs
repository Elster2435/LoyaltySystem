namespace LoyaltySystem.Core.Security
{
    public class UserListItem
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Login { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string ActivityStatus => IsActive ? "Активен" : "Отключен";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
