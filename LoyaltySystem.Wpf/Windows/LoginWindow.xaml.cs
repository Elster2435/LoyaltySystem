using LoyaltySystem.Core.Security;
using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new();

        public AuthUserDto? CurrentUser { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();

            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoginTextBox.Focus();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login();
            }
        }

        private void Login()
        {
            var login = LoginTextBox.Text.Trim();
            var password = PasswordBox.Password;

            try
            {
                CurrentUser = _authService.Login(login, password);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка входа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }
    }
}
