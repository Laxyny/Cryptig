using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Cryptig.Core
{
    public class MistigVault
    {
        private const string MagicHeader = "MIST";
        private const byte Version = 1;

        public VaultData Data { get; private set; } = new();
        private byte[]? _key;
        private byte[]? _salt;
        private byte[]? _iv;
        private string? _path;

        private MistigVault() { }

        public static MistigVault CreateNew(string path, string masterPassword)
        {
            var vault = new MistigVault
            {
                _salt = CryptoEngine.GenerateSalt(),
                _iv = CryptoEngine.GenerateIv(),
                _path = path
            };

            vault._key = CryptoEngine.DeriveKey(masterPassword, vault._salt);

            vault.Data = new VaultData(); // start empty
            vault.Save();

            return vault;
        }

        public static MistigVault Load(string path, string masterPassword)
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(fs);

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != MagicHeader)
                throw new InvalidDataException("Invalid file format.");

            byte version = reader.ReadByte();
            if (version != Version)
                throw new InvalidDataException("Unsupported file version.");

            byte[] salt = reader.ReadBytes(16);
            byte[] iv = reader.ReadBytes(12);
            byte[] tag = reader.ReadBytes(16);
            int length = reader.ReadInt32();
            byte[] ciphertext = reader.ReadBytes(length);

            byte[] key = CryptoEngine.DeriveKey(masterPassword, salt);

            byte[] plaintext = CryptoEngine.Decrypt(ciphertext, key, iv, tag);

            string json = Encoding.UTF8.GetString(plaintext);
            VaultData data = JsonSerializer.Deserialize<VaultData>(json) ?? new VaultData();

            return new MistigVault
            {
                _path = path,
                _salt = salt,
                _iv = iv,
                _key = key,
                Data = data
            };
        }

        public void AddEntry(VaultEntry entry)
        {
            Data.Entries.Add(entry);
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(Data);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            _iv = CryptoEngine.GenerateIv();
            byte[] ciphertext = CryptoEngine.Encrypt(plaintext, _key, _iv, out byte[] tag);

            using FileStream fs = new FileStream(_path, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new BinaryWriter(fs);

            writer.Write(Encoding.ASCII.GetBytes(MagicHeader));
            writer.Write(Version);
            writer.Write(_salt);
            writer.Write(_iv);
            writer.Write(tag);
            writer.Write(ciphertext.Length);
            writer.Write(ciphertext);
        }
    }
}