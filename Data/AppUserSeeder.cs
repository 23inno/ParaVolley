using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public static class AppUserSeeder
    {
        private const string PlayerEmail =
            "john.doe@email.com";

        private const string CoachEmail =
            "john.smith@paravolley.com";

        public static void Seed(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration)
        {
            SeedPlayer(
                context,
                passwordHasher,
                configuration);

            SeedCoach(
                context,
                passwordHasher,
                configuration);

            context.SaveChanges();
        }

        private static void SeedPlayer(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration)
        {
            var playerPassword =
                configuration["SeedUsers:PlayerPassword"];

            if (string.IsNullOrWhiteSpace(playerPassword))
            {
                throw new InvalidOperationException(
                    "The player password is missing from User Secrets.");
            }

            var player = context.Players.FirstOrDefault(
                playerItem =>
                    playerItem.Email == PlayerEmail);

            if (player is null)
            {
                throw new InvalidOperationException(
                    $"No player record exists for {PlayerEmail}.");
            }

            var user = context.AppUsers.FirstOrDefault(
                userItem =>
                    userItem.Email == PlayerEmail);

            if (user is null)
            {
                user = new AppUser
                {
                    Email = PlayerEmail
                };

                context.AppUsers.Add(user);
            }

            user.Role = AppUserRole.Player;
            user.IsActive = true;
            user.PlayerId = player.Id;
            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    playerPassword);
        }

        private static void SeedCoach(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration)
        {
            var coachPassword =
                configuration["SeedUsers:CoachPassword"];

            if (string.IsNullOrWhiteSpace(coachPassword))
            {
                throw new InvalidOperationException(
                    "The coach password is missing from User Secrets.");
            }

            var user = context.AppUsers.FirstOrDefault(
                userItem =>
                    userItem.Email == CoachEmail);

            if (user is null)
            {
                user = new AppUser
                {
                    Email = CoachEmail
                };

                context.AppUsers.Add(user);
            }

            user.Role = AppUserRole.Coach;
            user.IsActive = true;
            user.PlayerId = null;
            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    coachPassword);
        }
    }
}