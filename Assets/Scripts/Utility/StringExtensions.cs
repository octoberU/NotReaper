using System.Linq;

public static class StringExtensions
{
    public static string Sanitize(this string input)
        => string.IsNullOrEmpty(input) ? input : new string(input.Where(char.IsLetterOrDigit).ToArray());
}