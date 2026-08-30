using Microsoft.AspNetCore.Identity;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public static class AppUserSeeder
    {
        private const string PlayerEmail = "john.doe@email.com";
        private const string CoachEmail = "john.smith@paravolley.com";
        private const string AdminEmail = "admin@paravolley.com";

        public static void Seed(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                SeedDevelopmentPlayer(
                    context,
                    passwordHasher,
                    configuration["SeedUsers:PlayerPassword"]);

                SeedDevelopmentCoach(
                    context,
                    passwordHasher,
                    configuration["SeedUsers:CoachPassword"]);
            }

            var bootstrapAdminEnabled = configuration.GetValue<bool>(
                "SeedUsers:BootstrapAdminEnabled");

            if (environment.IsDevelopment() || bootstrapAdminEnabled)
            {
                SeedBootstrapAdmin(
                    context,
                    passwordHasher,
                    configuration["SeedUsers:AdminPassword"],
                    bootstrapAdminEnabled);
            }
        }

        private static void SeedDevelopmentPlayer(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                AccountExists(context, PlayerEmail))
            {
                return;
            }

            var player = context.Players.FirstOrDefault(playerItem =>
                playerItem.Email.ToLower() == PlayerEmail);

            if (player == null)
            {
                player = new Player
                {
                    Name = "John Doe",
                    Position = "Outside Hitter",
                    Team = "Team A",
                    Status = PlayerStatus.Active,
                    Age = 24,
                    Matches = 45,
                    Email = PlayerEmail,
                    Phone = "+27 76 000 0001",
                    Disability = "Wheelchair"
                };

                context.Players.Add(player);
                context.SaveChanges();
            }

            AddUser(
                context,
                passwordHasher,
                PlayerEmail,
                password,
                AppUserRole.Player,
                player.Id);
        }

        private static void SeedDevelopmentCoach(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            string? password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                AccountExists(context, CoachEmail))
            {
                return;
            }

            AddUser(
                context,
                passwordHasher,
                CoachEmail,
                password,
                AppUserRole.Coach,
                null);
        }

        private static void SeedBootstrapAdmin(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            string? password,
            bool explicitlyEnabled)
        {
            if (AccountExists(context, AdminEmail))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                if (explicitlyEnabled)
                {
                    throw new InvalidOperationException(
                        "Admin bootstrap is enabled but SeedUsers:AdminPassword is missing.");
                }

                return;
            }

            AddUser(
                context,
                passwordHasher,
                AdminEmail,
                password,
                AppUserRole.Admin,
                null);
        }

        private static bool AccountExists(
            ApplicationDbContext context,
            string normalizedEmail)
        {
            return context.AppUsers.Any(user =>
                user.NormalizedEmail == normalizedEmail);
        }

        private static void AddUser(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            string email,
            string password,
            AppUserRole role,
            int? playerId)
        {
            var user = new AppUser
            {
                Email = email,
                NormalizedEmail = email,
                Role = role,
                IsActive = true,
                PlayerId = playerId
            };

            user.PasswordHash = passwordHasher.HashPassword(user, password);
            context.AppUsers.Add(user);
            context.SaveChanges();
        }
    }
}
