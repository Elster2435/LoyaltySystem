using LoyaltySystem.Core.Enums;

namespace LoyaltySystem.Core.Security
{
    public static class AccessPolicy
    {
        public static bool IsAdministrator =>
            CurrentUserContext.CurrentUser?.RoleName == RoleNameEnum.Administrator;

        public static bool IsManager =>
            CurrentUserContext.CurrentUser?.RoleName == RoleNameEnum.Manager;

        public static bool IsAnalyst =>
            CurrentUserContext.CurrentUser?.RoleName == RoleNameEnum.Analyst;

        public static bool CanManageCustomers =>
            IsAdministrator || IsManager;

        public static bool CanBlockCustomers =>
            IsAdministrator;

        public static bool CanManageTransactions =>
            IsAdministrator || IsManager;

        public static bool CanManagePromotions =>
            IsAdministrator || IsManager;

        public static bool CanManageOffers =>
            IsAdministrator || IsManager;

        public static bool CanManageUsers =>
            IsAdministrator;

        public static bool CanViewMonitoring =>
            IsAdministrator || IsManager || IsAnalyst;

        public static bool CanViewMainData =>
            IsAdministrator || IsManager || IsAnalyst;

        public static bool CanViewAuditData =>
            IsAdministrator || IsManager || IsAnalyst;

        public static void EnsureCanManageCustomers()
        {
            if (!CanManageCustomers)
                throw new Exception("Недостаточно прав для управления клиентами.");
        }

        public static void EnsureCanBlockCustomers()
        {
            if (!CanBlockCustomers)
                throw new Exception("Недостаточно прав для блокировки клиентов.");
        }

        public static void EnsureCanManageTransactions()
        {
            if (!CanManageTransactions)
                throw new Exception("Недостаточно прав для управления транзакциями.");
        }

        public static void EnsureCanManagePromotions()
        {
            if (!CanManagePromotions)
                throw new Exception("Недостаточно прав для управления акциями.");
        }

        public static void EnsureCanManageOffers()
        {
            if (!CanManageOffers)
                throw new Exception("Недостаточно прав для управления персональными предложениями.");
        }

        public static void EnsureCanManageUsers()
        {
            if (!CanManageUsers)
                throw new Exception("Недостаточно прав для управления пользователями.");
        }
    }
}
