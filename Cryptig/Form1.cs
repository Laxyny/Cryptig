using System;
using System.IO;
using System.Windows.Forms;
using Cryptig.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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

            lblNotes = new Label { Text = "Notes", Top = txtPassword.Bottom + 10, Left = 20, Width = 100 };
            txtNotes = new TextBox { Top = lblNotes.Bottom + 2, Left = 20, Width = 200, Height = 60, Multiline = true };

            btnAddEntry = new Button { Text = "Add Entry", Top = txtNotes.Bottom + 10, Left = 20, Width = 100 };
            btnAddEntry.Click += BtnAddEntry_Click;

            btnSaveVault = new Button { Text = "Save Vault", Top = txtNotes.Bottom + 10, Left = 140, Width = 100 };
            btnSaveVault.Click += BtnSaveVault_Click;

            dgvEntries = new DataGridView
            {
                Top = btnAddEntry.Bottom + 20,
                Left = 20,
                Width = 740,
                Height = 300,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

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
            }
            catch
            {
                MessageBox.Show("Access denied: invalid username or password.");
                Environment.Exit(1);
            }
        }

        private void BtnAddEntry_Click(object sender, EventArgs e)
        {
            if (_vault == null) return;

            var entry = new VaultEntry
            {
                Label = txtLabel.Text,
                Username = txtUsername.Text,
                Password = txtPassword.Text,
                Notes = txtNotes.Text
            };

            _vault.AddEntry(entry);

            dgvEntries.DataSource = null;
            dgvEntries.DataSource = _vault.Data.Entries;
        }

        private void BtnSaveVault_Click(object sender, EventArgs e)
        {
            _vault?.Save();
            MessageBox.Show("Vault saved successfully.");
        }
    }
}