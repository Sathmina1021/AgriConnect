using System;
using System.Drawing;
using System.Windows.Forms;
using Farmer.Client.Controls;

namespace Farmer.Client.Forms
{
    /// <summary>Main window: header + one tab per Farmer feature. Each tab is a reusable UserControl.</summary>
    public class MainForm : Form
    {
        private readonly TabControl _tabs;

        public bool LoggedOut { get; private set; }

        public MainForm()
        {
            Text = "AgriConnect - Farmer Portal";
            Size = new Size(1180, 760);
            MinimumSize = new Size(1000, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = UiHelper.BaseFont;

            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UiHelper.Primary };
            header.Controls.Add(new Label { Text = "AgriConnect  |  Farmer", ForeColor = Color.White, Font = new Font("Segoe UI", 15f, FontStyle.Bold), Left = 16, Top = 12, Width = 320, Height = 32 });
            var welcome = new Label { Text = "Signed in as " + Session.CurrentFarmer.FullName + " (" + Session.CurrentFarmer.District + ")", ForeColor = Color.White, Left = 420, Top = 18, Width = 520, Height = 24, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var logout = UiHelper.MakeButton("Logout", 0, 13, 90, (s, e) => { LoggedOut = true; Close(); });
            logout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.Add(welcome);
            header.Controls.Add(logout);
            header.Resize += (s, e) => { logout.Left = header.Width - 106; welcome.Left = header.Width - 106 - 530; };

            _tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
            AddTab("Dashboard", new DashboardControl());
            AddTab("Supply Requests", new SupplyRequestsControl());
            AddTab("Quotations & Orders", new QuotationsControl());
            AddTab("Crop Listings", new CropListingsControl());
            _tabs.SelectedIndexChanged += (s, e) => RefreshSelectedTab();

            Controls.Add(_tabs);     // Fill first, then the Top-docked header (see WinForms docking order)
            Controls.Add(header);

            Shown += (s, e) => { logout.Left = header.Width - 106; welcome.Left = header.Width - 106 - 530; RefreshSelectedTab(); };
        }

        private void AddTab(string title, Control content)
        {
            content.Dock = DockStyle.Fill;
            var page = new TabPage(title);
            page.Controls.Add(content);
            _tabs.TabPages.Add(page);
        }

        private void RefreshSelectedTab()
        {
            if (_tabs.SelectedTab == null || _tabs.SelectedTab.Controls.Count == 0) return;
            var refreshable = _tabs.SelectedTab.Controls[0] as IRefreshable;
            if (refreshable != null) refreshable.RefreshData();
        }
    }
}
