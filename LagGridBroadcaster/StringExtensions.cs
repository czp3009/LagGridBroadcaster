namespace LagGridBroadcaster
{
    internal static class StringExtensions
    {
        internal static string SubstringAfter(this string s, char delimiter, string missingDelimiterValue = null)
        {
            var index = s.IndexOf(delimiter);
            if (index != -1)
                return index == s.Length - 1 ? "" : s.Substring(index + 1);
            return missingDelimiterValue ?? s;
        }
    }
}