namespace RssFeeder.Console;

internal static class Extensions
{
    public static void Upsert<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        if (dict == null)
        {
            throw new ArgumentNullException("Invalid dictionary reference");
        }
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (dict.ContainsKey(key))
        {
            dict[key] = value;
        }
        else
        {
            dict.Add(key, value);
        }
    }
}
