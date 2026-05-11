using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class CustomerService
    {
        public List<Customer> GetAll()
        {
            using var db = DbContextFactory.Create();

            return db.Customers
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToList();
        }

        public Customer? GetById(int customerId)
        {
            using var db = DbContextFactory.Create();

            return db.Customers
                .FirstOrDefault(x => x.CustomerId == customerId);
        }

        public List<CustomerListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.Customers
                .AsNoTracking()
                .Include(x => x.LoyaltyAccount)
                    .ThenInclude(x => x!.Level)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .AsEnumerable()
                .Select(x => new CustomerListItem
                {
                    CustomerId = x.CustomerId,
                    LastName = x.LastName,
                    FirstName = x.FirstName,
                    MiddleName = x.MiddleName,
                    FullName = BuildFullName(x.LastName, x.FirstName, x.MiddleName),
                    Phone = x.Phone,
                    Email = x.Email,
                    Status = EnumDisplayHelper.GetPgName(x.Status),
                    LevelName = x.LoyaltyAccount?.Level?.LevelName ?? "Не указан",
                    BonusBalance = x.LoyaltyAccount?.BonusBalance ?? 0,
                    TotalSpent = x.LoyaltyAccount?.TotalSpent ?? 0,
                    RegistrationDate = x.RegistrationDate,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();
        }

        public List<CustomerComboBoxItem> GetComboBoxItems()
        {
            using var db = DbContextFactory.Create();

            return db.Customers
                .AsNoTracking()
                .Where(x => x.Status == StatusEnum.Active)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .AsEnumerable()
                .Select(x => new CustomerComboBoxItem
                {
                    CustomerId = x.CustomerId,
                    FullName = BuildFullName(x.LastName, x.FirstName, x.MiddleName),
                    Phone = x.Phone
                })
                .ToList();
        }

        public void Add(Customer customer)
        {
            using var db = DbContextFactory.Create();

            customer.RegistrationDate = default;
            customer.UpdatedAt = default;

            db.Customers.Add(customer);
            db.SaveChanges();
        }

        public void Update(Customer customer)
        {
            using var db = DbContextFactory.Create();

            var existingCustomer = db.Customers
                .FirstOrDefault(x => x.CustomerId == customer.CustomerId);

            if (existingCustomer == null)
                throw new InvalidOperationException("Клиент не найден.");

            existingCustomer.LastName = customer.LastName;
            existingCustomer.FirstName = customer.FirstName;
            existingCustomer.MiddleName = customer.MiddleName;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Email = customer.Email;
            existingCustomer.BirthDate = customer.BirthDate;
            existingCustomer.Gender = customer.Gender;
            if (existingCustomer.Status != customer.Status)
            {
                UpdateCustomerAndAccountStatus(db, existingCustomer.CustomerId, customer.Status);
            }

            db.CustomerActivities.Add(new CustomerActivity
            {
                CustomerId = existingCustomer.CustomerId,
                ActivityType = ActivityTypeEnum.ProfileChanged,
                ActivityDatetime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Description = "Изменены данные профиля клиента."
            });

            db.SaveChanges();
        }

        public void Delete(int customerId)
        {
            SetStatus(customerId, StatusEnum.Inactive);
        }

        private static string BuildFullName(string lastName, string firstName, string? middleName)
        {
            return string.IsNullOrWhiteSpace(middleName)
                ? $"{lastName} {firstName}"
                : $"{lastName} {firstName} {middleName}";
        }

        public void SetStatus(int customerId, StatusEnum status)
        {
            using var db = DbContextFactory.Create();

            var customer = db.Customers
                .FirstOrDefault(x => x.CustomerId == customerId);

            if (customer == null)
                throw new Exception("Клиент не найден.");

            if (customer.Status == status)
                throw new Exception($"Клиент уже имеет статус \"{GetStatusDisplayName(status)}\".");

            UpdateCustomerAndAccountStatus(db, customerId, status);

            customer.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            db.CustomerActivities.Add(new CustomerActivity
            {
                CustomerId = customerId,
                ActivityType = ActivityTypeEnum.ProfileChanged,
                ActivityDatetime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Description = $"Изменен статус клиента на \"{GetStatusDisplayName(status)}\"."
            });

            db.SaveChanges();
        }

        private static void UpdateCustomerAndAccountStatus(
            ApplicationDbContext db,
            int customerId,
            StatusEnum status)
        {
            var customer = db.Customers
                .FirstOrDefault(x => x.CustomerId == customerId);

            if (customer == null)
                throw new Exception("Клиент не найден.");

            customer.Status = status;

            var account = db.CustomerLoyaltyAccounts
                .FirstOrDefault(x => x.CustomerId == customerId);

            if (account != null)
            {
                account.AccountStatus = status;
            }
        }

        private static string GetStatusDisplayName(StatusEnum status)
        {
            return status switch
            {
                StatusEnum.Active => "Активный",
                StatusEnum.Inactive => "Неактивный",
                StatusEnum.Blocked => "Заблокирован",
                _ => status.ToString()
            };
        }
    }
}
