namespace bankconnector.moniepoint.Helpers
{
    public static class UniqueReferenceGenerator
    {
        private const string Digits = "0123456789";

        public static string Generate(string prefix)
        {
            if (prefix.Length != 8)
                throw new ArgumentException("Prefix must be exactly 8 characters.", nameof(prefix));

            var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss"); 
            var suffixLength = 32 - prefix.Length - timestamp.Length; // ensure total = 32

            var suffix = GenerateNumericSuffix(suffixLength);

            return $"{prefix}{timestamp}{suffix}";
        }

        private static string GenerateNumericSuffix(int length)
        {
            var buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = Digits[Random.Shared.Next(Digits.Length)];
            }
            return new string(buffer);
        }
    }
}
