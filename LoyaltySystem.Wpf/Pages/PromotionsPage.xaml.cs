using LoyaltySystem.Core.DTOs;
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
    public partial class PromotionsPage : Page
    {
        private readonly PromotionService _promotionService = new();
        private ObservableCollection<PromotionListItem> _promotions = new();
        private ICollectionView? _promotionsView;

        public PromotionsPage()
        {
            InitializeComponent();

            ApplyAccessPolicy();

            DataGridZoomHelper.ApplyDefault(PromotionsDataGrid, TableZoomTextBlock);

            PromotionTypeFilterComboBox.SelectedIndex = 0;

            LoadPromotions();
        }

        private void ApplyAccessPolicy()
        {
            var canManage = AccessPolicy.CanManagePromotions;

            AddPromotionButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            EditPromotionButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            DisablePromotionButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadPromotions()
        {
            try
            {
                _promotions = new ObservableCollection<PromotionListItem>(
                    _promotionService.GetListItems());

                _promotionsView = CollectionViewSource.GetDefaultView(_promotions);
                _promotionsView.Filter = FilterPromotions;

                PromotionsDataGrid.ItemsSource = _promotionsView;

                LoadRequiredLevelFilter();
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
            try
            {
                AccessPolicy.EnsureCanManagePromotions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            try
            {
                AccessPolicy.EnsureCanManagePromotions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            try
            {
                AccessPolicy.EnsureCanManagePromotions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

        private void LoadRequiredLevelFilter()
        {
            var currentSelectedLevel = RequiredLevelFilterComboBox.SelectedItem as string;

            var levels = _promotions
                .Select(x => string.IsNullOrWhiteSpace(x.RequiredLevelName)
                    ? "Без ограничения"
                    : x.RequiredLevelName)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            levels.Insert(0, "Все уровни");

            RequiredLevelFilterComboBox.ItemsSource = levels;

            if (!string.IsNullOrWhiteSpace(currentSelectedLevel) &&
                levels.Contains(currentSelectedLevel))
            {
                RequiredLevelFilterComboBox.SelectedItem = currentSelectedLevel;
            }
            else
            {
                RequiredLevelFilterComboBox.SelectedIndex = 0;
            }
        }

        private bool FilterPromotions(object item)
        {
            if (item is not PromotionListItem promotion)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(promotion.PromotionName, searchText) ||
                    ContainsIgnoreCase(promotion.Description, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedType = GetSelectedPromotionTypeFilter();

            if (!string.IsNullOrWhiteSpace(selectedType) &&
                !string.Equals(promotion.PromotionType, selectedType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedLevel = GetSelectedRequiredLevelFilter();

            if (!string.IsNullOrWhiteSpace(selectedLevel))
            {
                var promotionLevel = string.IsNullOrWhiteSpace(promotion.RequiredLevelName)
                    ? "Без ограничения"
                    : promotion.RequiredLevelName;

                if (!string.Equals(promotionLevel, selectedLevel, StringComparison.OrdinalIgnoreCase))
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

        private string? GetSelectedPromotionTypeFilter()
        {
            if (PromotionTypeFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var type = selectedItem.Content?.ToString();

            return type == "Все типы"
                ? null
                : type;
        }

        private string? GetSelectedRequiredLevelFilter()
        {
            if (RequiredLevelFilterComboBox.SelectedItem is not string levelName)
                return null;

            return levelName == "Все уровни"
                ? null
                : levelName;
        }

        private void RefreshFilter()
        {
            _promotionsView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void PromotionTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void RequiredLevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            PromotionTypeFilterComboBox.SelectedIndex = 0;
            RequiredLevelFilterComboBox.SelectedIndex = 0;

            RefreshFilter();
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(PromotionsDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(PromotionsDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(PromotionsDataGrid, TableZoomTextBlock);
        }
    }
}
