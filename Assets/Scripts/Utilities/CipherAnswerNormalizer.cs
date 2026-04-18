namespace LudumDare2026.Utilities
{
    public static class CipherAnswerNormalizer
    {
        public static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
