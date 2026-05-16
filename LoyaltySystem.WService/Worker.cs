using LoyaltySystem.WService.Services;

namespace LoyaltySystem.WService
{
    public class Worker : BackgroundService
    {
        private readonly DatabaseAutomationService _databaseAutomationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<Worker> _logger;

        public Worker(
            DatabaseAutomationService databaseAutomationService,
            IConfiguration configuration,
            ILogger<Worker> logger)
        {
            _databaseAutomationService = databaseAutomationService;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновый сервис автоматизации запущен.");

            var runOnStartup = _configuration.GetValue<bool>("Automation:RunOnStartup");

            if (runOnStartup)
            {
                await RunAutomationSafelyAsync(stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var nextRunTime = GetNextRunTime();

                var delay = nextRunTime - DateTime.Now;

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                _logger.LogInformation(
                    "Следующий запуск регламентных операций: {NextRunTime}",
                    nextRunTime);

                await Task.Delay(delay, stoppingToken);

                await RunAutomationSafelyAsync(stoppingToken);
            }
        }

        private DateTime GetNextRunTime()
        {
            var hour = _configuration.GetValue<int>("Automation:DailyRunHour");
            var minute = _configuration.GetValue<int>("Automation:DailyRunMinute");

            var now = DateTime.Now;

            var nextRunTime = now.Date
                .AddHours(hour)
                .AddMinutes(minute);

            if (nextRunTime <= now)
                nextRunTime = nextRunTime.AddDays(1);

            return nextRunTime;
        }

        private async Task RunAutomationSafelyAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _databaseAutomationService.RunDailyAutomationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка при выполнении ежедневных регламентных операций.");
            }
        }
    }
}
