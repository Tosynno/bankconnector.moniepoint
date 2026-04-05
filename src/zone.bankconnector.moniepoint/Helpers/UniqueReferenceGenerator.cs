namespace zone.bankconnector.moniepoint.Helpers
{
    public static class UniqueReferenceGenerator
    {
        private const string Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Generate(string prefix)
        {
            if (prefix.Length != 8)
                throw new ArgumentException("TeamApt prefix must be exactly 8 characters.", nameof(prefix));

            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            var suffix    = GenerateSuffix(12);
            return $"{prefix}{timestamp}{suffix}";
        }

        private static string GenerateSuffix(int length)
        {
            var buf = new char[length];
            for (var i = 0; i < length; i++)
                buf[i] = Chars[Random.Shared.Next(Chars.Length)];
            return new string(buf);
        }
    }
}
