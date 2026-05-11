using LoyaltySystem.Core.Data;
using LoyaltySystem.Core.Entities;
using LoyaltySystem.Core.Enums;
using LoyaltySystem.Core.Helpers;
using LoyaltySystem.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace LoyaltySystem.Core.Services
{
    public class AuthService
    {
        public AuthUserDto Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login))
                throw new Exception("Введите логин.");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Введите пароль.");

            var normalizedLogin = login.Trim().ToLower();

            using var db = DbContextFactory.Create();

            var user = db.Users
                .Include(x => x.Role)
                .AsNoTracking()
                .FirstOrDefault(x => x.Login.ToLower() == normalizedLogin);

            if (user == null)
                throw new Exception("Пользователь с таким логином не найден.");

            if (!user.IsActive)
                throw new Exception("Учетная запись пользователя отключена.");

            var isPasswordValid = PasswordHasher.VerifyPassword(password, user.PasswordHash);

            if (!isPasswordValid)
                throw new Exception("Неверный пароль.");

            if (user.Role == null)
                throw new Exception("Для пользователя не назначена роль.");

            return new AuthUserDto
            {
                UserId = user.UserId,
                Login = user.Login,
                LastName = user.LastName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                RoleName = user.Role.RoleName,
                RoleDisplayName = EnumDisplayHelper.GetPgName(user.Role.RoleName)
            };
        }

        public string GeneratePasswordHash(string password)
        {
            return PasswordHasher.HashPassword(password);
        }

        public List<UserListItem> GetUserListItems()
        {
            using var db = DbContextFactory.Create();

            return db.Users
                .Include(x => x.Role)
                .AsNoTracking()
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .AsEnumerable()
                .Select(x => new UserListItem
                {
                    UserId = x.UserId,
                    FullName = BuildFullName(x.LastName, x.FirstName, x.MiddleName),
                    Login = x.Login,
                    RoleName = x.Role == null
                        ? "Без роли"
                        : EnumDisplayHelper.GetPgName(x.Role.RoleName),
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();
        }

        public UserSaveDto GetUserForEdit(int userId)
        {
            using var db = DbContextFactory.Create();

            var user = db.Users
                .AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            return new UserSaveDto
            {
                UserId = user.UserId,
                LastName = user.LastName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                Login = user.Login,
                Password = null,
                RoleId = user.RoleId,
                IsActive = user.IsActive
            };
        }

        public List<RoleComboBoxItem> GetRoleComboBoxItems()
        {
            using var db = DbContextFactory.Create();

            return db.Roles
                .AsNoTracking()
                .OrderBy(x => x.RoleId)
                .AsEnumerable()
                .Select(x => new RoleComboBoxItem
                {
                    RoleId = x.RoleId,
                    RoleName = EnumDisplayHelper.GetPgName(x.RoleName)
                })
                .ToList();
        }

        public void AddUser(UserSaveDto dto)
        {
            ValidateUserData(dto, isNewUser: true);

            using var db = DbContextFactory.Create();

            var normalizedLogin = dto.Login.Trim().ToLower();

            var loginExists = db.Users
                .Any(x => x.Login.ToLower() == normalizedLogin);

            if (loginExists)
                throw new Exception("Пользователь с таким логином уже существует.");

            var roleExists = db.Roles.Any(x => x.RoleId == dto.RoleId);

            if (!roleExists)
                throw new Exception("Выбранная роль не найдена.");

            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            var user = new User
            {
                LastName = dto.LastName.Trim(),
                FirstName = dto.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName)
                    ? null
                    : dto.MiddleName.Trim(),
                Login = dto.Login.Trim(),
                PasswordHash = PasswordHasher.HashPassword(dto.Password!),
                RoleId = dto.RoleId,
                IsActive = dto.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Users.Add(user);
            db.SaveChanges();
        }

        public void UpdateUser(UserSaveDto dto)
        {
            if (dto.UserId == null)
                throw new Exception("Не указан пользователь для изменения.");

            ValidateUserData(dto, isNewUser: false);

            using var db = DbContextFactory.Create();

            var user = db.Users
                .Include(x => x.Role)
                .FirstOrDefault(x => x.UserId == dto.UserId.Value);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            var normalizedLogin = dto.Login.Trim().ToLower();

            var loginExists = db.Users
                .Any(x =>
                    x.UserId != user.UserId &&
                    x.Login.ToLower() == normalizedLogin);

            if (loginExists)
                throw new Exception("Пользователь с таким логином уже существует.");

            var newRole = db.Roles
                .FirstOrDefault(x => x.RoleId == dto.RoleId);

            if (newRole == null)
                throw new Exception("Выбранная роль не найдена.");

            var wasAdministrator = user.Role?.RoleName == RoleNameEnum.Administrator;
            var willBeAdministrator = newRole.RoleName == RoleNameEnum.Administrator;

            if (wasAdministrator && (!willBeAdministrator || !dto.IsActive))
            {
                EnsureCanRemoveAdministratorRights(db, user.UserId);
            }

            user.LastName = dto.LastName.Trim();
            user.FirstName = dto.FirstName.Trim();
            user.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName)
                ? null
                : dto.MiddleName.Trim();
            user.Login = dto.Login.Trim();
            user.RoleId = dto.RoleId;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = PasswordHasher.HashPassword(dto.Password);
            }

            db.SaveChanges();
        }

        public void SetUserActive(int userId, bool isActive)
        {
            using var db = DbContextFactory.Create();

            var user = db.Users
                .Include(x => x.Role)
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            if (user.IsActive == isActive)
            {
                var statusText = isActive ? "активен" : "отключен";
                throw new Exception($"Пользователь уже {statusText}.");
            }

            if (!isActive && user.Role?.RoleName == RoleNameEnum.Administrator)
            {
                EnsureCanRemoveAdministratorRights(db, user.UserId);
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            db.SaveChanges();
        }

        public void ChangePassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new Exception("Введите новый пароль.");

            if (newPassword.Length < 6)
                throw new Exception("Пароль должен содержать минимум 6 символов.");

            using var db = DbContextFactory.Create();

            var user = db.Users
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            db.SaveChanges();
        }

        private static void ValidateUserData(UserSaveDto dto, bool isNewUser)
        {
            if (string.IsNullOrWhiteSpace(dto.LastName))
                throw new Exception("Введите фамилию пользователя.");

            if (string.IsNullOrWhiteSpace(dto.FirstName))
                throw new Exception("Введите имя пользователя.");

            if (string.IsNullOrWhiteSpace(dto.Login))
                throw new Exception("Введите логин пользователя.");

            if (dto.Login.Trim().Length < 3)
                throw new Exception("Логин должен содержать минимум 3 символа.");

            if (dto.RoleId <= 0)
                throw new Exception("Выберите роль пользователя.");

            if (isNewUser)
            {
                if (string.IsNullOrWhiteSpace(dto.Password))
                    throw new Exception("Введите пароль пользователя.");

                if (dto.Password.Length < 6)
                    throw new Exception("Пароль должен содержать минимум 6 символов.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 6)
                    throw new Exception("Пароль должен содержать минимум 6 символов.");
            }
        }

        private static void EnsureCanRemoveAdministratorRights(ApplicationDbContext db, int userId)
        {
            var activeAdministratorsCount = db.Users
                .Include(x => x.Role)
                .Count(x =>
                    x.UserId != userId &&
                    x.IsActive &&
                    x.Role != null &&
                    x.Role.RoleName == RoleNameEnum.Administrator);

            if (activeAdministratorsCount == 0)
            {
                throw new Exception(
                    "Нельзя отключить или изменить роль последнего активного администратора.");
            }
        }

        private static string BuildFullName(string lastName, string firstName, string? middleName)
        {
            var parts = new List<string>
        {
            lastName,
            firstName
        };

            if (!string.IsNullOrWhiteSpace(middleName))
                parts.Add(middleName);

            return string.Join(" ", parts);
        }
    }
}
