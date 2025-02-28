using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextHiveGrok
{
    public class CustomMessageBox : Form
    {
        private Label messageLabel;
        private Button okButton;

        public CustomMessageBox(string message, string title)
        {
            Text = title;
            Size = new Size(400, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            messageLabel = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            Controls.Add(messageLabel);
            Controls.Add(okButton);
        }

        public static void Show(string message, string title)
        {
            using (var form = new CustomMessageBox(message, title))
            {
                form.ShowDialog();
            }
        }
    }
}
