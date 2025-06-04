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
        private const byte Version = 2;

        private readonly Dictionary<string, byte[]> _files;
        private byte[]? _key;
        private byte[]? _salt;
        private byte[]? _iv;
        private string? _path;
        private string _owner = string.Empty;

        public string? Path => _path;
        public string Owner => _owner;

        private FileVault()
        {
            _files = new Dictionary<string, byte[]>();
        }

        public static FileVault CreateNew(string path, string password, string owner)
        {
            var vault = new FileVault
            {
                _salt = CryptoEngine.GenerateSalt(),
                _iv = CryptoEngine.GenerateIv(),
                _path = path,
                _owner = owner
            };

            vault._key = CryptoEngine.DeriveKey(password, vault._salt);
            vault.Save();
            return vault;
        }

        public static FileVault Load(string path, string password, string expectedOwner = "")
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new BinaryReader(fs);

            string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (magic != MagicHeader)
                throw new InvalidDataException("Invalid file format.");

            byte version = reader.ReadByte();
            string owner = string.Empty;
            if (version == 2)
            {
                int ownerLen = reader.ReadByte();
                owner = Encoding.UTF8.GetString(reader.ReadBytes(ownerLen));
                if (!string.IsNullOrEmpty(expectedOwner) && !owner.Equals(expectedOwner, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException("Vault owner mismatch.");
            }
            else if (version != 1)
            {
                throw new InvalidDataException("Unsupported file version.");
            }

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
                _key = key,
                _owner = owner
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

        public void RenameFile(string oldName, string newName)
        {
            if (!_files.ContainsKey(oldName)) return;
            byte[] data = _files[oldName];
            _files.Remove(oldName);
            _files[newName] = data;
            Logger.Info($"File renamed in vault '{_path}': {oldName} -> {newName}");
        }

        public IEnumerable<string> GetFileNames() => _files.Keys;

        public IEnumerable<(string Name, long Size, string Type)> GetFileInfos()
        {
            foreach (var kvp in _files)
            {
                string type = System.IO.Path.GetExtension(kvp.Key);
                yield return (kvp.Key, kvp.Value.Length, type);
            }
        }

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
            if (Version == 2)
            {
                byte[] ownerBytes = Encoding.UTF8.GetBytes(_owner);
                writer.Write((byte)ownerBytes.Length);
                writer.Write(ownerBytes);
            }
            writer.Write(_salt!);
            writer.Write(_iv);
            writer.Write(tag);
            writer.Write(ciphertext.Length);
            writer.Write(ciphertext);

            Logger.Info($"Vault saved to '{_path}'");
        }
    }
}
