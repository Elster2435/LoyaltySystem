using Npgsql;

namespace LoyaltySystem.WService.Services
{
    public class DatabaseAutomationService
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<DatabaseAutomationService> _logger;

        public DatabaseAutomationService(
            NpgsqlDataSource dataSource,
            ILogger<DatabaseAutomationService> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task RunDailyAutomationAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запуск ежедневных регламентных операций.");

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var expiredOffersCount = await ExecuteFunctionAsync(
                connection,
                "select fn_expire_overdue_customer_offers();",
                cancellationToken);

            _logger.LogInformation(
                "Истечение просроченных персональных предложений выполнено. Изменено записей: {Count}",
                expiredOffersCount);

            var inactiveCustomersResult = await ExecuteInactiveCustomersFunctionAsync(
                connection,
                cancellationToken);

            _logger.LogInformation(
                "Обработка неактивных клиентов выполнена. Создано предложений для возврата клиента: {OffersCount}, деактивировано клиентов: {CustomersCount}",
                inactiveCustomersResult.ReturnOffersCreated,
                inactiveCustomersResult.CustomersDeactivated);

            var birthdayOffersCount = await ExecuteFunctionAsync(
                connection,
                "select fn_assign_birthday_customer_offers();",
                cancellationToken);

            _logger.LogInformation(
                "Назначение предложений ко дню рождения выполнено. Создано предложений: {Count}",
                birthdayOffersCount);

            _logger.LogInformation("Ежедневные регламентные операции завершены.");
        }

        private static async Task<int> ExecuteFunctionAsync(
            NpgsqlConnection connection,
            string sql,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        private static async Task<(int ReturnOffersCreated, int CustomersDeactivated)>
            ExecuteInactiveCustomersFunctionAsync(
                NpgsqlConnection connection,
                CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(
                "select return_offers_created, customers_deactivated from fn_process_inactive_customers();",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return (0, 0);

            var returnOffersCreated = reader.GetInt32(0);
            var customersDeactivated = reader.GetInt32(1);

            return (returnOffersCreated, customersDeactivated);
        }
    }
}
