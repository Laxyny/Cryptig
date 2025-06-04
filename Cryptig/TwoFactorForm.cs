using System.Windows.Forms;

namespace Cryptig
{
    public class TwoFactorForm : Form
    {
        private TextBox txtCode;
        private Button btnVerify;
        public string EnteredCode { get; private set; } = string.Empty;

        public TwoFactorForm()
        {
            Text = "Two-Factor Authentication";
            Width = 300;
            Height = 160;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label lbl = new Label { Text = "Authentication Code", Top = 20, Left = 20, Width = 240 };
            txtCode = new TextBox { Top = lbl.Bottom + 2, Left = 20, Width = 240 };
            btnVerify = new Button { Text = "Verify", Top = txtCode.Bottom + 10, Left = 20, Width = 240 };
            btnVerify.Click += (s, e) => { EnteredCode = txtCode.Text.Trim(); DialogResult = DialogResult.OK; Close(); };

            Controls.AddRange(new Control[] { lbl, txtCode, btnVerify });
            AcceptButton = btnVerify;
        }
    }
}
