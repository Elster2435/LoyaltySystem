using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class CustomerOfferWindow : Window
    {
        private readonly CustomerService _customerService = new();
        private readonly PromotionService _promotionService = new();
        private readonly CustomerOfferService _customerOfferService = new();
        private ObservableCollection<CustomerComboBoxItem> _customers = new();
        private ICollectionView? _customersView;
        private ObservableCollection<PromotionComboBoxItem> _promotions = new();
        private ICollectionView? _promotionsView;

        public CustomerOfferWindow()
        {
            InitializeComponent();

            LoadCustomers();
            LoadPromotions();

            ValidUntilDatePicker.SelectedDate = DateTime.Today.AddDays(14);
        }

        private void LoadCustomers()
        {
            _customers = new ObservableCollection<CustomerComboBoxItem>(
                _customerService.GetComboBoxItems());

            _customersView = CollectionViewSource.GetDefaultView(_customers);
            _customersView.Filter = FilterCustomers;

            CustomerComboBox.ItemsSource = _customersView;
        }

        private bool FilterCustomers(object item)
        {
            if (item is not CustomerComboBoxItem customer)
                return false;

            var searchText = CustomerComboBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return customer.DisplayText.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase);
        }

        private void CustomerComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _customersView?.Refresh();

            if (!CustomerComboBox.IsDropDownOpen)
                CustomerComboBox.IsDropDownOpen = true;
        }

        private void CustomerComboBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            CustomerComboBox.IsDropDownOpen = true;
        }

        private void LoadPromotions()
        {
            _promotions = new ObservableCollection<PromotionComboBoxItem>(
                _promotionService.GetActivePersonalOfferPromotionItems());

            _promotionsView = CollectionViewSource.GetDefaultView(_promotions);
            _promotionsView.Filter = FilterPromotions;

            PromotionComboBox.ItemsSource = _promotionsView;
        }

        private void PromotionComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _promotionsView?.Refresh();

            if (!PromotionComboBox.IsDropDownOpen)
                PromotionComboBox.IsDropDownOpen = true;
        }

        private void PromotionComboBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            PromotionComboBox.IsDropDownOpen = true;
        }

        private bool FilterPromotions(object item)
        {
            if (item is not PromotionComboBoxItem promotion)
                return false;

            var searchText = PromotionComboBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return promotion.DisplayText.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var validUntilDate = ValidUntilDatePicker.SelectedDate!.Value.Date
                    .AddHours(23)
                    .AddMinutes(59)
                    .AddSeconds(59);

                var offer = new CustomerOffer
                {
                    CustomerId = (int)CustomerComboBox.SelectedValue,
                    PromotionId = (int)PromotionComboBox.SelectedValue,
                    AssignedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                    ValidUntil = DateTime.SpecifyKind(validUntilDate, DateTimeKind.Unspecified),
                    OfferStatus = OfferStatusEnum.Assigned
                };

                _customerOfferService.Add(offer);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось назначить персональное предложение.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ValidateFields()
        {
            if (CustomerComboBox.SelectedValue is not int)
            {
                MessageBox.Show(
                    "Выберите клиента.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (PromotionComboBox.SelectedValue is not int)
            {
                MessageBox.Show(
                    "Выберите акцию.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (ValidUntilDatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    "Выберите дату окончания действия предложения.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (ValidUntilDatePicker.SelectedDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show(
                    "Дата окончания действия предложения не может быть раньше сегодняшней даты.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
