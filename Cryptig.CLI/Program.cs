using System;
using System.IO;
using Cryptig.Core;

namespace Cryptig.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: cryptig-cli <username> <password> <command> [args]");
                Console.WriteLine("Commands:\n  list\n  add <label> <user> <pass> [notes]\n  remove <label>");
                return;
            }

            string user = args[0];
            string pwd = args[1];
            string command = args[2];

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cryptig", "vaults", $"vault_{user}.mistig");

            if (!File.Exists(path))
            {
                Console.WriteLine("Vault not found");
                return;
            }

            MistigVault vault;
            try
            {
                vault = MistigVault.Load(path, pwd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load vault: {ex.Message}");
                return;
            }

            if (!string.IsNullOrEmpty(vault.Data.TwoFactorSecret))
            {
                Console.Write("Two-factor code: ");
                string? code = Console.ReadLine();
                if (string.IsNullOrEmpty(code) || !TwoFactorAuth.VerifyCode(vault.Data.TwoFactorSecret, code))
                {
                    Console.WriteLine("Invalid code");
                    return;
                }
            }

            switch (command.ToLower())
            {
                case "list":
                    foreach (var entry in vault.Data.Entries)
                    {
                        Console.WriteLine($"{entry.Label}: {entry.Username} / {entry.Password}");
                    }
                    break;
                case "add":
                    if (args.Length < 6)
                    {
                        Console.WriteLine("Usage: add <label> <user> <pass> [notes]");
                        break;
                    }
                    string label = args[3];
                    string u = args[4];
                    string p = args[5];
                    string notes = args.Length > 6 ? args[6] : string.Empty;
                    vault.AddEntry(new VaultEntry { Label = label, Username = u, Password = p, Notes = notes });
                    vault.Save();
                    Console.WriteLine("Entry added.");
                    break;
                case "remove":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Usage: remove <label>");
                        break;
                    }
                    string lbl = args[3];
                    var entryToRemove = vault.Data.Entries.Find(e => e.Label.Equals(lbl, StringComparison.OrdinalIgnoreCase));
                    if (entryToRemove == null)
                    {
                        Console.WriteLine("Entry not found");
                        break;
                    }
                    vault.Data.Entries.Remove(entryToRemove);
                    vault.Save();
                    Console.WriteLine("Entry removed.");
                    break;
                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
        }
    }
}
