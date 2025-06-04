using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;


namespace Cryptig.Core
{
    /// <summary>
    /// FileVault provides simple encrypted storage for arbitrary files using
    /// the same Argon2id and AES-256-GCM scheme as the MistigVault.
    /// Files are stored in an encrypted zip archive inside a single ".misf" file.
    /// </summary>
    public class FileVault
    {
        private const string MagicHeader = "MISF";
        private const byte Version = 1;

        private readonly Dictionary<string, byte[]> _files;
        private byte[]? _key;
        private byte[]? _salt;
        private byte[]? _iv;
        private string? _path;

        public string? Path => _path;

        private FileVault()
        {
            _files = new Dictionary<string, byte[]>(); // Initialize readonly field in the constructor
        }

        public static FileVault CreateNew(string path, string password)
        {
            var vault = new FileVault
            {
                _salt = CryptoEngine.GenerateSalt(),
                _iv = CryptoEngine.GenerateIv(),
                _path = path
            };

            vault._key = CryptoEngine.DeriveKey(password, vault._salt);
            vault.Save();
            return vault;
        }

        public static FileVault Load(string path, string password)
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

            byte[] key = CryptoEngine.DeriveKey(password, salt);
            byte[] plaintext = CryptoEngine.Decrypt(ciphertext, key, iv, tag);

            var files = new Dictionary<string, byte[]>();
            using (var ms = new MemoryStream(plaintext))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    using var entryStream = entry.Open();
                    using var mem = new MemoryStream();
                    entryStream.CopyTo(mem);
                    files[entry.FullName] = mem.ToArray();
                }
            }

            var vault = new FileVault
            {
                _path = path,
                _salt = salt,
                _iv = iv,
                _key = key
            };

            foreach (var file in files)
            {
                vault._files.Add(file.Key, file.Value); // Populate readonly field using its instance
            }

            return vault;
        }

        public void AddFile(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath)); // Fix potential null reference issue
            string name = System.IO.Path.GetFileName(filePath); // Use fully qualified name to avoid ambiguity
            byte[] data = File.ReadAllBytes(filePath);
            _files[name] = data;
            Logger.Info($"File added to vault '{_path}': {name}");
        }

        public void RemoveFile(string fileName)
        {
            _files.Remove(fileName);
            Logger.Info($"File removed from vault '{_path}': {fileName}");
        }

        public IEnumerable<string> GetFileNames() => _files.Keys;

        public byte[] GetFileData(string fileName)
        {
            return _files[fileName];
        }

        public void ExtractAll(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory)); // Ensure directory is not null
            Directory.CreateDirectory(directory);
            foreach (var (name, data) in _files)
            {
                if (name == null) throw new ArgumentNullException(nameof(name)); // Ensure name is not null
                File.WriteAllBytes(System.IO.Path.Combine(directory, name), data); // Use fully qualified name for Path.Combine
            }
            Logger.Info($"Vault '{_path}' extracted to '{directory}'");
        }

        public void Save()
        {
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var (name, data) in _files)
                {
                    var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(data, 0, data.Length);
                }
            }
            byte[] plaintext = ms.ToArray();

            _iv = CryptoEngine.GenerateIv();
            byte[] ciphertext = CryptoEngine.Encrypt(plaintext, _key!, _iv, out byte[] tag);

            using FileStream fs = new FileStream(_path!, FileMode.Create, FileAccess.Write);
            using BinaryWriter writer = new BinaryWriter(fs);

            writer.Write(Encoding.ASCII.GetBytes(MagicHeader));
            writer.Write(Version);
            writer.Write(_salt!);
            writer.Write(_iv);
            writer.Write(tag);
            writer.Write(ciphertext.Length);
            writer.Write(ciphertext);

            Logger.Info($"Vault saved to '{_path}'");
        }
    }
}
