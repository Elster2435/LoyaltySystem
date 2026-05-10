using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class ReturnTransactionWindow : Window
    {
        private readonly TransactionService _transactionService = new();

        private List<ReturnableTransactionItem> _returnableTransactions = new();

        public ReturnTransactionWindow()
        {
            InitializeComponent();

            LoadReturnableTransactions();
        }

        private void LoadReturnableTransactions()
        {
            try
            {
                _returnableTransactions = _transactionService.GetReturnableTransactions();

                OriginalTransactionComboBox.ItemsSource = _returnableTransactions;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить покупки для возврата.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OriginalTransactionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OriginalTransactionComboBox.SelectedValue is not int transactionId)
            {
                ClearPurchaseInfo();
                return;
            }

            var transaction = _returnableTransactions
                .FirstOrDefault(x => x.TransactionId == transactionId);

            if (transaction == null)
            {
                ClearPurchaseInfo();
                return;
            }

            CustomerTextBlock.Text = transaction.CustomerFullName;
            DateTextBlock.Text = transaction.TransactionDatetime.ToString("dd.MM.yyyy HH:mm");
            AmountTextBlock.Text = $"{transaction.TransactionAmount:N2}";
            PaidAmountTextBlock.Text = $"{transaction.PaidAmount:N2}";
            BonusTextBlock.Text =
                $"Списано: {transaction.BonusUsed:N2}; начислено: {transaction.BonusAccrued:N2}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (OriginalTransactionComboBox.SelectedValue is not int originalTransactionId)
            {
                MessageBox.Show(
                    "Выберите покупку для возврата.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                "Оформить возврат по выбранной покупке?\n\n" +
                "Система вернет списанные бонусы и аннулирует все начисленные бонусы за покупку.",
                "Подтверждение возврата",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _transactionService.AddReturn(
                    originalTransactionId,
                    "Возврат покупки через приложение");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось оформить возврат.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearPurchaseInfo()
        {
            CustomerTextBlock.Text = string.Empty;
            DateTextBlock.Text = string.Empty;
            AmountTextBlock.Text = string.Empty;
            PaidAmountTextBlock.Text = string.Empty;
            BonusTextBlock.Text = string.Empty;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
