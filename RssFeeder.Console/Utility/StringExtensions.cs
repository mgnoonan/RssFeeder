namespace RssFeeder.Console.Utility
{
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
}
