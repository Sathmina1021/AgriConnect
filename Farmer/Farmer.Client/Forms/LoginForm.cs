using System;
using System.Drawing;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Forms
{
    /// <summary>Farmer login screen (UI built in code to keep the project easy to review and merge in Git).</summary>
    public class LoginForm : Form
    {
        private readonly TextBox _txtEmail;
        private readonly TextBox _txtPassword;

        public LoginForm()
        {
            Text = "AgriConnect - Farmer Login";
            ClientSize = new Size(420, 330);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = UiHelper.BaseFont;

            Controls.Add(new Label { Text = "AgriConnect", Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = UiHelper.Primary, Left = 30, Top = 20, Width = 360, Height = 40 });
            Controls.Add(new Label { Text = "Farmer portal - please sign in", Left = 32, Top = 62, Width = 340 });

            Controls.Add(UiHelper.MakeLabel("Email", 30, 105, 100));
            _txtEmail = UiHelper.MakeText(30, 130, 360);
            Controls.Add(_txtEmail);

            Controls.Add(UiHelper.MakeLabel("Password", 30, 165, 100));
            _txtPassword = UiHelper.MakeText(30, 190, 360, password: true);
            Controls.Add(_txtPassword);

            Button btnLogin = UiHelper.MakeButton("Login", 30, 235, 170, OnLogin, primary: true);
            Button btnRegister = UiHelper.MakeButton("Create account", 220, 235, 170, OnRegister);
            Controls.Add(btnLogin);
            Controls.Add(btnRegister);
            AcceptButton = btnLogin;

            Controls.Add(new Label
            {
                Text = "Demo: nimal.perera@agriconnect.lk / Farmer@123",
                ForeColor = Color.Gray, Left = 30, Top = 285, Width = 360
            });
        }

        private void OnLogin(object sender, EventArgs e)
        {
            string email = _txtEmail.Text.Trim();
            string password = _txtPassword.Text;
            if (email.Length == 0 || password.Length == 0)
            {
                UiHelper.ShowError("Please enter your email and password.");
                return;
            }

            LoginResult result;
            if (!ServiceCaller.TryCall(c => c.Login(new LoginCredentials { Email = email, Password = password }), out result))
                return;

            if (!result.Success)
            {
                UiHelper.ShowError(result.Message);
                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }

            Session.CurrentFarmer = result.Profile;
            DialogResult = DialogResult.OK;
        }

        private void OnRegister(object sender, EventArgs e)
        {
            using (var form = new RegisterForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _txtEmail.Text = form.RegisteredEmail;
                    _txtPassword.Clear();
                    _txtPassword.Focus();
                }
            }
        }
    }
}
