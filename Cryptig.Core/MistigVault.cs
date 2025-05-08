using System.Text;

namespace Cryptig.Core
{
    public static class MistigVault
    {
        private const string MagicHeader = "MIST";
        private const byte Version = 1;

        /// <summary>
        /// Creates a new .mistig file with encrypted default content.
        /// </summary>
        public static void CreateNew(string path, string masterPassword)
        {
            // Generate cryptographic material
            byte[] salt = CryptoEngine.GenerateSalt();
            byte[] iv = CryptoEngine.GenerateIv();
            byte[] key = CryptoEngine.DeriveKey(masterPassword, salt);

            // Payload to encrypt (example message)
            string message = "Cryptig Vault initialized.";
            byte[] plaintext = Encoding.UTF8.GetBytes(message);

            // Encrypt the payload
            byte[] ciphertext = CryptoEngine.Encrypt(plaintext, key, iv, out byte[] tag);

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new BinaryWriter(fs);

            // Write header
            writer.Write(Encoding.ASCII.GetBytes(MagicHeader)); // 4 bytes
            writer.Write(Version);                              // 1 byte
            writer.Write(salt);                                 // 16 bytes
            writer.Write(iv);                                   // 12 bytes
            writer.Write(tag);                                  // 16 bytes
            writer.Write(ciphertext.Length);                    // 4 bytes (int)
            writer.Write(ciphertext);                           // payload
        }
    }
}