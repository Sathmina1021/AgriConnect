using System;
using System.Drawing;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Forms
{
    /// <summary>New farmer registration. Basic checks happen here; the service re-validates everything.</summary>
    public class RegisterForm : Form
    {
        private readonly TextBox _name, _email, _phone, _district, _address, _password, _confirm;

        public string RegisteredEmail { get; private set; }

        public RegisterForm()
        {
            Text = "AgriConnect - Farmer Registration";
            ClientSize = new Size(440, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = UiHelper.BaseFont;

            int y = 20;
            _name = AddRow("Full name", ref y, false);
            _email = AddRow("Email", ref y, false);
            _phone = AddRow("Phone", ref y, false);
            _district = AddRow("District", ref y, false);
            _address = AddRow("Address (optional)", ref y, false);
            _password = AddRow("Password", ref y, true);
            _confirm = AddRow("Confirm password", ref y, true);

            Controls.Add(new Label { Text = "Password: min 8 characters with letters and digits.", ForeColor = Color.Gray, Left = 160, Top = y - 4, Width = 270 });
            Button ok = UiHelper.MakeButton("Register", 160, y + 25, 120, OnRegister, primary: true);
            Controls.Add(ok);
            Controls.Add(UiHelper.MakeButton("Cancel", 290, y + 25, 120, (s, e) => DialogResult = DialogResult.Cancel));
            AcceptButton = ok;
        }

        private TextBox AddRow(string caption, ref int y, bool password)
        {
            Controls.Add(UiHelper.MakeLabel(caption, 20, y, 135));
            TextBox box = UiHelper.MakeText(160, y, 250, password);
            Controls.Add(box);
            y += 42;
            return box;
        }

        private void OnRegister(object sender, EventArgs e)
        {
            if (_password.Text != _confirm.Text)
            {
                UiHelper.ShowError("Passwords do not match.");
                return;
            }

            var registration = new FarmerRegistration
            {
                FullName = _name.Text, Email = _email.Text, Phone = _phone.Text,
                District = _district.Text, Address = _address.Text, Password = _password.Text
            };

            FarmerDto created;
            if (!ServiceCaller.TryCall(c => c.RegisterFarmer(registration), out created)) return;

            RegisteredEmail = created.Email;
            UiHelper.ShowInfo("Account created for " + created.FullName + ". You can now sign in.");
            DialogResult = DialogResult.OK;
        }
    }
}
