using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Windows;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class CustomerWindow : Window
    {
        private readonly CustomerService _customerService = new();

        private readonly int? _customerId;

        private Customer? _customer;

        public CustomerWindow()
        {
            InitializeComponent();

            _customerId = null;

            TitleTextBlock.Text = "Добавление клиента";

            GenderComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
        }

        public CustomerWindow(int customerId)
        {
            InitializeComponent();

            _customerId = customerId;

            TitleTextBlock.Text = "Изменение клиента";

            LoadCustomer();
        }

        private void LoadCustomer()
        {
            if (_customerId == null)
                return;

            _customer = _customerService.GetById(_customerId.Value);

            if (_customer == null)
            {
                MessageBox.Show(
                    "Клиент не найден.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                DialogResult = false;
                Close();
                return;
            }

            LastNameTextBox.Text = _customer.LastName;
            FirstNameTextBox.Text = _customer.FirstName;
            MiddleNameTextBox.Text = _customer.MiddleName;
            PhoneTextBox.Text = _customer.Phone;
            EmailTextBox.Text = _customer.Email;
            BirthDatePicker.SelectedDate = _customer.BirthDate;

            GenderComboBox.SelectedIndex = _customer.Gender switch
            {
                GenderEnum.Male => 0,
                GenderEnum.Female => 1,
                _ => 0
            };

            StatusComboBox.SelectedIndex = _customer.Status switch
            {
                StatusEnum.Active => 0,
                StatusEnum.Inactive => 1,
                StatusEnum.Blocked => 2,
                _ => 0
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var customer = _customer ?? new Customer();

                customer.LastName = LastNameTextBox.Text.Trim();
                customer.FirstName = FirstNameTextBox.Text.Trim();
                customer.MiddleName = string.IsNullOrWhiteSpace(MiddleNameTextBox.Text)
                    ? null
                    : MiddleNameTextBox.Text.Trim();

                customer.Phone = PhoneTextBox.Text.Trim();
                customer.Email = EmailTextBox.Text.Trim();
                customer.BirthDate = BirthDatePicker.SelectedDate;

                customer.Gender = GenderComboBox.SelectedIndex == 1
                    ? GenderEnum.Female
                    : GenderEnum.Male;

                customer.Status = StatusComboBox.SelectedIndex switch
                {
                    0 => StatusEnum.Active,
                    1 => StatusEnum.Inactive,
                    2 => StatusEnum.Blocked,
                    _ => StatusEnum.Active
                };

                if (_customerId == null)
                {
                    _customerService.Add(customer);
                }
                else
                {
                    customer.CustomerId = _customerId.Value;
                    _customerService.Update(customer);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сохранить клиента.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (GenderComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("Выберите пол.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (StatusComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("Выберите статус.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
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
