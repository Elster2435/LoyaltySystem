using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class CustomerActivitiesPage : Page
    {
        private readonly CustomerActivityService _customerActivityService = new();

        public CustomerActivitiesPage()
        {
            InitializeComponent();

            LoadCustomerActivities();
        }

        private void LoadCustomerActivities()
        {
            try
            {
                CustomerActivitiesDataGrid.ItemsSource = _customerActivityService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить активность клиентов.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
