using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Security;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Helpers;
using LoyaltySystem.Wpf.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class CustomersPage : Page
    {
        private readonly CustomerService _customerService = new();
        private readonly LoyaltyLevelService _loyaltyLevelService = new();
        private ObservableCollection<CustomerListItem> _customers = new();
        private ICollectionView? _customersView;

        public CustomersPage()
        {
            InitializeComponent();

            ApplyAccessPolicy();

            DataGridZoomHelper.ApplyDefault(CustomersDataGrid, TableZoomTextBlock);

            LoadLevelFilter();

            StatusFilterComboBox.SelectedIndex = 0;
            LevelFilterComboBox.SelectedIndex = 0;

            LoadCustomers();
        }

        private void ApplyAccessPolicy()
        {
            var canManage = AccessPolicy.CanManageCustomers;
            var canBlock = AccessPolicy.CanBlockCustomers;

            AddCustomerButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            EditCustomerButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            ActivateCustomerButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            DeleteCustomerButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            BlockCustomerButton.Visibility = canBlock ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadCustomers()
        {
            try
            {
                _customers = new ObservableCollection<CustomerListItem>(
                    _customerService.GetListItems());

                _customersView = CollectionViewSource.GetDefaultView(_customers);
                _customersView.Filter = FilterCustomers;

                CustomersDataGrid.ItemsSource = _customersView;
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
            try
            {
                AccessPolicy.EnsureCanManageCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            try
            {
                AccessPolicy.EnsureCanManageCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeCustomerStatus(StatusEnum.Active, "активировать");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeCustomerStatus(StatusEnum.Inactive, "деактивировать");
        }

        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeCustomerStatus(StatusEnum.Blocked, "заблокировать");
        }

        private void ChangeCustomerStatus(StatusEnum status, string actionText)
        {
            try
            {
                if (status == StatusEnum.Blocked)
                    AccessPolicy.EnsureCanBlockCustomers();
                else
                    AccessPolicy.EnsureCanManageCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (CustomersDataGrid.SelectedItem is not CustomerListItem selectedCustomer)
            {
                MessageBox.Show(
                    "Выберите клиента.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedCustomer.Status == GetStatusDisplayName(status))
            {
                MessageBox.Show(
                    $"Клиент уже имеет статус \"{GetStatusDisplayName(status)}\".",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            var result = MessageBox.Show(
                $"Вы действительно хотите {actionText} клиента \"{selectedCustomer.FullName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _customerService.SetStatus(selectedCustomer.CustomerId, status);

                LoadCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось изменить статус клиента.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string GetStatusDisplayName(StatusEnum status)
        {
            return status switch
            {
                StatusEnum.Active => "Активный",
                StatusEnum.Inactive => "Неактивный",
                StatusEnum.Blocked => "Заблокирован",
                _ => status.ToString()
            };
        }

        private void LoadLevelFilter()
        {
            var levels = _loyaltyLevelService.GetComboBoxItems();

            levels.Insert(0, new LoyaltyLevel
            {
                LevelId = 0,
                LevelName = "Все уровни"
            });

            LevelFilterComboBox.ItemsSource = levels;
        }

        private bool FilterCustomers(object item)
        {
            if (item is not CustomerListItem customer)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(customer.FullName, searchText) ||
                    ContainsIgnoreCase(customer.Phone, searchText) ||
                    ContainsIgnoreCase(customer.Email, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedStatus = GetSelectedStatusFilter();

            if (!string.IsNullOrWhiteSpace(selectedStatus) &&
                !string.Equals(customer.Status, selectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedLevel = GetSelectedLevelFilter();

            if (!string.IsNullOrWhiteSpace(selectedLevel) &&
                !string.Equals(customer.LevelName, selectedLevel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool ContainsIgnoreCase(string? source, string searchText)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            return source.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private string? GetSelectedStatusFilter()
        {
            if (StatusFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var status = selectedItem.Content?.ToString();

            return status == "Все статусы"
                ? null
                : status;
        }

        private string? GetSelectedLevelFilter()
        {
            if (LevelFilterComboBox.SelectedValue is not string levelName)
                return null;

            return levelName == "Все уровни"
                ? null
                : levelName;
        }

        private void RefreshFilter()
        {
            _customersView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void LevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            StatusFilterComboBox.SelectedIndex = 0;
            LevelFilterComboBox.SelectedIndex = 0;

            RefreshFilter();
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(CustomersDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(CustomersDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(CustomersDataGrid, TableZoomTextBlock);
        }
    }
}
