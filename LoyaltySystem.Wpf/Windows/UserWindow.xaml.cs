using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Security;
using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class UserWindow : Window
    {
        private readonly AuthService _authService = new();

        private readonly int? _userId;
        private bool _isEditMode;
        private bool _editingAdministrator;

        public UserWindow()
        {
            InitializeComponent();

            LoadRoles();

            _isEditMode = false;
            TitleTextBlock.Text = "Добавление пользователя";
            Title = "Добавление пользователя";
            IsActiveCheckBox.IsChecked = true;
        }

        public UserWindow(int userId)
        {
            InitializeComponent();

            _userId = userId;
            _isEditMode = true;

            LoadRoles();
            LoadUser();

            TitleTextBlock.Text = "Изменение пользователя";
            Title = "Изменение пользователя";
            PasswordHintTextBlock.Visibility = Visibility.Visible;
        }

        private void LoadRoles()
        {
            try
            {
                RoleComboBox.ItemsSource = _authService.GetRoleComboBoxItems();

                if (RoleComboBox.Items.Count > 0)
                    RoleComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить роли.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadUser()
        {
            if (_userId == null)
                return;

            try
            {
                var user = _authService.GetUserForEdit(_userId.Value);

                LastNameTextBox.Text = user.LastName;
                FirstNameTextBox.Text = user.FirstName;
                MiddleNameTextBox.Text = user.MiddleName;
                LoginTextBox.Text = user.Login;
                RoleComboBox.SelectedValue = user.RoleId;
                IsActiveCheckBox.IsChecked = user.IsActive;

                if (RoleComboBox.SelectedItem is RoleComboBoxItem selectedRole)
                {
                    _editingAdministrator = selectedRole.RoleName == "Администратор";
                }

                ConfigureCurrentPasswordVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить пользователя.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                DialogResult = false;
                Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RoleComboBox.SelectedValue is not int roleId)
                {
                    MessageBox.Show(
                        "Выберите роль пользователя.",
                        "Проверка данных",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var password = PasswordBox.Password;
                var currentPassword = CurrentPasswordBox.Password;
                var currentUser = CurrentUserContext.GetRequiredUser();

                var dto = new UserSaveDto
                {
                    UserId = _userId,
                    LastName = LastNameTextBox.Text,
                    FirstName = FirstNameTextBox.Text,
                    MiddleName = MiddleNameTextBox.Text,
                    Login = LoginTextBox.Text,

                    Password = string.IsNullOrWhiteSpace(password)
                        ? null
                        : password,

                    CurrentPassword = string.IsNullOrWhiteSpace(currentPassword)
                        ? null
                        : currentPassword,

                    RoleId = roleId,
                    IsActive = IsActiveCheckBox.IsChecked == true,

                    CurrentUserId = currentUser.UserId
                };

                if (_isEditMode)
                {
                    _authService.UpdateUser(dto);
                }
                else
                {
                    _authService.AddUser(dto);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfigureCurrentPasswordVisibility()
        {
            var showCurrentPassword = _isEditMode && _editingAdministrator;

            CurrentPasswordTextBlock.Visibility = showCurrentPassword
                ? Visibility.Visible
                : Visibility.Collapsed;

            CurrentPasswordPanel.Visibility = showCurrentPassword
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
