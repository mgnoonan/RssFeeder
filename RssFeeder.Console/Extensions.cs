﻿namespace RssFeeder.Console;

internal static class Extensions
{
    public static void Upsert<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
    {
        if (dict is null)
        {
            throw new ArgumentNullException(nameof(dict));
        }
        if (key is null)
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
