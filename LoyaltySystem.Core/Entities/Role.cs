using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        public RoleNameEnum RoleName { get; set; }

        public List<User> Users { get; set; } = new();
    }
}
