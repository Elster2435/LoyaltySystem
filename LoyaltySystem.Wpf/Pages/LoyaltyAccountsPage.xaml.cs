using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class LoyaltyAccountsPage : Page
    {
        private readonly LoyaltyAccountService _loyaltyAccountService = new();
        private ObservableCollection<LoyaltyAccountListItem> _loyaltyAccounts = new();
        private ICollectionView? _loyaltyAccountsView;

        public LoyaltyAccountsPage()
        {
            InitializeComponent();

            DataGridZoomHelper.ApplyDefault(LoyaltyAccountsDataGrid, TableZoomTextBlock);

            LoadLevelFilter();

            StatusFilterComboBox.SelectedIndex = 0;
            LevelFilterComboBox.SelectedIndex = 0;

            LoadLoyaltyAccounts();
        }

        private void LoadLoyaltyAccounts()
        {
            try
            {
                _loyaltyAccounts = new ObservableCollection<LoyaltyAccountListItem>(
                    _loyaltyAccountService.GetListItems());

                _loyaltyAccountsView = CollectionViewSource.GetDefaultView(_loyaltyAccounts);
                _loyaltyAccountsView.Filter = FilterLoyaltyAccounts;

                LoyaltyAccountsDataGrid.ItemsSource = _loyaltyAccountsView;
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

        private void LoadLevelFilter()
        {
            var levels = _loyaltyAccountService.GetListItems()
                .Select(x => x.LevelName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            levels.Insert(0, "Все уровни");

            LevelFilterComboBox.ItemsSource = levels;
        }

        private bool FilterLoyaltyAccounts(object item)
        {
            if (item is not LoyaltyAccountListItem account)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(account.CustomerFullName, searchText) ||
                    ContainsIgnoreCase(account.Phone, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedLevel = GetSelectedLevelFilter();

            if (!string.IsNullOrWhiteSpace(selectedLevel) &&
                !string.Equals(account.LevelName, selectedLevel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedStatus = GetSelectedStatusFilter();

            if (!string.IsNullOrWhiteSpace(selectedStatus) &&
                !string.Equals(account.AccountStatus, selectedStatus, StringComparison.OrdinalIgnoreCase))
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

        private string? GetSelectedLevelFilter()
        {
            if (LevelFilterComboBox.SelectedItem is not string levelName)
                return null;

            return levelName == "Все уровни"
                ? null
                : levelName;
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

        private void RefreshFilter()
        {
            _loyaltyAccountsView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void LevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            LevelFilterComboBox.SelectedIndex = 0;
            StatusFilterComboBox.SelectedIndex = 0;

            RefreshFilter();
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(LoyaltyAccountsDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(LoyaltyAccountsDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(LoyaltyAccountsDataGrid, TableZoomTextBlock);
        }
    }
}
