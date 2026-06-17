using LoyaltySystem.WService;
using LoyaltySystem.WService.Services;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LoyaltyService.WService";
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Не найдена строка подключения DefaultConnection в appsettings.json.");
}

builder.Services.AddSingleton(_ =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});

builder.Services.AddSingleton<DatabaseAutomationService>();
builder.Services.AddSingleton<BackupService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<BackupWorker>();

var host = builder.Build();

host.Run();