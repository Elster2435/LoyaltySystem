using LoyaltySystem.Core.Security;
using LoyaltySystem.Wpf.Windows;
using System.Windows;

namespace LoyaltySystem.Wpf
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true || loginWindow.CurrentUser == null)
            {
                Shutdown();
                return;
            }

            CurrentUserContext.SetUser(loginWindow.CurrentUser);

            var mainWindow = new MainWindow(loginWindow.CurrentUser);
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
    }
}
