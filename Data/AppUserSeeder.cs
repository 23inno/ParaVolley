using Microsoft.AspNetCore.Identity;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Data
{
    public static class AppUserSeeder
    {
        public static void Seed(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher)
        {
            const string playerEmail = "john.doe@email.com";

            if (context.AppUsers.Any(user => user.Email == playerEmail))
            {
                return;
            }

            var player = context.Players
                .SingleOrDefault(player => player.Email == playerEmail);

            if (player == null)
            {
                throw new InvalidOperationException(
                    "The test player could not be found.");
            }

            var user = new AppUser
            {
                Email = playerEmail,
                Role = AppUserRole.Player,
                IsActive = true,
                PlayerId = player.Id
            };

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                "Player123!");

            context.AppUsers.Add(user);
            context.SaveChanges();
        }
    }
}