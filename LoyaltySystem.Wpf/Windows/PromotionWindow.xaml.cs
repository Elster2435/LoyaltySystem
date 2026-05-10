using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Services;
using System.Globalization;
using System.Windows;

namespace LoyaltySystem.Wpf.Windows
{
    public partial class PromotionWindow : Window
    {
        private readonly PromotionService _promotionService = new();
        private readonly LoyaltyLevelService _loyaltyLevelService = new();

        private readonly int? _promotionId;

        private Promotion? _promotion;

        public PromotionWindow()
        {
            InitializeComponent();

            _promotionId = null;

            TitleTextBlock.Text = "Добавление акции";

            LoadLevels();

            PromotionTypeComboBox.SelectedIndex = 0;
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(14);
        }

        public PromotionWindow(int promotionId)
        {
            InitializeComponent();

            _promotionId = promotionId;

            TitleTextBlock.Text = "Изменение акции";

            LoadLevels();
            LoadPromotion();
        }

        private void LoadLevels()
        {
            var levels = _loyaltyLevelService.GetComboBoxItems();

            levels.Insert(0, new LoyaltyLevel
            {
                LevelId = 0,
                LevelName = "Без ограничения"
            });

            RequiredLevelComboBox.ItemsSource = levels;
            RequiredLevelComboBox.SelectedValue = 0;
        }

        private void LoadPromotion()
        {
            if (_promotionId == null)
                return;

            _promotion = _promotionService.GetById(_promotionId.Value);

            if (_promotion == null)
            {
                MessageBox.Show(
                    "Акция не найдена.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                DialogResult = false;
                Close();
                return;
            }

            PromotionNameTextBox.Text = _promotion.PromotionName;
            DescriptionTextBox.Text = _promotion.Description;
            StartDatePicker.SelectedDate = _promotion.StartDate;
            EndDatePicker.SelectedDate = _promotion.EndDate;
            BonusMultiplierTextBox.Text = _promotion.BonusMultiplier.ToString(CultureInfo.InvariantCulture);
            ExtraBonusTextBox.Text = _promotion.ExtraBonus.ToString(CultureInfo.InvariantCulture);
            RequiredLevelComboBox.SelectedValue = _promotion.RequiredLevelId ?? 0;

            PromotionTypeComboBox.SelectedIndex = _promotion.PromotionType switch
            {
                PromotionTypeEnum.General => 0,
                PromotionTypeEnum.Personal => 1,
                PromotionTypeEnum.NewCustomer => 2,
                PromotionTypeEnum.Birthday => 3,
                PromotionTypeEnum.CustomerReturn => 4,
                PromotionTypeEnum.PurchaseReturn => 5,
                _ => 0
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                var promotion = _promotion ?? new Promotion();

                promotion.PromotionName = PromotionNameTextBox.Text.Trim();

                promotion.PromotionType = PromotionTypeComboBox.SelectedIndex switch
                {
                    0 => PromotionTypeEnum.General,
                    1 => PromotionTypeEnum.Personal,
                    2 => PromotionTypeEnum.NewCustomer,
                    3 => PromotionTypeEnum.Birthday,
                    4 => PromotionTypeEnum.CustomerReturn,
                    5 => PromotionTypeEnum.PurchaseReturn,
                    _ => PromotionTypeEnum.General
                };

                promotion.StartDate = StartDatePicker.SelectedDate!.Value.Date;
                promotion.EndDate = EndDatePicker.SelectedDate!.Value.Date;
                promotion.BonusMultiplier = ParseDecimal(BonusMultiplierTextBox.Text);
                promotion.ExtraBonus = ParseDecimal(ExtraBonusTextBox.Text);

                promotion.RequiredLevelId = RequiredLevelComboBox.SelectedValue is int levelId && levelId > 0
                    ? levelId
                    : null;

                promotion.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text)
                    ? null
                    : DescriptionTextBox.Text.Trim();

                promotion.IsActive = _promotionId == null
                    ? true
                    : _promotion?.IsActive ?? true;

                if (_promotionId == null)
                {
                    _promotionService.Add(promotion);
                }
                else
                {
                    promotion.PromotionId = _promotionId.Value;
                    _promotionService.Update(promotion);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сохранить акцию.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(PromotionNameTextBox.Text))
            {
                MessageBox.Show(
                    "Введите название акции.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (PromotionTypeComboBox.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Выберите тип акции.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    "Выберите дату начала акции.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show(
                    "Выберите дату окончания акции.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (EndDatePicker.SelectedDate.Value.Date < StartDatePicker.SelectedDate.Value.Date)
            {
                MessageBox.Show(
                    "Дата окончания не может быть раньше даты начала.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (!TryParseDecimal(BonusMultiplierTextBox.Text, out var bonusMultiplier) ||
                bonusMultiplier < 1)
            {
                MessageBox.Show(
                    "Введите корректный множитель бонусов. Значение должно быть не меньше 1.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            if (!TryParseDecimal(ExtraBonusTextBox.Text, out var extraBonus) ||
                extraBonus < 0)
            {
                MessageBox.Show(
                    "Введите корректное количество дополнительных бонусов. Значение не может быть отрицательным.",
                    "Проверка данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            text = text.Trim().Replace(',', '.');

            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static decimal ParseDecimal(string text)
        {
            if (!TryParseDecimal(text, out var value))
                throw new InvalidOperationException("Некорректное числовое значение.");

            return value;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
