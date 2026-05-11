using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class CustomerActivitiesPage : Page
    {
        private readonly CustomerActivityService _customerActivityService = new();
        private ObservableCollection<CustomerActivityListItem> _customerActivities = new();
        private ICollectionView? _customerActivitiesView;

        public CustomerActivitiesPage()
        {
            InitializeComponent();

            ActivityTypeFilterComboBox.SelectedIndex = 0;

            LoadCustomerActivities();
        }

        private void LoadCustomerActivities()
        {
            try
            {
                _customerActivities = new ObservableCollection<CustomerActivityListItem>(
                    _customerActivityService.GetListItems());

                _customerActivitiesView = CollectionViewSource.GetDefaultView(_customerActivities);
                _customerActivitiesView.Filter = FilterCustomerActivities;

                CustomerActivitiesDataGrid.ItemsSource = _customerActivitiesView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить активность клиентов.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool FilterCustomerActivities(object item)
        {
            if (item is not CustomerActivityListItem activity)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(activity.CustomerFullName, searchText) ||
                    ContainsIgnoreCase(activity.Description, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedActivityType = GetSelectedActivityTypeFilter();

            if (!string.IsNullOrWhiteSpace(selectedActivityType) &&
                !string.Equals(activity.ActivityType, selectedActivityType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var activityDate = activity.ActivityDatetime.Date;

            if (StartDatePicker.SelectedDate != null &&
                activityDate < StartDatePicker.SelectedDate.Value.Date)
            {
                return false;
            }

            if (EndDatePicker.SelectedDate != null &&
                activityDate > EndDatePicker.SelectedDate.Value.Date)
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

        private string? GetSelectedActivityTypeFilter()
        {
            if (ActivityTypeFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var activityType = selectedItem.Content?.ToString();

            return activityType == "Все типы"
                ? null
                : activityType;
        }

        private void RefreshFilter()
        {
            _customerActivitiesView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ActivityTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            ActivityTypeFilterComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;

            RefreshFilter();
        }
    }
}
