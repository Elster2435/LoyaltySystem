using LoyaltySystem.WService.Services;

namespace LoyaltySystem.WService
{
    public class BackupWorker : BackgroundService
    {
        private readonly BackupService _backupService;
        private readonly ILogger<BackupWorker> _logger;

        public BackupWorker(
            BackupService backupService,
            ILogger<BackupWorker> logger)
        {
            _backupService = backupService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновый сервис резервного копирования запущен.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var intervalDays = _backupService.IntervalDays;
                var latestBackupTime = _backupService.GetLatestBackupTime();
                var now = DateTime.Now;

                var nextBackupTime = latestBackupTime.HasValue
                    ? latestBackupTime.Value.AddDays(intervalDays)
                    : now;

                if (nextBackupTime <= now)
                {
                    await RunBackupSafelyAsync(stoppingToken);
                    continue;
                }

                _logger.LogInformation(
                    "Следующая резервная копия будет создана: {NextBackupTime}",
                    nextBackupTime);

                var delay = nextBackupTime - now;

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task RunBackupSafelyAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _backupService.CreateBackupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка при создании резервной копии базы данных. Повтор через сутки.");

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
