using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using LoyaltySystem.Wpf.Windows;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class PromotionsPage : Page
    {
        private readonly PromotionService _promotionService = new();

        public PromotionsPage()
        {
            InitializeComponent();

            LoadPromotions();
        }

        private void LoadPromotions()
        {
            try
            {
                PromotionsDataGrid.ItemsSource = _promotionService.GetListItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить акции.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new PromotionWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadPromotions();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (PromotionsDataGrid.SelectedItem is not PromotionListItem selectedPromotion)
            {
                MessageBox.Show(
                    "Выберите акцию для изменения.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new PromotionWindow(selectedPromotion.PromotionId)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadPromotions();
            }
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            if (PromotionsDataGrid.SelectedItem is not PromotionListItem selectedPromotion)
            {
                MessageBox.Show(
                    "Выберите акцию для отключения.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var result = MessageBox.Show(
                $"Отключить акцию \"{selectedPromotion.PromotionName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _promotionService.Disable(selectedPromotion.PromotionId);

                LoadPromotions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось отключить акцию.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
