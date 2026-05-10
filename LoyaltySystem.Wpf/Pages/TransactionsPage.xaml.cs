using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class TransactionsPage : Page
    {
        private readonly TransactionService _transactionService = new();

        public TransactionsPage()
        {
            InitializeComponent();

            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                TransactionsDataGrid.ItemsSource = _transactionService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить транзакции.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new PurchaseWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadTransactions();
            }
        }

        private void ReturnTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReturnTransactionWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadTransactions();
            }
        }
    }
}
