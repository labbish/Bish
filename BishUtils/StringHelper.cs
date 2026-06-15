namespace BishUtils;

public static class StringHelper
{
    extension(string str)
    {
        public string RemoveStart(string sub) => str.StartsWith(sub) ? str[sub.Length..] : str;
        public string RemoveEnd(string sub) => str.EndsWith(sub) ? str[..^sub.Length] : str;
    }
}