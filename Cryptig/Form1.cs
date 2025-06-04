using System;
using System.IO;
using System.Windows.Forms;
using Cryptig.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Cryptig;
using Timer = System.Windows.Forms.Timer;

namespace Cryptig
{
    public partial class Form1 : Form
    {
        private readonly UserPreferences _userPrefs;

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
        private HashSet<int> _revealedRows = new HashSet<int>();

        private readonly InactivityService _idleService;
        private readonly Timer _inactivityTimer;

        public Form1(MistigVault vault, string username, string password)
        {
            _userPrefs = UserPreferences.Load();

            _vault = vault ?? throw new ArgumentNullException(nameof(vault));
            _username = username;
            _password = password;

            _idleService = new InactivityService(TimeSpan.FromMinutes(5));

            _inactivityTimer = new Timer { Interval = 10_000 };
            _inactivityTimer.Tick += InactivityTimer_Tick;
            _inactivityTimer.Start();

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            CreateUI();
            LoadVault();
        }

        private void CreateUI()
        {
            this.Text = "Cryptig Vault";
            this.Width = 800;
            this.Height = 600;

            // Menu with file, edition...
            MenuStrip menuStrip = new MenuStrip();

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem importItem = new ToolStripMenuItem("Import Vault");
            ToolStripMenuItem exportItem = new ToolStripMenuItem("Export Vault");
            ToolStripMenuItem lockItem = new ToolStripMenuItem("Lock Vault");
            ToolStripMenuItem createFVItem = new ToolStripMenuItem("Create File Vault");
            ToolStripMenuItem openFVItem = new ToolStripMenuItem("Open File Vault");
            importItem.Click += ImportVault_Click;
            exportItem.Click += ExportVault_Click;
            lockItem.Click += (s, e) => LockVault();
            createFVItem.Click += CreateFileVault_Click;
            openFVItem.Click += OpenFileVault_Click;

            menuStrip.Dock = DockStyle.Top;
            fileMenu.DropDownItems.Add(importItem);
            fileMenu.DropDownItems.Add(exportItem);
            fileMenu.DropDownItems.Add(lockItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(createFVItem);
            fileMenu.DropDownItems.Add(openFVItem);
            menuStrip.Items.Add(fileMenu);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About Cryptig");
            aboutItem.Click += (s, e) => new AboutForm().ShowDialog();
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);

            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;

            int y = menuStrip.Height + 10;
            lblLabel = new Label { Text = "Label", Top = y, Left = 20, Width = 100 };
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

            Label lblSearch = new Label { Text = "Search", Top = y, Left = this.Width - 280, Width = 60 };
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
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvEntries.Width = this.ClientSize.Width - 40;
            dgvEntries.Height = this.ClientSize.Height - dgvEntries.Top - 40;


            dgvEntries.CellClick += DgvEntries_CellClick;
            dgvEntries.CellFormatting += DgvEntries_CellFormatting;
            dgvEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEntries.MultiSelect = false;
            dgvEntries.AllowUserToAddRows = false;

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem copyPwdItem = new ToolStripMenuItem("Copy Password");
            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete Entry");

            copyPwdItem.Click += (s, e) => CopySelectedPassword();
            deleteItem.Click += (s, e) => DeleteSelectedEntry();

            contextMenu.Items.AddRange(new ToolStripItem[] { copyPwdItem, deleteItem });
            dgvEntries.ContextMenuStrip = contextMenu;

            var btnReveal = new DataGridViewButtonColumn
            {
                Name = "RevealBtn",
                HeaderText = "",
                Text = "👁",
                UseColumnTextForButtonValue = true,
                Width = 50
            };
            btnReveal.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvEntries.Columns.Add(btnReveal);

            var btnEdit = new DataGridViewButtonColumn
            {
                Name = "EditBtn",
                HeaderText = "",
                Text = "✏️",
                UseColumnTextForButtonValue = true,
                Width = 50
            };
            btnEdit.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgvEntries.Columns.Add(btnEdit);

            var btnDelete = new DataGridViewButtonColumn
            {
                Name = "DeleteBtn",
                HeaderText = "",
                Text = "🗑",
                UseColumnTextForButtonValue = true,
                Width = 50
            };
            btnDelete.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
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

        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            if (_idleService.IsExpired())
            {
                _inactivityTimer.Stop();
                Logger.Info($"Vault locked due to inactivity by user='{_username}'");

                foreach (Form openForm in Application.OpenForms)
                {
                    if (openForm != this)
                        openForm.Invoke((MethodInvoker)(() => openForm.Close()));
                }
                LockVault();
            }
        }

        private void BuildPasswordsDictionary(IEnumerable<VaultEntry> entries)
        {
            _realPasswords.Clear();
            int i = 0;
            foreach (var entry in entries)
                _realPasswords[i++] = entry.Password;
        }

        private void ImportVault_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Cryptig Vault (*.mistig)|*.mistig",
                Title = "Select a Vault to Import"
            };

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            string importPath = openDialog.FileName;

            using Form passwordPrompt = new Form
            {
                Width = 300,
                Height = 150,
                Text = "Enter Vault Password",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox txtPwd = new TextBox { Left = 20, Top = 20, Width = 240, UseSystemPasswordChar = true };
            Button btnOk = new Button { Text = "OK", Left = 20, Top = txtPwd.Bottom + 10, Width = 240 };
            btnOk.Click += (s, ev) => passwordPrompt.DialogResult = DialogResult.OK;

            passwordPrompt.Controls.AddRange(new Control[] { txtPwd, btnOk });
            passwordPrompt.AcceptButton = btnOk;

            if (passwordPrompt.ShowDialog() != DialogResult.OK)
                return;

            string enteredPwd = txtPwd.Text;

            try
            {
                var importedVault = MistigVault.Load(importPath, enteredPwd);

                foreach (var entry in importedVault.Data.Entries)
                {
                    _vault?.AddEntry(entry);
                    Logger.Info($"Imported entry: Label='{entry.Label}' into current session.");
                }

                dgvEntries.DataSource = null;
                dgvEntries.DataSource = _vault?.Data.Entries;
                BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);

                MessageBox.Show(
                    $"Imported and merged {importedVault.Data.Entries.Count} entries into your current vault.\n" +
                    "Remember to click 'Save Vault' if you want to keep them.",
                    "Import Successful"
                );

                Logger.Info($"Vault imported and merged from '{importPath}' into current session.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed import from '{importPath}': {ex.Message}");
                MessageBox.Show("Invalid password or corrupted vault.", "Import Failed");
            }
        }

        private void ExportVault_Click(object sender, EventArgs e)
        {
            if (_vault == null)
            {
                MessageBox.Show("No vault loaded.");
                return;
            }

            using SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Cryptig Vault (*.mistig)|*.mistig",
                FileName = $"vault_{_username}_export.mistig"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string currentVaultPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Cryptig", "vaults", $"vault_{_username}.mistig"
                    );

                    File.Copy(currentVaultPath, saveDialog.FileName, true);
                    Logger.Info($"Vault manually exported by user='{_username}' to '{saveDialog.FileName}'");
                    MessageBox.Show("Vault exported successfully.");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Export failed for user='{_username}': {ex.Message}");
                    MessageBox.Show("Export failed: " + ex.Message);
                }
            }
        }

        private void CreateFileVault_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "File Vault (*.misf)|*.misf",
                Title = "Create File Vault"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string path = dlg.FileName;

            if (string.IsNullOrWhiteSpace(path))
                return;

            string password = PromptForPassword();
            if (string.IsNullOrEmpty(password))
                return;

            try
            {
                var vault = FileVault.CreateNew(path, password);
                using var fvForm = new FileVaultForm(vault, _username);
                fvForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create vault: " + ex.Message);
            }
        }

        private void OpenFileVault_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "File Vault (*.misf)|*.misf",
                Title = "Open File Vault"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string password = PromptForPassword();
            if (string.IsNullOrEmpty(password))
                return;

            try
            {
                var vault = FileVault.Load(dlg.FileName, password);
                using var fvForm = new FileVaultForm(vault, _username);
                fvForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open vault: " + ex.Message);
            }
        }

        private static string PromptForPassword()
        {
            using Form prompt = new Form
            {
                Width = 300,
                Height = 150,
                Text = "Enter Vault Password",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox txtPwd = new TextBox { Left = 20, Top = 20, Width = 240, UseSystemPasswordChar = true };
            Button btnOk = new Button { Text = "OK", Left = 20, Top = txtPwd.Bottom + 10, Width = 240 };
            btnOk.Click += (s, e) => prompt.DialogResult = DialogResult.OK;

            prompt.Controls.AddRange(new Control[] { txtPwd, btnOk });
            prompt.AcceptButton = btnOk;

            return prompt.ShowDialog() == DialogResult.OK ? txtPwd.Text : string.Empty;
        }

        private void LoadVault()
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cryptig", "vaults", $"vault_{_username}.mistig"
            );

            try
            {
                if (!File.Exists(path))
                    throw new Exception("This user does not exist.");

                _vault = MistigVault.Load(path, _password);
                dgvEntries.DataSource = _vault?.Data.Entries;
                BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);
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
            BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);

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

            string rawPwd = txtPassword.Text;
            bool isMasked = rawPwd.All(c => c == '•');

            if (isMasked && _vault.Data.Entries.Count >= 0 && _realPasswords.TryGetValue(_vault.Data.Entries.Count, out var recovered))
            {
                rawPwd = recovered;
            }

            var entry = new VaultEntry
            {
                Label = txtLabel.Text,
                Username = txtUsername.Text,
                Password = rawPwd,
                Notes = txtNotes.Text
            };

            _vault.AddEntry(entry);
            Logger.Info($"Entry added: Label='{entry.Label}' for user='{_username}'");

            dgvEntries.DataSource = null;
            dgvEntries.DataSource = _vault.Data.Entries;
            BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);
        }

        private void BtnSaveVault_Click(object sender, EventArgs e)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cryptig", "vaults", $"vault_{_username}.mistig"
            );

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
                if (_revealedRows.Contains(e.RowIndex))
                    _revealedRows.Remove(e.RowIndex);
                else
                    _revealedRows.Add(e.RowIndex);

                dgvEntries.InvalidateRow(e.RowIndex);
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
                BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);
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
                        BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);
                    }
                }
            }
        }

        private void CopySelectedPassword()
        {
            if (dgvEntries.SelectedRows.Count > 0)
            {
                int index = dgvEntries.SelectedRows[0].Index;
                if (_realPasswords.TryGetValue(index, out string realPwd))
                {
                    Clipboard.SetText(realPwd);
                    Logger.Info($"Password copied from entry index {index}.");
                }
            }
        }

        private void DeleteSelectedEntry()
        {
            if (_vault == null || dgvEntries.SelectedRows.Count == 0)
                return;

            int index = dgvEntries.SelectedRows[0].Index;
            if (index >= 0 && index < _vault.Data.Entries.Count)
            {
                var confirm = MessageBox.Show("Delete this entry?", "Confirm", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    Logger.Info($"Entry deleted (context menu): Label='{_vault.Data.Entries[index].Label}'");
                    _vault.Data.Entries.RemoveAt(index);
                    _realPasswords.Remove(index);

                    dgvEntries.DataSource = null;
                    dgvEntries.DataSource = _vault.Data.Entries;
                    BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);
                }
            }
        }

        private void DgvEntries_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvEntries.Columns[e.ColumnIndex].Name != "Password") return;
            if (!_realPasswords.TryGetValue(e.RowIndex, out var realPwd)) return;

            if (!_revealedRows.Contains(e.RowIndex))
            {
                e.Value = new string('•', Math.Min(10, realPwd.Length));
                e.FormattingApplied = true;
            }
        }

        private void LockVault()
        {
            Logger.Info($"Vault locked manually by user='{_username}'");

            Hide();
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Cryptig", "vaults", $"vault_{login.EnteredUsername}.mistig"
                    );

                    try
                    {
                        var vault = MistigVault.Load(path, login.EnteredPassword);
                        _vault = vault;
                        dgvEntries.DataSource = _vault?.Data.Entries;
                        BuildPasswordsDictionary((IEnumerable<VaultEntry>)dgvEntries.DataSource);

                        Logger.Info($"Vault re-unlocked by user='{login.EnteredUsername}'");
                        _revealedRows.Clear();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Re-login failed: {ex.Message}");
                        MessageBox.Show("Failed to unlock vault: " + ex.Message);
                        Application.Exit();
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
            Show();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.L)
            {
                Logger.Info("Vault manually locked via Ctrl+L shortcut.");
                LockVault();
                e.Handled = true;
            }
        }
    }
}