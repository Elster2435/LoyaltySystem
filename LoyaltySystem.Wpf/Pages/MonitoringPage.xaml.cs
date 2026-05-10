using LoyaltySystem.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace LoyaltySystem.Wpf.Pages
{
    public partial class MonitoringPage : Page
    {
        private readonly MonitoringService _monitoringService = new();

        public MonitoringPage()
        {
            InitializeComponent();

            LoadAllMonitoringData();
        }

        private void LoadAllMonitoringData()
        {
            try
            {
                LoadSummary();
                LoadPeriodSummary(null, null);
                LoadTables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить данные мониторинга.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadSummary()
        {
            var summary = _monitoringService.GetSummary();

            TotalCustomersTextBlock.Text = summary.TotalCustomers.ToString();
            ActiveCustomersTextBlock.Text = summary.ActiveCustomers.ToString();
            TotalPurchasesTextBlock.Text = summary.TotalPurchases.ToString();
            TotalReturnsTextBlock.Text = summary.TotalReturns.ToString();

            GrossPurchaseAmountTextBlock.Text = FormatMoney(summary.GrossPurchaseAmount);
            NetRevenueTextBlock.Text = FormatMoney(summary.NetRevenue);

            ActivePromotionsTextBlock.Text = summary.ActivePromotions.ToString();
            ActiveOffersTextBlock.Text = summary.ActiveOffers.ToString();

            TotalBonusAccruedTextBlock.Text = FormatBonus(summary.TotalBonusAccrued);
            TotalBonusUsedTextBlock.Text = FormatBonus(summary.TotalBonusUsed);
            TotalBonusReturnedTextBlock.Text = FormatBonus(summary.TotalBonusReturned);
            TotalBonusCancelledTextBlock.Text = FormatBonus(summary.TotalBonusCancelled);
            TotalBonusCompensationTextBlock.Text = FormatMoney(summary.TotalBonusCompensationAmount);
        }

        private void LoadPeriodSummary(DateTime? startDate, DateTime? endDate)
        {
            var periodSummary = _monitoringService.GetPeriodSummary(startDate, endDate);

            PeriodPurchasesTextBlock.Text = periodSummary.TotalPurchases.ToString();
            PeriodReturnsTextBlock.Text = periodSummary.TotalReturns.ToString();

            PeriodGrossAmountTextBlock.Text = FormatMoney(periodSummary.GrossPurchaseAmount);
            PeriodNetRevenueTextBlock.Text = FormatMoney(periodSummary.NetRevenue);

            PeriodBonusAccruedTextBlock.Text = FormatBonus(periodSummary.TotalBonusAccrued);
            PeriodBonusUsedTextBlock.Text = FormatBonus(periodSummary.TotalBonusUsed);
            PeriodBonusReturnedTextBlock.Text = FormatBonus(periodSummary.TotalBonusReturned);
            PeriodBonusCancelledTextBlock.Text = FormatBonus(periodSummary.TotalBonusCancelled);
            PeriodBonusCompensationTextBlock.Text = FormatMoney(periodSummary.TotalBonusCompensationAmount);
        }

        private void LoadTables()
        {
            LoyaltyLevelStatisticsDataGrid.ItemsSource =
                _monitoringService.GetLoyaltyLevelStatistics();

            TopCustomersDataGrid.ItemsSource =
                _monitoringService.GetTopCustomers(10);

            InactiveCustomersDataGrid.ItemsSource =
                _monitoringService.GetInactiveCustomers(30);

            PromotionAnalyticsDataGrid.ItemsSource =
                _monitoringService.GetPromotionAnalytics();

            OfferAnalyticsDataGrid.ItemsSource =
                _monitoringService.GetOfferAnalytics();
        }

        private void ApplyPeriodFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                if (startDate != null &&
                    endDate != null &&
                    endDate.Value.Date < startDate.Value.Date)
                {
                    MessageBox.Show(
                        "Дата окончания не может быть раньше даты начала.",
                        "Проверка данных",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                LoadPeriodSummary(startDate, endDate);

                PromotionAnalyticsDataGrid.ItemsSource =
                    _monitoringService.GetPromotionAnalytics(startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось применить фильтр.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetPeriodFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartDatePicker.SelectedDate = null;
                EndDatePicker.SelectedDate = null;

                LoadPeriodSummary(null, null);

                PromotionAnalyticsDataGrid.ItemsSource =
                    _monitoringService.GetPromotionAnalytics();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сбросить фильтр.\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string FormatMoney(decimal value)
        {
            return $"{value:N2} ₽";
        }

        private static string FormatBonus(decimal value)
        {
            return $"{value:N2}";
        }
    }
}
