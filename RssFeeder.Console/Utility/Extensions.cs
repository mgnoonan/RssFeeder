using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Helper class containing .NET extenstion methods
    /// </summary>
    public static class Extensions
    {
        public static string Left(this string s, int length)
        {
            if (length > s.Length)
                return s;

            return s.Substring(0, length);
        }

        public static string Right(this string s, int length)
        {
            if (length > s.Length)
                return s;

            return s.Substring(s.Length - length);
        }

        public static string StripAfter(this string s, string searchValue)
        {
            int pos = s.IndexOf(searchValue);
            if (pos == -1)
                return s;

            return s.Substring(0, pos);
        }

        public static string StripBefore(this string s, string searchValue)
        {
            int pos = s.IndexOf(searchValue);
            if (pos == -1)
                return s;

            return s.Substring(0, pos - 1);
        }

        public static bool StartsWithAlphaNumeric(this string s)
        {
            char firstChar = s.ToLowerInvariant().ToCharArray(0, 1)[0];

            return (firstChar >= 'a' && firstChar <= 'z');
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            if (s == null) return true;

            return string.IsNullOrEmpty(s.Trim());
        }

        public static bool IsNumeric(this string s)
        {
            Regex pattern = new Regex("[^0-9]");
            return !pattern.IsMatch(s);
        }
    }
}
