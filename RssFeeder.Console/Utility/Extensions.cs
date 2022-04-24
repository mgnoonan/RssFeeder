namespace RssFeeder.Console.Utility;

public static class StringExtensions
{
    public static string Left(this string value, int length)
    {
        if (length >= value.Length)
            return value;

        return value.Substring(0, length);
    }

    public static string Right(this string value, int length)
    {
        if (length >= value.Length)
            return value;

        return value.Substring(value.Length - length, length);
    }
}

static class DictionaryExtensions
{
    public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
    {
        if (source is null) throw new ArgumentNullException("source");
        if (target is null) throw new ArgumentNullException("target");

        foreach (var keyValuePair in source)
        {
            target.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }
}
