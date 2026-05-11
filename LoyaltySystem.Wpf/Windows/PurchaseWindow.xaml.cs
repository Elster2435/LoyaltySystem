using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class PurchaseWindow : Window
    {
        private readonly CustomerService _customerService = new();
        private readonly PromotionService _promotionService = new();
        private readonly CustomerOfferService _customerOfferService = new();
        private readonly TransactionService _transactionService = new();
        private ObservableCollection<CustomerComboBoxItem> _customers = new();
        private ICollectionView? _customersView;
        private ObservableCollection<TransactionBonusConditionItem> _bonusConditions = new();
        private ICollectionView? _bonusConditionsView;

        public PurchaseWindow()
        {
            InitializeComponent();

            LoadCustomers();

            TransactionChannelComboBox.SelectedIndex = 0;
            BonusConditionTypeComboBox.SelectedIndex = 0;
            BonusConditionComboBox.IsEnabled = false;
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

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedValue is int)
            {
                LoadBonusConditions();
            }
        }

        private void BonusConditionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadBonusConditions();
        }

        private void LoadBonusConditions()
        {
            BonusConditionComboBox.ItemsSource = null;
            BonusConditionComboBox.Text = string.Empty;
            BonusConditionComboBox.IsEnabled = false;

            if (CustomerComboBox.SelectedValue is not int customerId)
                return;

            if (BonusConditionTypeComboBox.SelectedIndex <= 0)
                return;

            List<TransactionBonusConditionItem> items;

            if (BonusConditionTypeComboBox.SelectedIndex == 1)
            {
                items = _promotionService.GetActiveGeneralPromotionsForPurchase(customerId);
            }
            else
            {
                items = _customerOfferService.GetAvailableOffersForPurchase(customerId);
            }

            _bonusConditions = new ObservableCollection<TransactionBonusConditionItem>(items);

            _bonusConditionsView = CollectionViewSource.GetDefaultView(_bonusConditions);
            _bonusConditionsView.Filter = FilterBonusConditions;

            BonusConditionComboBox.ItemsSource = _bonusConditionsView;
            BonusConditionComboBox.IsEnabled = true;
        }

        private bool FilterBonusConditions(object item)
        {
            if (item is not TransactionBonusConditionItem condition)
                return false;

            var searchText = BonusConditionComboBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return condition.DisplayText.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase);
        }

        private void BonusConditionComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            _bonusConditionsView?.Refresh();

            if (!BonusConditionComboBox.IsDropDownOpen)
                BonusConditionComboBox.IsDropDownOpen = true;
        }

        private void BonusConditionComboBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            BonusConditionComboBox.IsDropDownOpen = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var conditionType = GetSelectedComboBoxText(BonusConditionTypeComboBox);

                int? promotionId = null;
                int? offerId = null;

                if (conditionType == "Общая акция" &&
                    BonusConditionComboBox.SelectedValue is int selectedPromotionId)
                {
                    promotionId = selectedPromotionId;
                }

                if (conditionType == "Персональное предложение" &&
                    BonusConditionComboBox.SelectedValue is int selectedOfferId)
                {
                    offerId = selectedOfferId;
                }

                var transaction = new CustomerTransaction
                {
                    TransactionType = TransactionTypeEnum.Purchase,
                    CustomerId = (int)CustomerComboBox.SelectedValue,
                    TransactionDatetime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                    TransactionAmount = ParseDecimal(TransactionAmountTextBox.Text),
                    BonusUsed = ParseDecimal(BonusUsedTextBox.Text),
                    PaidAmount = 0,
                    BonusAccrued = 0,
                    TransactionChannel = GetSelectedComboBoxText(TransactionChannelComboBox) == "Онлайн"
                        ? TransactionChannelEnum.Online
                        : TransactionChannelEnum.Offline,
                    PromotionId = promotionId,
                    OfferId = offerId,
                    Comment = string.IsNullOrWhiteSpace(CommentTextBox.Text)
                        ? null
                        : CommentTextBox.Text.Trim()
                };

                _transactionService.AddPurchase(transaction);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось добавить покупку.\n\n{ex.Message}",
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

            if (!TryParseDecimal(TransactionAmountTextBox.Text, out var transactionAmount) ||
                transactionAmount <= 0)
            {
                MessageBox.Show(
                    "Введите корректную сумму покупки больше 0.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (!TryParseDecimal(BonusUsedTextBox.Text, out var bonusUsed) ||
                bonusUsed < 0)
            {
                MessageBox.Show(
                    "Введите корректное количество списываемых бонусов.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            var maxBonusUsed = Math.Round(transactionAmount * 0.20m, 2);

            if (bonusUsed > maxBonusUsed)
            {
                MessageBox.Show(
                    $"Списать бонусами можно не более 20% от суммы покупки.\n\n" +
                    $"Сумма покупки: {transactionAmount:N2}\n" +
                    $"Максимум для списания: {maxBonusUsed:N2}\n" +
                    $"Указано к списанию: {bonusUsed:N2}",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            var conditionType = GetSelectedComboBoxText(BonusConditionTypeComboBox);

            if ((conditionType == "Общая акция" ||
                 conditionType == "Персональное предложение") &&
                BonusConditionComboBox.SelectedValue is not int)
            {
                MessageBox.Show(
                    "Выберите акцию или персональное предложение.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            text = text.Trim().Replace(',', '.');

            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static decimal ParseDecimal(string text)
        {
            if (!TryParseDecimal(text, out var value))
                throw new InvalidOperationException("Некорректное числовое значение.");

            return value;
        }

        private static string GetSelectedComboBoxText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? string.Empty;

            return comboBox.SelectedItem?.ToString() ?? string.Empty;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
