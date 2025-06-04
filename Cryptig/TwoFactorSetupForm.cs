using System;
using System.Drawing;
using System.Windows.Forms;
using Cryptig.Core;
using QRCoder;

namespace Cryptig
{
    public class TwoFactorSetupForm : Form
    {
        private readonly string _secret;
        private readonly TextBox _txtCode;
        public bool Confirmed { get; private set; }
        public string Secret => _secret;

        public TwoFactorSetupForm(string secret)
        {
            _secret = secret;
            Text = "Enable Two-Factor";
            Width = 320;
            Height = 480;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            PictureBox pb = new PictureBox { Width = 280, Height = 280, Top = 20, Left = 10 };
            var otpUri = $"otpauth://totp/Cryptig?secret={secret}&issuer=Cryptig";
            using (var gen = new QRCodeGenerator())
            using (var data = gen.CreateQrCode(otpUri, QRCodeGenerator.ECCLevel.Q))
            using (var code = new QRCode(data))
            {
                pb.Image = code.GetGraphic(5);
            }
            pb.SizeMode = PictureBoxSizeMode.StretchImage;

            Label lblSecret = new Label { Text = secret, Top = pb.Bottom + 10, Left = 20, Width = 260 };
            Label lblPrompt = new Label { Text = "Enter code from your app", Top = lblSecret.Bottom + 10, Left = 20, Width = 260 };
            _txtCode = new TextBox { Top = lblPrompt.Bottom + 2, Left = 20, Width = 260 };
            Button btnConfirm = new Button { Text = "Confirm", Top = _txtCode.Bottom + 10, Left = 20, Width = 260 };
            btnConfirm.Click += BtnConfirm_Click;

            Controls.AddRange(new Control[] { pb, lblSecret, lblPrompt, _txtCode, btnConfirm });
            AcceptButton = btnConfirm;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (TwoFactorAuth.VerifyCode(_secret, _txtCode.Text.Trim()))
            {
                Confirmed = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid code. Please try again.");
            }
        }
    }
}
