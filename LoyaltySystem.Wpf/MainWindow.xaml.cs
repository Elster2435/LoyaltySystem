using LoyaltySystem.Wpf.Pages;
using System.Windows;

namespace LoyaltySystem.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

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
    }
}