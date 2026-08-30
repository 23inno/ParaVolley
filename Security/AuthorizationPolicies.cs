namespace SportsManagementMVC.Security
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = nameof(AdminOnly);
        public const string CoachOnly = nameof(CoachOnly);
        public const string PlayerOnly = nameof(PlayerOnly);
        public const string AdminOrCoach = nameof(AdminOrCoach);
    }
}
