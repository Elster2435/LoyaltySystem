namespace LoyaltySystem.Core.DTOs
{
    public class CustomerListItem
    {
        public int CustomerId { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string LevelName { get; set; } = string.Empty;

        public decimal BonusBalance { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
