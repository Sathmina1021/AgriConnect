using System;
using System.Windows.Forms;
using Farmer.Client.Forms;

namespace Farmer.Client
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => UiHelper.ShowError("Unexpected error: " + e.Exception.Message);

            // Login -> main window -> (logout) -> login ... until the user closes a window.
            while (true)
            {
                Session.CurrentFarmer = null;
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                using (var main = new MainForm())
                {
                    main.ShowDialog();
                    if (!main.LoggedOut) return;
                }
            }
        }
    }
}
