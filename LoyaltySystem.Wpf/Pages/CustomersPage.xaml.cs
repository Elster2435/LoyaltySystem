using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class CustomersPage : Page
    {
        private readonly CustomerService _customerService = new();

        public CustomersPage()
        {
            InitializeComponent();

            LoadCustomers();
        }

        private void LoadCustomers()
        {
            try
            {
                CustomersDataGrid.ItemsSource = _customerService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить клиентов.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CustomerWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadCustomers();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is not CustomerListItem selectedCustomer)
            {
                MessageBox.Show(
                    "Выберите клиента для изменения.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new CustomerWindow(selectedCustomer.CustomerId)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadCustomers();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is not CustomerListItem selectedCustomer)
            {
                MessageBox.Show(
                    "Выберите клиента для деактивации.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                $"Деактивировать клиента {selectedCustomer.FullName}?\n\n" +
                "Клиент не будет удален физически, его статус изменится на \"Неактивный\".",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _customerService.Delete(selectedCustomer.CustomerId);

                LoadCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось деактивировать клиента.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
