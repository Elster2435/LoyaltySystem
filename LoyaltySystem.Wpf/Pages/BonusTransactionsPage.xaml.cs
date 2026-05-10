using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class BonusTransactionsPage : Page
    {
        private readonly BonusTransactionService _bonusTransactionService = new();

        public BonusTransactionsPage()
        {
            InitializeComponent();

            LoadBonusTransactions();
        }

        private void LoadBonusTransactions()
        {
            try
            {
                BonusTransactionsDataGrid.ItemsSource = _bonusTransactionService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить бонусные операции.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
