using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.DTOs;
using LoyaltySystem.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class CustomerActivityService
    {
        public List<CustomerActivityListItem> GetListItems()
        {
            using var db = DbContextFactory.Create();

            return db.CustomerActivities
                .AsNoTracking()
                .Include(x => x.Customer)
                .OrderByDescending(x => x.ActivityDatetime)
                .AsEnumerable()
                .Select(x => new CustomerActivityListItem
                {
                    ActivityId = x.ActivityId,
                    CustomerId = x.CustomerId,
                    CustomerFullName = x.Customer == null
                        ? "Клиент удален"
                        : BuildFullName(x.Customer.LastName, x.Customer.FirstName, x.Customer.MiddleName),
                    ActivityType = EnumDisplayHelper.GetPgName(x.ActivityType),
                    ActivityDatetime = x.ActivityDatetime,
                    Description = x.Description ?? string.Empty
                })
                .ToList();
        }

        private static string BuildFullName(string lastName, string firstName, string? middleName)
        {
            return string.IsNullOrWhiteSpace(middleName)
                ? $"{lastName} {firstName}"
                : $"{lastName} {firstName} {middleName}";
        }
    }
}
