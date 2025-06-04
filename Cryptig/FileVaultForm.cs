using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Cryptig.Core;

namespace Cryptig
{
    public class FileVaultForm : Form
    {
        private readonly FileVault _vault;
        private readonly string _username;
        private readonly DataGridView _dgvFiles;
        private readonly Button _btnAdd;
        private readonly Button _btnRemove;
        private readonly Button _btnExtract;
        private readonly Button _btnOpen;
        private readonly Button _btnSave;
        private readonly ContextMenuStrip _contextMenu;
        private readonly PictureBox _preview;

        public FileVaultForm(FileVault vault, string username)
        {
            _vault = vault;
            _username = username;
            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (string file in files)
                        _vault.AddFile(file);
                    LoadFiles();
                }
            };

            Text = "File Vault";
            Width = 600;
            Height = 400;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;

            _dgvFiles = new DataGridView
            {
                Left = 20,
                Top = 20,
                Width = ClientSize.Width - 200,
                Height = ClientSize.Height - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvFiles.DoubleClick += (s, e) => OpenSelectedFile();
            _dgvFiles.AllowDrop = true;
            _dgvFiles.DragEnter += (s, e) =>
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                    e.Effect = DragDropEffects.Copy;
            };
            _dgvFiles.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (string file in files)
                        _vault.AddFile(file);
                    LoadFiles();
                }
            };

            _preview = new PictureBox
            {
                Left = _dgvFiles.Right + 10,
                Top = 20,
                Width = 150,
                Height = 150,
                SizeMode = PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Rename", null, (s, e) => RenameSelectedFile());
            _contextMenu.Items.Add("Delete", null, (s, e) => BtnRemove_Click(s, e));
            _dgvFiles.ContextMenuStrip = _contextMenu;
            _dgvFiles.SelectionChanged += (s, e) => UpdatePreview();

            _btnAdd = new Button { Text = "Add File", Left = 20, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnRemove = new Button { Text = "Remove File", Left = _btnAdd.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnExtract = new Button { Text = "Extract All", Left = _btnRemove.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnOpen = new Button { Text = "Open File", Left = _btnExtract.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnSave = new Button { Text = "Save && Close", Left = _btnOpen.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 110 };

            _btnAdd.Click += BtnAdd_Click;
            _btnRemove.Click += BtnRemove_Click;
            _btnExtract.Click += BtnExtract_Click;
            _btnOpen.Click += BtnOpen_Click;
            _btnSave.Click += BtnSave_Click;

            Controls.AddRange(new Control[] { _dgvFiles, _preview, _btnAdd, _btnRemove, _btnExtract, _btnOpen, _btnSave });

            LoadFiles();
        }

        private void LoadFiles()
        {
            _dgvFiles.DataSource = _vault.GetFileInfos()
                .Select(f => new { f.Name, Size = f.Size, Type = f.Type })
                .ToList();
            UpdatePreview();
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            OpenSelectedFile();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog { Multiselect = true, Title = "Select files" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in dlg.FileNames)
                    _vault.AddFile(file);
                LoadFiles();
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (_dgvFiles.SelectedRows.Count > 0)
            {
                string name = _dgvFiles.SelectedRows[0].Cells[0].Value.ToString() ?? string.Empty;
                _vault.RemoveFile(name);
                LoadFiles();
            }
        }

        private void RenameSelectedFile()
        {
            if (_dgvFiles.SelectedRows.Count == 0)
                return;
            string oldName = _dgvFiles.SelectedRows[0].Cells[0].Value.ToString() ?? string.Empty;
            string newName = Microsoft.VisualBasic.Interaction.InputBox("New name", "Rename", oldName);
            if (!string.IsNullOrWhiteSpace(newName) && newName != oldName)
            {
                _vault.RenameFile(oldName, newName);
                LoadFiles();
            }
        }

        private void BtnExtract_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _vault.ExtractAll(dlg.SelectedPath);
                MessageBox.Show("Files extracted.");
            }
        }

        private void UpdatePreview()
        {
            _preview.Image = null;
            if (_dgvFiles.SelectedRows.Count == 0)
                return;
            string name = _dgvFiles.SelectedRows[0].Cells[0].Value.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) return;
            string ext = System.IO.Path.GetExtension(name).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
            {
                try
                {
                    byte[] data = _vault.GetFileData(name);
                    using var ms = new MemoryStream(data);
                    _preview.Image = System.Drawing.Image.FromStream(ms);
                }
                catch {}
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            BackupHelper.CreateDailyBackup(_vault.Path!, _username);
            _vault.Save();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenSelectedFile()
        {
            if (_dgvFiles.SelectedRows.Count == 0)
                return;

            string name = _dgvFiles.SelectedRows[0].Cells[0].Value.ToString() ?? string.Empty;
            try
            {
                byte[] data = _vault.GetFileData(name);
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
                System.IO.File.WriteAllBytes(tempPath, data);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                Logger.Info($"Opened file '{name}' from vault '{_vault.Path}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file: {ex.Message}");
                Logger.Warn($"Failed to open '{name}' from vault '{_vault.Path}': {ex.Message}");
            }
        }
    }
}
