using System;
using System.Windows.Forms;
using Cryptig.Core;

namespace Cryptig
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            while (true)
            {
                LoginForm login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK)
                    break;

                string path = $"vault_{login.EnteredUsername}.mistig";

                try
                {
                    if (!File.Exists(path))
                        throw new Exception("Vault not found.");

                    var vault = MistigVault.Load(path, login.EnteredPassword);
                    if (vault == null)
                        throw new Exception("Vault is null.");

                    Application.Run(new Form1(vault, login.EnteredUsername, login.EnteredPassword));
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid username or password.\n\n" + ex.Message);
                }
            }

        }
    }
}
