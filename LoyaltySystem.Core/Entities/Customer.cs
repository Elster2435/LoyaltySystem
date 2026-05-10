using LoyaltySystem.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoyaltySystem.Core.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public GenderEnum Gender { get; set; }

        public DateTime RegistrationDate { get; set; }

        public StatusEnum Status { get; set; }

        public DateTime UpdatedAt { get; set; }

        public CustomerLoyaltyAccount? LoyaltyAccount { get; set; }

        public List<CustomerTransaction> Transactions { get; set; } = new();

        public List<CustomerOffer> Offers { get; set; } = new();

        public List<CustomerActivity> Activities { get; set; } = new();
    }
}
