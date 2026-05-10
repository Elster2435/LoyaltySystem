using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class LoyaltyAccountsPage : Page
    {
        private readonly LoyaltyAccountService _loyaltyAccountService = new();

        public LoyaltyAccountsPage()
        {
            InitializeComponent();

            LoadLoyaltyAccounts();
        }

        private void LoadLoyaltyAccounts()
        {
            try
            {
                LoyaltyAccountsDataGrid.ItemsSource = _loyaltyAccountService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить бонусные счета.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
