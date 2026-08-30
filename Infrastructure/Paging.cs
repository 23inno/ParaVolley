namespace SportsManagementMVC.Infrastructure;

public static class Paging
{
    public const int DefaultPageSize = 25;
    public const int DefaultApiPageSize = 100;
    public const int MaximumApiPageSize = 200;

    public static int Page(int page) => Math.Max(1, page);

    public static int PageSize(int pageSize, int maximum) =>
        Math.Clamp(pageSize, 1, maximum);

    public static int PageCount(int totalItems, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
}
