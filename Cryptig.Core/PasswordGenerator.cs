using System.Text;

namespace Cryptig.Core
{
    public static class PasswordGenerator
    {
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";

        public static string Generate(int length = 16, bool includeUppercase = true, bool includeLowercase = true,
                                      bool includeDigits = true, bool includeSymbols = true)
        {
            var charPool = new StringBuilder();

            if (includeLowercase) charPool.Append(Lowercase);
            if (includeUppercase) charPool.Append(Uppercase);
            if (includeDigits) charPool.Append(Digits);
            if (includeSymbols) charPool.Append(Symbols);

            if (charPool.Length == 0)
                throw new ArgumentException("At least one character set must be selected.");

            var chars = charPool.ToString();
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
