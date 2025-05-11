using System;
using System.IO;
using System.Windows.Forms;
using Cryptig.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Cryptig;

namespace Cryptig
{
    public partial class Form1 : Form
    {
        private MistigVault? _vault;

        private Label lblLabel;
        private TextBox txtLabel;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblNotes;
        private TextBox txtNotes;
        private Button btnAddEntry;
        private Button btnSaveVault;
        private DataGridView dgvEntries;

        private readonly string _username;
        private readonly string _password;

        private Dictionary<int, string> _realPasswords = new Dictionary<int, string>();

        public Form1(MistigVault vault, string username, string password)
        {
            _vault = vault ?? throw new ArgumentNullException(nameof(vault));
            _username = username;
            _password = password;

            CreateUI();
            LoadVault();
        }

        private void CreateUI()
        {
            this.Text = "Cryptig Vault";
            this.Width = 800;
            this.Height = 600;

            lblLabel = new Label { Text = "Label", Top = 20, Left = 20, Width = 100 };
            txtLabel = new TextBox { Top = lblLabel.Bottom + 2, Left = 20, Width = 200 };

            lblUsername = new Label { Text = "Username", Top = txtLabel.Bottom + 10, Left = 20, Width = 100 };
            txtUsername = new TextBox { Top = lblUsername.Bottom + 2, Left = 20, Width = 200 };

            lblPassword = new Label { Text = "Password", Top = txtUsername.Bottom + 10, Left = 20, Width = 100 };
            txtPassword = new TextBox { Top = lblPassword.Bottom + 2, Left = 20, Width = 200 };
            txtPassword.UseSystemPasswordChar = true;

            var btnTogglePassword = new Button
            {
                Text = "Show",
                Top = txtPassword.Top,
                Left = txtPassword.Right + 10,
                Width = 60,
                Height = txtPassword.Height
            };
            btnTogglePassword.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnTogglePassword.Text = txtPassword.UseSystemPasswordChar ? "Show" : "Hide";
            };

            Controls.Add(btnTogglePassword);

            lblNotes = new Label { Text = "Notes", Top = txtPassword.Bottom + 10, Left = 20, Width = 100 };
            txtNotes = new TextBox { Top = lblNotes.Bottom + 2, Left = 20, Width = 200, Height = 60, Multiline = true };

            btnAddEntry = new Button { Text = "Add Entry", Top = txtNotes.Bottom + 10, Left = 20, Width = 100 };
            btnAddEntry.Click += BtnAddEntry_Click;

            btnSaveVault = new Button { Text = "Save Vault", Top = txtNotes.Bottom + 10, Left = 140, Width = 100 };
            btnSaveVault.Click += BtnSaveVault_Click;

            Label lblSearch = new Label { Text = "Search", Top = 20, Left = this.Width - 280, Width = 60 };
            TextBox txtSearch = new TextBox { Top = lblSearch.Bottom + 2, Left = lblSearch.Left, Width = 200 };
            txtSearch.TextChanged += (s, e) => ApplySearch(txtSearch.Text);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(lblSearch);
            Controls.Add(txtSearch);

            dgvEntries = new DataGridView
            {
                Top = btnAddEntry.Bottom + 20,
                Left = 20,
                Width = 740,
                Height = 300,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvEntries.CellClick += DgvEntries_CellClick;
            dgvEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEntries.MultiSelect = false;
            dgvEntries.AllowUserToAddRows = false;

            var btnReveal = new DataGridViewButtonColumn
            {
                Name = "RevealBtn",
                HeaderText = "",
                Text = "👁",
                UseColumnTextForButtonValue = true,
                Width = 30
            };
            dgvEntries.Columns.Add(btnReveal);

            var btnEdit = new DataGridViewButtonColumn
            {
                Name = "EditBtn",
                HeaderText = "",
                Text = "✏️",
                UseColumnTextForButtonValue = true,
                Width = 30
            };
            dgvEntries.Columns.Add(btnEdit);

            var btnDelete = new DataGridViewButtonColumn
            {
                Name = "DeleteBtn",
                HeaderText = "",
                Text = "🗑",
                UseColumnTextForButtonValue = true,
                Width = 30
            };
            dgvEntries.Columns.Add(btnDelete);

            Controls.AddRange(new Control[]
            {
                lblLabel, txtLabel,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblNotes, txtNotes,
                btnAddEntry, btnSaveVault,
                dgvEntries
            });
        }

        private void LoadVault()
        {
            string path = $"vault_{_username}.mistig";

            try
            {
                if (!File.Exists(path))
                    throw new Exception("This user does not exist.");

                _vault = MistigVault.Load(path, _password);

                dgvEntries.DataSource = _vault?.Data.Entries;

                foreach (DataGridViewRow row in dgvEntries.Rows)
                {
                    if (row.Cells["Password"].Value is string pwd)
                    {
                        _realPasswords[row.Index] = pwd;
                        int fakeLength = Math.Min(10, pwd.Length);
                        row.Cells["Password"].Value = new string('•', fakeLength);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Access denied: invalid username or password.");
                Environment.Exit(1);
            }
        }

        private void ApplySearch(string query)
        {
            if (_vault == null) return;

            query = query.ToLowerInvariant();

            var filtered = _vault.Data.Entries
                .Where(entry =>
                    entry.Label.ToLowerInvariant().Contains(query) ||
                    entry.Username.ToLowerInvariant().Contains(query) ||
                    entry.Notes.ToLowerInvariant().Contains(query))
                .ToList();

            dgvEntries.DataSource = null;
            dgvEntries.DataSource = filtered;

            for (int i = 0; i < filtered.Count; i++)
            {
                if (_realPasswords.TryGetValue(i, out string realPwd))
                {
                    int fakeLength = Math.Min(10, realPwd.Length);
                    dgvEntries.Rows[i].Cells["Password"].Value = new string('•', fakeLength);
                }
            }
        }

        private void BtnAddEntry_Click(object sender, EventArgs e)
        {
            if (_vault == null) return;

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                txtPassword.Text = PasswordGenerator.Generate(
                    length: 32,
                    includeUppercase: true,
                    includeLowercase: true,
                    includeDigits: true,
                    includeSymbols: true
                );
            }

            var entry = new VaultEntry
            {
                Label = txtLabel.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                Notes = txtNotes.Text
            };

            _vault.AddEntry(entry);
            Logger.Info($"Entry added: Label='{entry.Label}' for user='{_username}'");

            dgvEntries.DataSource = null;
            dgvEntries.DataSource = _vault.Data.Entries;
        }

        private void BtnSaveVault_Click(object sender, EventArgs e)
        {
            string path = $"vault_{_username}.mistig";
            BackupHelper.CreateDailyBackup(path, _username);

            _vault?.Save();
            Logger.Info($"Vault saved for user='{_username}'");
            MessageBox.Show("Vault saved successfully.");
        }

        private void DgvEntries_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var col = dgvEntries.Columns[e.ColumnIndex];
            var row = dgvEntries.Rows[e.RowIndex];

            if (col.Name == "RevealBtn")
            {
                if (_realPasswords.TryGetValue(e.RowIndex, out string realPwd))
                {
                    bool isMasked = row.Cells["Password"].Value?.ToString()?.StartsWith("•") ?? true;

                    row.Cells["Password"].Value = isMasked ? realPwd : new string('•', Math.Min(10, realPwd.Length));
                }
            }
            else if (col.Name == "Password")
            {
                if (_realPasswords.TryGetValue(e.RowIndex, out string realPwd))
                {
                    try
                    {
                        Clipboard.SetText(realPwd);
                        MessageBox.Show("Password copied!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Clipboard error: " + ex.Message);
                    }
                }
            }
            else if (col.Name == "EditBtn")
            {
                if (_vault != null && e.RowIndex >= 0 && e.RowIndex < _vault.Data.Entries.Count)
                {
                    var entry = _vault.Data.Entries[e.RowIndex];

                    txtLabel.Text = entry.Label;
                    txtUsername.Text = entry.Username;
                    txtPassword.Text = _realPasswords[e.RowIndex];
                    txtNotes.Text = entry.Notes;

                    Logger.Info($"Entry edited: Label='{entry.Label}' by user='{_username}'");
                    _vault.Data.Entries.RemoveAt(e.RowIndex);
                }

                dgvEntries.DataSource = null;
                dgvEntries.DataSource = _vault.Data.Entries;
            }
            else if (col.Name == "DeleteBtn")
            {
                if (_vault != null && e.RowIndex >= 0 && e.RowIndex < _vault.Data.Entries.Count)
                {
                    var confirm = MessageBox.Show("Are you sure you want to delete this entry?", "Confirm", MessageBoxButtons.YesNo);
                    if (confirm == DialogResult.Yes)
                    {
                        Logger.Info($"Entry deleted: Label='{_vault.Data.Entries[e.RowIndex].Label}' by user='{_username}'");
                        _vault.Data.Entries.RemoveAt(e.RowIndex);
                        _realPasswords.Remove(e.RowIndex);

                        dgvEntries.DataSource = null;
                        dgvEntries.DataSource = _vault.Data.Entries;
                    }
                }
            }
        }
    }
}