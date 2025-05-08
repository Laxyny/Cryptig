using Konscious.Security.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cryptig.Core
{
    internal class CryptoEngine
    {
        private const int KeySize = 32; // 256 bits
        private const int SaltSize = 16; // 128 bits
        private const int IvSize = 12;   // 96 bits, recommended for AES-GCM

        /// <summary>
        /// Derives a cryptographic key from a password using Argon2id.
        /// </summary>
        public static byte[] DeriveKey(string password, byte[] salt)
        {
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 2,
                MemorySize = 65536, // 64 MB
                Iterations = 4
            };

            return argon2.GetBytes(32); // 256-bit key
        }

        /// <summary>
        /// Encrypts a plaintext byte array using AES-256-GCM.
        /// </summary>
        public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv, out byte[] tag)
        {
            using var aes = new AesGcm(key, 16);
            byte[] ciphertext = new byte[plaintext.Length];
            tag = new byte[16];

            aes.Encrypt(iv, plaintext, ciphertext, tag);

            return ciphertext;
        }

        /// <summary>
        /// Decrypts a ciphertext byte array using AES-256-GCM.
        /// </summary>
        public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv, byte[] tag)
        {
            using var aes = new AesGcm(key, 16);
            byte[] plaintext = new byte[ciphertext.Length];

            aes.Decrypt(iv, ciphertext, tag, plaintext);

            return plaintext;
        }

        /// <summary>
        /// Generates a cryptographically secure random salt.
        /// </summary>
        public static byte[] GenerateSalt()
        {
            return RandomBytes(SaltSize);
        }

        /// <summary>
        /// Generates a cryptographically secure random IV.
        /// </summary>
        public static byte[] GenerateIv()
        {
            return RandomBytes(IvSize);
        }

        /// <summary>
        /// Generates a cryptographically secure byte array of given length.
        /// </summary>
        private static byte[] RandomBytes(int length)
        {
            byte[] buffer = new byte[length];
            RandomNumberGenerator.Fill(buffer);
            return buffer;
        }
    }
}
