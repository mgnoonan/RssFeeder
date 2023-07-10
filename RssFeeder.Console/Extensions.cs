namespace RssFeeder.Console;

internal static class Extensions
{
    public static bool IsNullOrEmptyOrData(this string value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith("#"))
            return true;

        return value.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase);
    }
}
