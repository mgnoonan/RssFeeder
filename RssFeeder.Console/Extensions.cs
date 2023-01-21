namespace RssFeeder.Console;

internal static class Extensions
{
    public static bool IsNullOrEmptyOrData(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return true;
        
        return value.StartsWith("data:", StringComparison.InvariantCultureIgnoreCase);
    }
}
