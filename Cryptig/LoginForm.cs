using Cryptig.Core;

namespace Cryptig
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnCreate;

        public string EnteredUsername { get; private set; } = string.Empty;
        public string EnteredPassword { get; private set; } = string.Empty;

        private static readonly string LastUserFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cryptig", "lastuser.txt"
        );

        public LoginForm()
        {
            Text = "Login";
            Width = 300;
            Height = 260;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            Label lblUser = new Label { Text = "Username", Top = 20, Left = 20, Width = 240 };
            txtUsername = new TextBox { Top = lblUser.Bottom + 2, Left = 20, Width = 240 };

            Label lblPass = new Label { Text = "Password", Top = txtUsername.Bottom + 10, Left = 20, Width = 240 };
            txtPassword = new TextBox { Top = lblPass.Bottom + 2, Left = 20, Width = 240, UseSystemPasswordChar = true };

            btnLogin = new Button { Text = "Unlock", Top = txtPassword.Bottom + 10, Left = 20, Width = 240 };
            btnLogin.Click += BtnLogin_Click;

            btnCreate = new Button { Text = "Create Account", Top = btnLogin.Bottom + 10, Left = 20, Width = 240 };
            btnCreate.Click += BtnCreate_Click;

            Controls.AddRange(new Control[] { lblUser, txtUsername, lblPass, txtPassword, btnLogin, btnCreate });

            AcceptButton = btnLogin;
            LoadLastUsername();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            EnteredUsername = txtUsername.Text.Trim();
            EnteredPassword = txtPassword.Text;

            SaveLastUsername(EnteredUsername);

            if (string.IsNullOrEmpty(EnteredUsername) || string.IsNullOrEmpty(EnteredPassword))
            {
                MessageBox.Show("Both fields are required.");
                return;
            }

            Logger.Info($"Login attempted for user='{EnteredUsername}'");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            EnteredUsername = txtUsername.Text.Trim();
            EnteredPassword = txtPassword.Text;

            if (string.IsNullOrEmpty(EnteredUsername) || string.IsNullOrEmpty(EnteredPassword))
            {
                MessageBox.Show("Both fields are required.");
                return;
            }

            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Cryptig", "vaults", $"vault_{EnteredUsername}.mistig"
            );

            // Create the directory if it doesn't exist
            if (!Directory.Exists(Path.GetDirectoryName(path)!))
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            if (File.Exists(path))
            {
                MessageBox.Show("This user already exists.");
                return;
            }

            try
            {
                var vault = Cryptig.Core.MistigVault.CreateNew(path, EnteredPassword);
                vault.Save();

                MessageBox.Show("Account created successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Vault creation failed for user='{EnteredUsername}': {ex.Message}");
                MessageBox.Show("Error: " + ex.Message);
                return;
            }

            Logger.Info($"New vault created for user='{EnteredUsername}'");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void LoadLastUsername()
        {
            if (File.Exists(LastUserFile))
            {
                try
                {
                    string lastUser = File.ReadAllText(LastUserFile).Trim();
                    if (!string.IsNullOrWhiteSpace(lastUser))
                    { 
                        txtUsername.Text = lastUser;
                        ActiveControl = txtPassword;
                    }
                }
                catch {}
            }
        }

        private void SaveLastUsername(string username)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastUserFile)!);
                File.WriteAllText(LastUserFile, username);
            }
            catch {}
        }

    }
}
