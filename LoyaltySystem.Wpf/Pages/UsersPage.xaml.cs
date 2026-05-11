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
    public partial class UsersPage : Page
    {
        private readonly AuthService _authService = new();

        private ObservableCollection<UserListItem> _users = new();
        private ICollectionView? _usersView;

        public UsersPage()
        {
            InitializeComponent();

            if (!AccessPolicy.CanManageUsers)
            {
                MessageBox.Show(
                    "Недостаточно прав для управления пользователями.",
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DataGridZoomHelper.ApplyDefault(UsersDataGrid, TableZoomTextBlock);

            RoleFilterComboBox.SelectedIndex = 0;
            StatusFilterComboBox.SelectedIndex = 0;

            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                _users = new ObservableCollection<UserListItem>(
                    _authService.GetUserListItems());

                _usersView = CollectionViewSource.GetDefaultView(_users);
                _usersView.Filter = FilterUsers;

                UsersDataGrid.ItemsSource = _usersView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить пользователей.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool FilterUsers(object item)
        {
            if (item is not UserListItem user)
                return false;

            var searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var containsSearchText =
                    ContainsIgnoreCase(user.FullName, searchText) ||
                    ContainsIgnoreCase(user.Login, searchText);

                if (!containsSearchText)
                    return false;
            }

            var selectedRole = GetSelectedRoleFilter();

            if (!string.IsNullOrWhiteSpace(selectedRole) &&
                !string.Equals(user.RoleName, selectedRole, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var selectedStatus = GetSelectedStatusFilter();

            if (!string.IsNullOrWhiteSpace(selectedStatus) &&
                !string.Equals(user.ActivityStatus, selectedStatus, StringComparison.OrdinalIgnoreCase))
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

        private string? GetSelectedRoleFilter()
        {
            if (RoleFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
                return null;

            var role = selectedItem.Content?.ToString();

            return role == "Все роли"
                ? null
                : role;
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
            _usersView?.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilter();
        }

        private void ResetFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            RoleFilterComboBox.SelectedIndex = 0;
            StatusFilterComboBox.SelectedIndex = 0;

            RefreshFilter();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AccessPolicy.EnsureCanManageUsers();

                var window = new UserWindow
                {
                    Owner = Window.GetWindow(this)
                };

                if (window.ShowDialog() == true)
                {
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AccessPolicy.EnsureCanManageUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UsersDataGrid.SelectedItem is not UserListItem selectedUser)
            {
                MessageBox.Show(
                    "Выберите пользователя для изменения.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new UserWindow(selectedUser.UserId)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                LoadUsers();
            }
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeUserActivityStatus(true);
        }

        private void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeUserActivityStatus(false);
        }

        private void ChangeUserActivityStatus(bool isActive)
        {
            try
            {
                AccessPolicy.EnsureCanManageUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UsersDataGrid.SelectedItem is not UserListItem selectedUser)
            {
                MessageBox.Show(
                    "Выберите пользователя.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var actionText = isActive ? "активировать" : "отключить";

            var result = MessageBox.Show(
                $"Вы действительно хотите {actionText} пользователя \"{selectedUser.FullName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _authService.SetUserActive(selectedUser.UserId, isActive);

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось изменить статус пользователя.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DecreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Decrease(UsersDataGrid, TableZoomTextBlock);
        }

        private void IncreaseTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Increase(UsersDataGrid, TableZoomTextBlock);
        }

        private void ResetTableZoomButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridZoomHelper.Reset(UsersDataGrid, TableZoomTextBlock);
        }
    }
}
