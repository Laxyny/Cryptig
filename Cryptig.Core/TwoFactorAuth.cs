using OtpNet;
using System;

namespace Cryptig.Core
{
    public static class TwoFactorAuth
    {
        public static string GenerateSecret()
        {
            var bytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(bytes);
        }

        public static string GenerateCode(string secret)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.ComputeTotp();
        }

        public static bool VerifyCode(string secret, string code)
        {
            var totp = new Totp(Base32Encoding.ToBytes(secret));
            return totp.VerifyTotp(code, out long _);
        }
    }
}
