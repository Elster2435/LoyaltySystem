using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<LoyaltyLevel> LoyaltyLevels => Set<LoyaltyLevel>();

        public DbSet<CustomerLoyaltyAccount> CustomerLoyaltyAccounts => Set<CustomerLoyaltyAccount>();

        public DbSet<Promotion> Promotions => Set<Promotion>();

        public DbSet<CustomerOffer> CustomerOffers => Set<CustomerOffer>();

        public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();

        public DbSet<CustomerTransaction> Transactions => Set<CustomerTransaction>();

        public DbSet<BonusTransaction> BonusTransactions => Set<BonusTransaction>();

        public DbSet<Role> Roles => Set<Role>();

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurePostgresEnums(modelBuilder);

            ConfigureCustomers(modelBuilder);
            ConfigureLoyaltyLevels(modelBuilder);
            ConfigureCustomerLoyaltyAccounts(modelBuilder);
            ConfigurePromotions(modelBuilder);
            ConfigureCustomerOffers(modelBuilder);
            ConfigureCustomerActivities(modelBuilder);
            ConfigureTransactions(modelBuilder);
            ConfigureBonusTransactions(modelBuilder);
            ConfigureRoles(modelBuilder);
            ConfigureUsers(modelBuilder);
        }

        private static void ConfigurePostgresEnums(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<GenderEnum>("gender_enum");
            modelBuilder.HasPostgresEnum<StatusEnum>("status_enum");
            modelBuilder.HasPostgresEnum<TransactionChannelEnum>("transaction_channel_enum");
            modelBuilder.HasPostgresEnum<TransactionTypeEnum>("transaction_type_enum");
            modelBuilder.HasPostgresEnum<PromotionTypeEnum>("promotion_type_enum");
            modelBuilder.HasPostgresEnum<OfferStatusEnum>("offer_status_enum");
            modelBuilder.HasPostgresEnum<ActivityTypeEnum>("activity_type_enum");
            modelBuilder.HasPostgresEnum<BonusTransactionTypeEnum>("bonus_transaction_type_enum");
            modelBuilder.HasPostgresEnum<RoleNameEnum>("role_name_enum");
        }

        private static void ConfigureCustomers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("customers");

                entity.HasKey(x => x.CustomerId);

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id");

                entity.Property(x => x.LastName)
                    .HasColumnName("last_name")
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.FirstName)
                    .HasColumnName("first_name")
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(64);

                entity.Property(x => x.Phone)
                    .HasColumnName("phone")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasColumnName("email")
                    .HasMaxLength(128)
                    .IsRequired();

                entity.Property(x => x.BirthDate)
                    .HasColumnName("birth_date")
                    .HasColumnType("date");

                entity.Property(x => x.Gender)
                    .HasColumnName("gender")
                    .HasColumnType("gender_enum")
                    .IsRequired();

                entity.Property(x => x.RegistrationDate)
                    .HasColumnName("registration_date")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Status)
                    .HasColumnName("status")
                    .HasColumnType("status_enum")
                    .IsRequired();

                entity.Property(x => x.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAddOrUpdate();
            });
        }

        private static void ConfigureLoyaltyLevels(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LoyaltyLevel>(entity =>
            {
                entity.ToTable("loyalty_levels");

                entity.HasKey(x => x.LevelId);

                entity.Property(x => x.LevelId)
                    .HasColumnName("level_id");

                entity.Property(x => x.LevelName)
                    .HasColumnName("level_name")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.MinTotalSpent)
                    .HasColumnName("min_total_spent")
                    .HasPrecision(12, 2);

                entity.Property(x => x.BonusPercent)
                    .HasColumnName("bonus_percent")
                    .HasPrecision(5, 2);
            });
        }

        private static void ConfigureCustomerLoyaltyAccounts(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerLoyaltyAccount>(entity =>
            {
                entity.ToTable("customer_loyalty_accounts");

                entity.HasKey(x => x.AccountId);

                entity.Property(x => x.AccountId)
                    .HasColumnName("account_id");

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id");

                entity.Property(x => x.LevelId)
                    .HasColumnName("level_id");

                entity.Property(x => x.BonusBalance)
                    .HasColumnName("bonus_balance")
                    .HasPrecision(12, 2);

                entity.Property(x => x.TotalSpent)
                    .HasColumnName("total_spent")
                    .HasPrecision(12, 2);

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.AccountStatus)
                    .HasColumnName("account_status")
                    .HasColumnType("status_enum")
                    .IsRequired();

                entity.HasOne(x => x.Customer)
                    .WithOne(x => x.LoyaltyAccount)
                    .HasForeignKey<CustomerLoyaltyAccount>(x => x.CustomerId);

                entity.HasOne(x => x.Level)
                    .WithMany(x => x.LoyaltyAccounts)
                    .HasForeignKey(x => x.LevelId);
            });
        }

        private static void ConfigurePromotions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("promotions");

                entity.HasKey(x => x.PromotionId);

                entity.Property(x => x.PromotionId)
                    .HasColumnName("promotion_id");

                entity.Property(x => x.PromotionType)
                    .HasColumnName("promotion_type")
                    .HasColumnType("promotion_type_enum")
                    .IsRequired();

                entity.Property(x => x.PromotionName)
                    .HasColumnName("promotion_name")
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasColumnName("description");

                entity.Property(x => x.StartDate)
                    .HasColumnName("start_date")
                    .HasColumnType("date");

                entity.Property(x => x.EndDate)
                    .HasColumnName("end_date")
                    .HasColumnType("date");

                entity.Property(x => x.BonusMultiplier)
                    .HasColumnName("bonus_multiplier")
                    .HasPrecision(5, 2);

                entity.Property(x => x.ExtraBonus)
                    .HasColumnName("extra_bonus")
                    .HasPrecision(12, 2);

                entity.Property(x => x.RequiredLevelId)
                    .HasColumnName("required_level_id");

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active");

                entity.HasOne(x => x.RequiredLevel)
                    .WithMany(x => x.Promotions)
                    .HasForeignKey(x => x.RequiredLevelId);
            });
        }

        private static void ConfigureCustomerOffers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerOffer>(entity =>
            {
                entity.ToTable("customer_offers");

                entity.HasKey(x => x.OfferId);

                entity.Property(x => x.OfferId)
                    .HasColumnName("offer_id");

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id");

                entity.Property(x => x.PromotionId)
                    .HasColumnName("promotion_id");

                entity.Property(x => x.AssignedAt)
                    .HasColumnName("assigned_at")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.ValidUntil)
                    .HasColumnName("valid_until")
                    .HasColumnType("timestamp without time zone");

                entity.Property(x => x.OfferStatus)
                    .HasColumnName("offer_status")
                    .HasColumnType("offer_status_enum")
                    .IsRequired();

                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.Offers)
                    .HasForeignKey(x => x.CustomerId);

                entity.HasOne(x => x.Promotion)
                    .WithMany(x => x.Offers)
                    .HasForeignKey(x => x.PromotionId);
            });
        }

        private static void ConfigureCustomerActivities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerActivity>(entity =>
            {
                entity.ToTable("customer_activity");

                entity.HasKey(x => x.ActivityId);

                entity.Property(x => x.ActivityId)
                    .HasColumnName("activity_id");

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id");

                entity.Property(x => x.ActivityType)
                    .HasColumnName("activity_type")
                    .HasColumnType("activity_type_enum")
                    .IsRequired();

                entity.Property(x => x.ActivityDatetime)
                    .HasColumnName("activity_datetime")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Description)
                    .HasColumnName("description");

                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.Activities)
                    .HasForeignKey(x => x.CustomerId);
            });
        }

        private static void ConfigureTransactions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CustomerTransaction>(entity =>
            {
                entity.ToTable("transactions");

                entity.HasKey(x => x.TransactionId);

                entity.Property(x => x.TransactionId)
                    .HasColumnName("transaction_id");

                entity.Property(x => x.TransactionType)
                    .HasColumnName("transaction_type")
                    .HasColumnType("transaction_type_enum")
                    .IsRequired();

                entity.Property(x => x.OriginalTransactionId)
                    .HasColumnName("original_transaction_id");

                entity.Property(x => x.CustomerId)
                    .HasColumnName("customer_id");

                entity.Property(x => x.TransactionDatetime)
                    .HasColumnName("transaction_datetime")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.TransactionAmount)
                    .HasColumnName("transaction_amount")
                    .HasPrecision(12, 2);

                entity.Property(x => x.BonusUsed)
                    .HasColumnName("bonus_used")
                    .HasPrecision(12, 2);

                entity.Property(x => x.PaidAmount)
                    .HasColumnName("paid_amount")
                    .HasPrecision(12, 2);

                entity.Property(x => x.BonusAccrued)
                    .HasColumnName("bonus_accrued")
                    .HasPrecision(12, 2);

                entity.Property(x => x.TransactionChannel)
                    .HasColumnName("transaction_channel")
                    .HasColumnType("transaction_channel_enum")
                    .IsRequired();

                entity.Property(x => x.PromotionId)
                    .HasColumnName("promotion_id");

                entity.Property(x => x.OfferId)
                    .HasColumnName("offer_id");

                entity.Property(x => x.Comment)
                    .HasColumnName("comment");

                entity.HasOne(x => x.Customer)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.CustomerId);

                entity.HasOne(x => x.Promotion)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.PromotionId);

                entity.HasOne(x => x.Offer)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.OfferId);

                entity.HasOne(x => x.OriginalTransaction)
                    .WithMany(x => x.ReturnTransactions)
                    .HasForeignKey(x => x.OriginalTransactionId);
            });
        }

        private static void ConfigureBonusTransactions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BonusTransaction>(entity =>
            {
                entity.ToTable("bonus_transactions");

                entity.HasKey(x => x.BonusTransactionId);

                entity.Property(x => x.BonusTransactionId)
                    .HasColumnName("bonus_transaction_id");

                entity.Property(x => x.AccountId)
                    .HasColumnName("account_id");

                entity.Property(x => x.TransactionId)
                    .HasColumnName("transaction_id");

                entity.Property(x => x.BonusTransactionType)
                    .HasColumnName("bonus_transaction_type")
                    .HasColumnType("bonus_transaction_type_enum")
                    .IsRequired();

                entity.Property(x => x.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(12, 2);

                entity.Property(x => x.BonusTransactionDatetime)
                    .HasColumnName("bonus_transaction_datetime")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Description)
                    .HasColumnName("description");

                entity.HasOne(x => x.Account)
                    .WithMany(x => x.BonusTransactions)
                    .HasForeignKey(x => x.AccountId);

                entity.HasOne(x => x.Transaction)
                    .WithMany(x => x.BonusTransactions)
                    .HasForeignKey(x => x.TransactionId);
            });
        }

        private static void ConfigureRoles(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");

                entity.HasKey(x => x.RoleId);

                entity.Property(x => x.RoleId)
                    .HasColumnName("role_id");

                entity.Property(x => x.RoleName)
                    .HasColumnName("role_name")
                    .HasColumnType("role_name_enum")
                    .IsRequired();
            });
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(x => x.UserId);

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id");

                entity.Property(x => x.LastName)
                    .HasColumnName("last_name")
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.FirstName)
                    .HasColumnName("first_name")
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(x => x.MiddleName)
                    .HasColumnName("middle_name")
                    .HasMaxLength(64);

                entity.Property(x => x.Login)
                    .HasColumnName("login")
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .HasColumnName("password_hash")
                    .IsRequired();

                entity.Property(x => x.RoleId)
                    .HasColumnName("role_id");

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active");

                entity.Property(x => x.CreatedAt)
                    .HasColumnName("created_at")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UpdatedAt)
                    .HasColumnName("updated_at")
                    .HasColumnType("timestamp without time zone")
                    .HasDefaultValueSql("(current_timestamp at time zone 'Asia/Yekaterinburg')")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.RoleId);
            });
        }
    }
}
