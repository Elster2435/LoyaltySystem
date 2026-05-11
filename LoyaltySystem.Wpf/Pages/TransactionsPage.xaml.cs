using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class TransactionsPage : Page
    {
        private readonly TransactionService _transactionService = new();
        private ObservableCollection<TransactionListItem> _transactions = new();
        private ICollectionView? _transactionsView;

        public TransactionsPage()
        {
            InitializeComponent();

            TransactionTypeFilterComboBox.SelectedIndex = 0;
            ChannelFilterComboBox.SelectedIndex = 0;

            LoadTransactions();
        }

        private void LoadTransactions()
        {
            try
            {
                _transactions = new ObservableCollection<TransactionListItem>(
                    _transactionService.GetListItems());

                _transactionsView = CollectionViewSource.GetDefaultView(_transactions);
                _transactionsView.Filter = FilterTransactions;

                TransactionsDataGrid.ItemsSource = _transactionsView;
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

        private bool FilterTransactions(object item)
        {
            if (item is not TransactionListItem transaction)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (!ContainsIgnoreCase(transaction.CustomerFullName, searchText))
                    return false;
            }

            var selectedType = GetSelectedTransactionTypeFilter();

            if (!string.IsNullOrWhiteSpace(selectedType) &&
                !string.Equals(transaction.TransactionType, selectedType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedChannel = GetSelectedChannelFilter();

            if (!string.IsNullOrWhiteSpace(selectedChannel) &&
                !string.Equals(transaction.TransactionChannel, selectedChannel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var transactionDate = transaction.TransactionDatetime.Date;

            if (StartDatePicker.SelectedDate != null &&
                transactionDate < StartDatePicker.SelectedDate.Value.Date)
            {
                return false;
            }

            if (EndDatePicker.SelectedDate != null &&
                transactionDate > EndDatePicker.SelectedDate.Value.Date)
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

        private string? GetSelectedTransactionTypeFilter()
        {
            if (TransactionTypeFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var type = selectedItem.Content?.ToString();

            return type == "Все типы"
                ? null
                : type;
        }

        private string? GetSelectedChannelFilter()
        {
            if (ChannelFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var channel = selectedItem.Content?.ToString();

            return channel == "Все каналы"
                ? null
                : channel;
        }

        private void RefreshFilter()
        {
            _transactionsView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void TransactionTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ChannelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            TransactionTypeFilterComboBox.SelectedIndex = 0;
            ChannelFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            RefreshFilter();
        }
    }
}
