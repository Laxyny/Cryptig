using System.Windows.Forms;
using Cryptig.Core;

namespace Cryptig
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = $"About {AboutService.AppName}";
            this.Width = 450;
            this.Height = 320;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var label = new Label
            {
                Text =
                    $"{AboutService.AppName} — Version {AboutService.Version}\n\n" +
                    $"Author: {AboutService.Author}\n" +
                    $"License: {AboutService.License}\n\n" +
                    $"{AboutService.Description}\n\n",
                AutoSize = false,
                Width = this.ClientSize.Width - 40,
                Height = this.ClientSize.Height - 40,
                Left = 20,
                Top = 20,
                TextAlign = System.Drawing.ContentAlignment.TopLeft
            };

            var link = new LinkLabel
            {
                Text = AboutService.GitHub,
                AutoSize = true,
                Left = 20,
                Top = label.Bottom
            };
            link.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AboutService.GitHub,
                UseShellExecute = true
            });

            Controls.Add(label);
            Controls.Add(link);
        }
    }
}