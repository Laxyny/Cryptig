using System;
using System.IO;
using System.Linq;

namespace Cryptig.Core
{
    public static class BackupHelper
    {
        public static void CreateDailyBackup(string vaultPath, string username)
        {
            try
            {
                if (!File.Exists(vaultPath))
                    return;

                string backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Cryptig", "backups", username
                );

                Directory.CreateDirectory(backupDir);

                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string backupFile = Path.Combine(backupDir, $"vault_backup_{today}.mistig");

                if (!File.Exists(backupFile))
                {
                    File.Copy(vaultPath, backupFile);
                    Logger.Info($"Backup created: {backupFile}");
                }

                // Keep only last 7 daily backups
                var backupFiles = Directory.GetFiles(backupDir, "vault_backup_*.mistig")
                                           .OrderByDescending(File.GetCreationTime)
                                           .Skip(7);

                foreach (var file in backupFiles)
                {
                    File.Delete(file);
                    Logger.Info($"Old backup deleted: {file}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Backup failed for '{username}': {ex.Message}");
            }
        }
    }
}
