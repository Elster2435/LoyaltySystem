namespace LoyaltySystem.Core.Security
{
    public static class CurrentUserContext
    {
        public static AuthUserDto? CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static void SetUser(AuthUserDto user)
        {
            CurrentUser = user;
        }

        public static void Clear()
        {
            CurrentUser = null;
        }

        public static AuthUserDto GetRequiredUser()
        {
            if (CurrentUser == null)
                throw new Exception("Пользователь не авторизован.");

            return CurrentUser;
        }
    }
}
