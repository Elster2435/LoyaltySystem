using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class PurchaseWindow : Window
    {
        private readonly CustomerService _customerService = new();
        private readonly PromotionService _promotionService = new();
        private readonly CustomerOfferService _customerOfferService = new();
        private readonly TransactionService _transactionService = new();

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
            CustomerComboBox.ItemsSource = _customerService.GetComboBoxItems();
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadBonusConditions();
        }

        private void BonusConditionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadBonusConditions();
        }

        private void LoadBonusConditions()
        {
            BonusConditionComboBox.ItemsSource = null;
            BonusConditionComboBox.IsEnabled = false;

            if (CustomerComboBox.SelectedValue is not int customerId)
                return;

            var conditionType = GetSelectedComboBoxText(BonusConditionTypeComboBox);

            if (conditionType == "Общая акция")
            {
                var promotions = _promotionService.GetActiveGeneralPromotionsForPurchase(customerId);

                BonusConditionComboBox.ItemsSource = promotions;
                BonusConditionComboBox.IsEnabled = promotions.Count > 0;
            }
            else if (conditionType == "Персональное предложение")
            {
                var offers = _customerOfferService.GetAvailableOffersForPurchase(customerId);

                BonusConditionComboBox.ItemsSource = offers;
                BonusConditionComboBox.IsEnabled = offers.Count > 0;
            }
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
