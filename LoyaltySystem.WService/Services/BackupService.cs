using Npgsql;
using System.Diagnostics;

namespace LoyaltySystem.WService.Services
{
    public class BackupService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            IConfiguration configuration,
            ILogger<BackupService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string BackupDirectory =>
            _configuration.GetValue<string>("Backup:Directory")
            ?? throw new InvalidOperationException(
                "Не задан путь к папке резервных копий (Backup:Directory).");

        public int IntervalDays =>
            Math.Max(1, _configuration.GetValue<int>("Backup:IntervalDays"));

        public int RetentionCount =>
            Math.Max(1, _configuration.GetValue<int>("Backup:RetentionCount"));

        public DateTime? GetLatestBackupTime()
        {
            if (!Directory.Exists(BackupDirectory))
                return null;

            var files = Directory.GetFiles(BackupDirectory, "db_diplom_*.sql");

            if (files.Length == 0)
                return null;

            return files
                .Select(File.GetCreationTime)
                .Max();
        }

        public async Task CreateBackupAsync(CancellationToken cancellationToken)
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Не найдена строка подключения DefaultConnection.");

            var pgDumpPath =
                _configuration.GetValue<string>("Backup:PgDumpPath")
                ?? throw new InvalidOperationException(
                    "Не задан путь к pg_dump.exe (Backup:PgDumpPath).");

            if (!File.Exists(pgDumpPath))
                throw new FileNotFoundException(
                    $"Не найден pg_dump.exe по пути '{pgDumpPath}'.");

            Directory.CreateDirectory(BackupDirectory);

            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"db_diplom_{timestamp}.sql";
            var filePath = Path.Combine(BackupDirectory, fileName);

            _logger.LogInformation(
                "Создание резервной копии базы данных '{Database}' в файл '{File}'.",
                builder.Database,
                filePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-h");
            startInfo.ArgumentList.Add(builder.Host ?? "localhost");
            startInfo.ArgumentList.Add("-p");
            startInfo.ArgumentList.Add(builder.Port.ToString());
            startInfo.ArgumentList.Add("-U");
            startInfo.ArgumentList.Add(builder.Username ?? string.Empty);
            startInfo.ArgumentList.Add("-d");
            startInfo.ArgumentList.Add(builder.Database ?? string.Empty);
            startInfo.ArgumentList.Add("-F");
            startInfo.ArgumentList.Add("p");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add(filePath);

            startInfo.EnvironmentVariables["PGPASSWORD"] = builder.Password ?? string.Empty;

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Не удалось запустить процесс pg_dump.");

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                throw new InvalidOperationException(
                    $"pg_dump завершился с кодом {process.ExitCode}. Сообщение: {stderr}");
            }

            var fileInfo = new FileInfo(filePath);

            _logger.LogInformation(
                "Резервная копия создана. Размер файла: {Size} КБ.",
                fileInfo.Length / 1024);

            ApplyRetention();
        }

        private void ApplyRetention()
        {
            var files = Directory.GetFiles(BackupDirectory, "db_diplom_*.sql")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (files.Count <= RetentionCount)
                return;

            var toDelete = files.Skip(RetentionCount).ToList();

            foreach (var file in toDelete)
            {
                try
                {
                    file.Delete();
                    _logger.LogInformation(
                        "Удалена устаревшая резервная копия: {File}",
                        file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Не удалось удалить устаревшую резервную копию {File}.",
                        file.Name);
                }
            }
        }
    }
}
