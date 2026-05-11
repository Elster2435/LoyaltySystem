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
    public partial class BonusTransactionsPage : Page
    {
        private readonly BonusTransactionService _bonusTransactionService = new();
        private ObservableCollection<BonusTransactionListItem> _bonusTransactions = new();
        private ICollectionView? _bonusTransactionsView;

        public BonusTransactionsPage()
        {
            InitializeComponent();

            DataGridZoomHelper.ApplyDefault(BonusTransactionsDataGrid, TableZoomTextBlock);

            BonusTypeFilterComboBox.SelectedIndex = 0;

            LoadBonusTransactions();
        }

        private void LoadBonusTransactions()
        {
            try
            {
                _bonusTransactions = new ObservableCollection<BonusTransactionListItem>(
                    _bonusTransactionService.GetListItems());

                _bonusTransactionsView = CollectionViewSource.GetDefaultView(_bonusTransactions);
                _bonusTransactionsView.Filter = FilterBonusTransactions;

                BonusTransactionsDataGrid.ItemsSource = _bonusTransactionsView;
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

        private bool FilterBonusTransactions(object item)
        {
            if (item is not BonusTransactionListItem bonusTransaction)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(bonusTransaction.CustomerFullName, searchText) ||
                    ContainsIgnoreCase(bonusTransaction.Description, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedType = GetSelectedBonusTypeFilter();

            if (!string.IsNullOrWhiteSpace(selectedType) &&
                !string.Equals(bonusTransaction.BonusTransactionType, selectedType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var operationDate = bonusTransaction.BonusTransactionDatetime.Date;

            if (StartDatePicker.SelectedDate != null &&
                operationDate < StartDatePicker.SelectedDate.Value.Date)
            {
                return false;
            }

            if (EndDatePicker.SelectedDate != null &&
                operationDate > EndDatePicker.SelectedDate.Value.Date)
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

        private string? GetSelectedBonusTypeFilter()
        {
            if (BonusTypeFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var type = selectedItem.Content?.ToString();

            return type == "Все типы"
                ? null
                : type;
        }

        private void RefreshFilter()
        {
            _bonusTransactionsView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void BonusTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            BonusTypeFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            RefreshFilter();
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(BonusTransactionsDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(BonusTransactionsDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(BonusTransactionsDataGrid, TableZoomTextBlock);
        }
    }
}
