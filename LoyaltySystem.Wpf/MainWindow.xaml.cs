using LoyaltySystem.Core.Security;
using LoyaltySystem.Wpf.Pages;
using LoyaltySystem.Wpf.Windows;
using System.Windows;

namespace LoyaltySystem.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly AuthUserDto _currentUser;

        public MainWindow(AuthUserDto currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            ApplyUserInfo();
            ApplyAccessPolicy();

            Title = $"Система лояльности — {_currentUser.FullName} ({_currentUser.RoleDisplayName})";

            MainFrame.Navigate(new MonitoringPage());
        }

        private void MonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new MonitoringPage());
        }

        private void CustomersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CustomersPage());
        }

        private void TransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TransactionsPage());
        }

        private void BonusTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new BonusTransactionsPage());
        }

        private void LoyaltyAccountsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LoyaltyAccountsPage());
        }

        private void CustomerActivitiesButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CustomerActivitiesPage());
        }

        private void PromotionsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PromotionsPage());
        }

        private void CustomerOffersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CustomerOffersPage());
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new UsersPage());
        }

        private void ApplyUserInfo()
        {
            CurrentUserTextBlock.Text = _currentUser.FullName;
            CurrentRoleTextBlock.Text = $"Роль: {_currentUser.RoleDisplayName}";
        }

        private void ApplyAccessPolicy()
        {
            UsersButton.Visibility = AccessPolicy.CanManageUsers
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из учетной записи?",
                "Выход",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            CurrentUserContext.Clear();

            var loginWindow = new LoginWindow();
            var loginResult = loginWindow.ShowDialog();

            if (loginResult != true || loginWindow.CurrentUser == null)
            {
                Application.Current.Shutdown();
                return;
            }

            CurrentUserContext.SetUser(loginWindow.CurrentUser);

            var mainWindow = new MainWindow(loginWindow.CurrentUser);
            Application.Current.MainWindow = mainWindow;

            mainWindow.Show();
            Close();
        }
    }
}