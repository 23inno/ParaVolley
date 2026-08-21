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

        private const string AdminEmail =
            "admin@paravolley.com";

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

            SeedAdmin(
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

        private static void SeedAdmin(
            ApplicationDbContext context,
            IPasswordHasher<AppUser> passwordHasher,
            IConfiguration configuration)
        {
            var adminPassword =
                configuration["SeedUsers:AdminPassword"];

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "The admin password is missing from User Secrets.");
            }

            var user = context.AppUsers.FirstOrDefault(
                userItem =>
                    userItem.Email == AdminEmail);

            if (user is null)
            {
                user = new AppUser
                {
                    Email = AdminEmail
                };

                context.AppUsers.Add(user);
            }

            user.Role = AppUserRole.Admin;
            user.IsActive = true;
            user.PlayerId = null;
            user.PasswordHash =
                passwordHasher.HashPassword(
                    user,
                    adminPassword);
        }
    }
}