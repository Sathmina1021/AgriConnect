using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Controls
{
    /// <summary>Summary tiles + quick views of recent supply requests and crop listings.</summary>
    internal class DashboardControl : UserControl, IRefreshable
    {
        private Label _total, _pending, _approved, _confirmed, _listings, _value;
        private readonly DataGridView _requests = UiHelper.MakeGrid();
        private readonly DataGridView _crops = UiHelper.MakeGrid();

        public DashboardControl()
        {
            var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            cards.Controls.Add(UiHelper.MakeCard("Total supply requests", Color.FromArgb(25, 118, 210), out _total));
            cards.Controls.Add(UiHelper.MakeCard("Pending officer approval", Color.FromArgb(245, 124, 0), out _pending));
            cards.Controls.Add(UiHelper.MakeCard("Approved - ready to confirm", Color.FromArgb(56, 142, 60), out _approved));
            cards.Controls.Add(UiHelper.MakeCard("Confirmed orders", Color.FromArgb(106, 27, 154), out _confirmed));
            cards.Controls.Add(UiHelper.MakeCard("Active crop listings", Color.FromArgb(0, 121, 107), out _listings));
            cards.Controls.Add(UiHelper.MakeCard("Total order value", Color.FromArgb(69, 90, 100), out _value));

            UiHelper.AddColumn(_requests, "ID", "RequestId", 45);
            UiHelper.AddColumn(_requests, "Item", "ItemName", 190);
            UiHelper.AddColumn(_requests, "Qty", "Quantity", 60);
            UiHelper.AddColumn(_requests, "Preferred", "PreferredDate", 90, "d");
            UiHelper.AddColumn(_requests, "Status", "Status", 90);
            _requests.CellFormatting += StatusColors.FormatRequestStatusCell;

            UiHelper.AddColumn(_crops, "Crop", "CropName", 150);
            UiHelper.AddColumn(_crops, "Qty", "Quantity", 70, "N0");
            UiHelper.AddColumn(_crops, "Unit", "Unit", 55);
            UiHelper.AddColumn(_crops, "Price", "UnitPrice", 80, "N2");
            UiHelper.AddColumn(_crops, "Status", "Status", 80);

            var layout = UiHelper.MakeLayout(2,
                new RowStyle(SizeType.Absolute, 110), new RowStyle(SizeType.Absolute, 28), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 40));
            layout.Controls.Add(cards, 0, 0);
            layout.SetColumnSpan(cards, 2);
            layout.Controls.Add(new Label { Text = "Recent supply requests", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 1);
            layout.Controls.Add(new Label { Text = "Recent crop listings", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Dock = DockStyle.Fill }, 1, 1);
            layout.Controls.Add(_requests, 0, 2);
            layout.Controls.Add(_crops, 1, 2);
            Button refresh = UiHelper.MakeButton("Refresh", 6, 6, 100, (s, e) => RefreshData());
            var bottom = new Panel { Dock = DockStyle.Fill };
            bottom.Controls.Add(refresh);
            layout.Controls.Add(bottom, 0, 3);
            Controls.Add(layout);
        }

        public void RefreshData()
        {
            DashboardSummary s;
            if (!ServiceCaller.TryCall(c => c.GetDashboard(Session.FarmerId), out s)) return;

            _total.Text = s.TotalSupplyRequests.ToString();
            _pending.Text = s.PendingApprovals.ToString();
            _approved.Text = s.ApprovedReadyToConfirm.ToString();
            _confirmed.Text = s.ConfirmedOrders.ToString();
            _listings.Text = s.ActiveCropListings.ToString();
            _value.Text = s.TotalOrderValue.ToString("N0");
            _requests.DataSource = s.RecentRequests.ToList();
            _crops.DataSource = s.RecentListings.ToList();
        }
    }
}
