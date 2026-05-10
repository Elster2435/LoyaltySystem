using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class CustomerOffersPage : Page
    {
        private readonly CustomerOfferService _customerOfferService = new();

        public CustomerOffersPage()
        {
            InitializeComponent();

            LoadOffers();
        }

        private void LoadOffers()
        {
            try
            {
                CustomerOffersDataGrid.ItemsSource = _customerOfferService.GetListItems();
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
            if (CustomerOffersDataGrid.SelectedItem is not CustomerOfferListItem selectedOffer)
            {
                MessageBox.Show(
                    "Выберите персональное предложение.",
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
    }
}
