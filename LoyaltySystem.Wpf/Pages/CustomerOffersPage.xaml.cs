using LoyaltySystem.Core.DTOs;
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
    public partial class CustomerOffersPage : Page
    {
        private readonly CustomerOfferService _customerOfferService = new();
        private ObservableCollection<CustomerOfferListItem> _customerOffers = new();
        private ICollectionView? _customerOffersView;

        public CustomerOffersPage()
        {
            InitializeComponent();

            ApplyAccessPolicy();

            DataGridZoomHelper.ApplyDefault(CustomerOffersDataGrid, TableZoomTextBlock);

            StatusFilterComboBox.SelectedIndex = 0;

            LoadOffers();
        }

        private void ApplyAccessPolicy()
        {
            var canManage = AccessPolicy.CanManageOffers;

            AddOfferButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            UseOfferButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            ExpireOfferButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            CancelOfferButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadOffers()
        {
            try
            {
                _customerOffers = new ObservableCollection<CustomerOfferListItem>(
                    _customerOfferService.GetListItems());

                _customerOffersView = CollectionViewSource.GetDefaultView(_customerOffers);
                _customerOffersView.Filter = FilterCustomerOffers;

                CustomerOffersDataGrid.ItemsSource = _customerOffersView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить персональные предложения.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AccessPolicy.EnsureCanManageOffers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new CustomerOfferWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadOffers();
            }
        }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedOfferStatus(
                OfferStatusEnum.Used,
                "Отметить выбранное предложение как использованное?");
        }

        private void ExpireButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedOfferStatus(
                OfferStatusEnum.Expired,
                "Отметить выбранное предложение как истекшее?");
        }

        private void CancelOfferButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeSelectedOfferStatus(
                OfferStatusEnum.Cancelled,
                "Отменить выбранное персональное предложение?");
        }

        private void ChangeSelectedOfferStatus(OfferStatusEnum status, string confirmationText)
        {
            try
            {
                AccessPolicy.EnsureCanManageOffers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CustomerOffersDataGrid.SelectedItem is not CustomerOfferListItem selectedOffer)
            {
                MessageBox.Show(
                    "Выберите персональное предложение.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (selectedOffer.OfferStatus != "Назначено")
            {
                MessageBox.Show(
                    "Изменять статус вручную можно только у предложений со статусом \"Назначено\".",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                confirmationText,
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _customerOfferService.UpdateStatus(selectedOffer.OfferId, status);

                LoadOffers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось изменить статус персонального предложения.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool FilterCustomerOffers(object item)
        {
            if (item is not CustomerOfferListItem offer)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(offer.CustomerFullName, searchText) ||
                    ContainsIgnoreCase(offer.PromotionName, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedStatus = GetSelectedStatusFilter();

            if (!string.IsNullOrWhiteSpace(selectedStatus) &&
                !string.Equals(offer.OfferStatus, selectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var assignedDate = offer.AssignedAt.Date;

            if (StartDatePicker.SelectedDate != null &&
                assignedDate < StartDatePicker.SelectedDate.Value.Date)
            {
                return false;
            }

            if (EndDatePicker.SelectedDate != null &&
                assignedDate > EndDatePicker.SelectedDate.Value.Date)
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

        private void RefreshFilter()
        {
            _customerOffersView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            StatusFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            RefreshFilter();
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(CustomerOffersDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(CustomerOffersDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(CustomerOffersDataGrid, TableZoomTextBlock);
        }
    }
}
