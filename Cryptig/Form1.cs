using Cryptig.Core;
using System.Diagnostics;

namespace Cryptig
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void btnCreateVault_Click(object sender, EventArgs e)
        {
            string path = "testvault.mistig";
            string password = "MyStrongMasterPassword";

            MistigVault.CreateNew(path, password);

            MessageBox.Show("Vault created successfully.");
            Process.Start("explorer.exe", Environment.CurrentDirectory);
        }

    }
}
