using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Cryptig.Core;

namespace Cryptig
{
    public class FileVaultForm : Form
    {
        private readonly FileVault _vault;
        private readonly DataGridView _dgvFiles;
        private readonly Button _btnAdd;
        private readonly Button _btnRemove;
        private readonly Button _btnExtract;
        private readonly Button _btnSave;

        public FileVaultForm(FileVault vault)
        {
            _vault = vault;

            Text = "File Vault";
            Width = 600;
            Height = 400;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;

            _dgvFiles = new DataGridView
            {
                Left = 20,
                Top = 20,
                Width = ClientSize.Width - 40,
                Height = ClientSize.Height - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            _btnAdd = new Button { Text = "Add File", Left = 20, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnRemove = new Button { Text = "Remove File", Left = _btnAdd.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnExtract = new Button { Text = "Extract All", Left = _btnRemove.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 90 };
            _btnSave = new Button { Text = "Save && Close", Left = _btnExtract.Right + 10, Top = _dgvFiles.Bottom + 10, Width = 110 };

            _btnAdd.Click += BtnAdd_Click;
            _btnRemove.Click += BtnRemove_Click;
            _btnExtract.Click += BtnExtract_Click;
            _btnSave.Click += BtnSave_Click;

            Controls.AddRange(new Control[] { _dgvFiles, _btnAdd, _btnRemove, _btnExtract, _btnSave });

            LoadFiles();
        }

        private void LoadFiles()
        {
            _dgvFiles.DataSource = _vault.GetFileNames().Select(n => new { Name = n }).ToList();
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

        private void BtnExtract_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _vault.ExtractAll(dlg.SelectedPath);
                MessageBox.Show("Files extracted.");
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _vault.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
