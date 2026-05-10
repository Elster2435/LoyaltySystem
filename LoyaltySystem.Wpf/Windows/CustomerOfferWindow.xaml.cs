using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Windows;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class CustomerOfferWindow : Window
    {
        private readonly CustomerService _customerService = new();
        private readonly PromotionService _promotionService = new();
        private readonly CustomerOfferService _customerOfferService = new();

        public CustomerOfferWindow()
        {
            InitializeComponent();

            LoadCustomers();
            LoadPromotions();

            ValidUntilDatePicker.SelectedDate = DateTime.Today.AddDays(14);
        }

        private void LoadCustomers()
        {
            CustomerComboBox.ItemsSource = _customerService.GetComboBoxItems();
        }

        private void LoadPromotions()
        {
            PromotionComboBox.ItemsSource = _promotionService.GetActivePersonalOfferPromotionItems();
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
