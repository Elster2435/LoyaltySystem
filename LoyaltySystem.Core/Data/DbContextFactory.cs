using LoyaltySystem.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LoyaltySystem.Core.Data
{
    public static class DbContextFactory
    {
        private const string ConnectionString =
            "Host=localhost;Port=5432;Database=db_diplom;Username=postgres;Password=postgres";

        public static ApplicationDbContext Create()
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);

            dataSourceBuilder.MapEnum<GenderEnum>("gender_enum");
            dataSourceBuilder.MapEnum<StatusEnum>("status_enum");
            dataSourceBuilder.MapEnum<TransactionChannelEnum>("transaction_channel_enum");
            dataSourceBuilder.MapEnum<TransactionTypeEnum>("transaction_type_enum");
            dataSourceBuilder.MapEnum<PromotionTypeEnum>("promotion_type_enum");
            dataSourceBuilder.MapEnum<OfferStatusEnum>("offer_status_enum");
            dataSourceBuilder.MapEnum<ActivityTypeEnum>("activity_type_enum");
            dataSourceBuilder.MapEnum<BonusTransactionTypeEnum>("bonus_transaction_type_enum");
            dataSourceBuilder.MapEnum<RoleNameEnum>("role_name_enum");

            var dataSource = dataSourceBuilder.Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseNpgsql(
                dataSource,
                options =>
                {
                    options.MapEnum<GenderEnum>("gender_enum");
                    options.MapEnum<StatusEnum>("status_enum");
                    options.MapEnum<TransactionChannelEnum>("transaction_channel_enum");
                    options.MapEnum<TransactionTypeEnum>("transaction_type_enum");
                    options.MapEnum<PromotionTypeEnum>("promotion_type_enum");
                    options.MapEnum<OfferStatusEnum>("offer_status_enum");
                    options.MapEnum<ActivityTypeEnum>("activity_type_enum");
                    options.MapEnum<BonusTransactionTypeEnum>("bonus_transaction_type_enum");
                    options.MapEnum<RoleNameEnum>("role_name_enum");
                });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
