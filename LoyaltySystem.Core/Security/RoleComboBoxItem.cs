namespace LoyaltySystem.Core.Security
{
    public class RoleComboBoxItem
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string DisplayText => RoleName;
    }
}
