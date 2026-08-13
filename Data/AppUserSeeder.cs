using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public static class AppUserSeeder
    {
        private const string PlayerEmail = "john.doe@email.com";

        public static void Seed(
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
                player => player.Email == PlayerEmail);

            if (player is null)
            {
                throw new InvalidOperationException(
                    $"No player record exists for {PlayerEmail}.");
            }

            var user = context.AppUsers.FirstOrDefault(
                user => user.Email == PlayerEmail);

            if (user is null)
            {
                user = new AppUser
                {
                    Email = PlayerEmail,
                    Role = AppUserRole.Player,
                    IsActive = true,
                    PlayerId = player.Id
                };

                context.AppUsers.Add(user);
            }
            else
            {
                user.Role = AppUserRole.Player;
                user.IsActive = true;
                user.PlayerId = player.Id;
            }

            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    playerPassword);

            context.SaveChanges();
        }
    }
}