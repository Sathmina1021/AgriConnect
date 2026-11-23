using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Controls
{
    /// <summary>Submit a new supply request and track the status of existing ones.</summary>
    internal class SupplyRequestsControl : UserControl, IRefreshable
    {
        private readonly TextBox _item = UiHelper.MakeText(110, 28, 260);
        private readonly NumericUpDown _quantity = new NumericUpDown { Left = 500, Top = 28, Width = 110, Minimum = 1, Maximum = 1000000, Value = 1 };
        private readonly TextBox _location = UiHelper.MakeText(110, 64, 260);
        private readonly DateTimePicker _preferred = new DateTimePicker { Left = 500, Top = 64, Width = 140, Format = DateTimePickerFormat.Short };
        private readonly DataGridView _grid = UiHelper.MakeGrid();

        public SupplyRequestsControl()
        {
            _preferred.MinDate = DateTime.Today;
            _preferred.Value = DateTime.Today.AddDays(7);

            var form = new GroupBox { Text = "New supply request", Dock = DockStyle.Fill };
            form.Controls.Add(UiHelper.MakeLabel("Item name", 12, 28, 95));
            form.Controls.Add(_item);
            form.Controls.Add(UiHelper.MakeLabel("Quantity", 410, 28, 85));
            form.Controls.Add(_quantity);
            form.Controls.Add(UiHelper.MakeLabel("Delivery location", 12, 64, 95));
            form.Controls.Add(_location);
            form.Controls.Add(UiHelper.MakeLabel("Preferred date", 410, 64, 85));
            form.Controls.Add(_preferred);
            form.Controls.Add(UiHelper.MakeButton("Submit request", 670, 40, 150, OnSubmit, primary: true));

            UiHelper.AddColumn(_grid, "ID", "RequestId", 45);
            UiHelper.AddColumn(_grid, "Item", "ItemName", 200);
            UiHelper.AddColumn(_grid, "Qty", "Quantity", 60);
            UiHelper.AddColumn(_grid, "Delivery location", "DeliveryLocation", 170);
            UiHelper.AddColumn(_grid, "Preferred date", "PreferredDate", 100, "d");
            UiHelper.AddColumn(_grid, "Status", "Status", 90);
            UiHelper.AddColumn(_grid, "Officer remarks", "OfficerRemarks", 260);
            UiHelper.AddColumn(_grid, "Supplier notified", "SupplierNotified", 100);
            UiHelper.AddColumn(_grid, "Officer notified", "OfficerNotified", 100);
            _grid.CellFormatting += StatusColors.FormatRequestStatusCell;

            var buttons = new Panel { Dock = DockStyle.Fill };
            buttons.Controls.Add(UiHelper.MakeButton("Refresh list", 4, 8, 110, (s, e) => RefreshData()));
            buttons.Controls.Add(UiHelper.MakeButton("Check approval status", 124, 8, 170, OnCheckStatus));
            buttons.Controls.Add(UiHelper.MakeButton("Cancel request", 304, 8, 130, OnCancel));
            buttons.Controls.Add(new Label { Text = "Approved requests can be confirmed in the 'Quotations & Orders' tab.", ForeColor = Color.Gray, Left = 450, Top = 14, Width = 450 });

            var layout = UiHelper.MakeLayout(1, new RowStyle(SizeType.Absolute, 110), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 48));
            layout.Controls.Add(form, 0, 0);
            layout.Controls.Add(_grid, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            Controls.Add(layout);
        }

        private SupplyRequestDto Selected
        {
            get { return _grid.CurrentRow == null ? null : _grid.CurrentRow.DataBoundItem as SupplyRequestDto; }
        }

        public void RefreshData()
        {
            System.Collections.Generic.List<SupplyRequestDto> requests;
            if (!ServiceCaller.TryCall(c => c.GetSupplyRequests(Session.FarmerId, null).ToList(), out requests)) return;
            _grid.DataSource = requests;
        }

        private void OnSubmit(object sender, EventArgs e)
        {
            var input = new SupplyRequestInput
            {
                FarmerId = Session.FarmerId,
                ItemName = _item.Text,
                Quantity = (int)_quantity.Value,
                DeliveryLocation = _location.Text,
                PreferredDate = _preferred.Value.Date
            };

            SupplyRequestDto created;
            if (!ServiceCaller.TryCall(c => c.CreateSupplyRequest(input), out created)) return;

            UiHelper.ShowInfo("Supply request #" + created.RequestId + " submitted.\n" +
                              "Supplier notified: " + (created.SupplierNotified ? "yes" : "not yet (will retry)") + "\n" +
                              "Officer notified: " + (created.OfficerNotified ? "yes" : "not yet (will retry)"));
            _item.Clear();
            _location.Clear();
            _quantity.Value = 1;
            RefreshData();
        }

        private void OnCheckStatus(object sender, EventArgs e)
        {
            SupplyRequestDto selected = Selected;
            if (selected == null) { UiHelper.ShowError("Select a request first."); return; }

            SupplyRequestDto updated;
            if (!ServiceCaller.TryCall(c => c.RefreshRequestStatus(Session.FarmerId, selected.RequestId), out updated)) return;

            RefreshData();
            string remarks = string.IsNullOrEmpty(updated.OfficerRemarks) ? "" : "\nOfficer remarks: " + updated.OfficerRemarks;
            UiHelper.ShowInfo("Request #" + updated.RequestId + " is now: " + updated.Status + remarks);
        }

        private void OnCancel(object sender, EventArgs e)
        {
            SupplyRequestDto selected = Selected;
            if (selected == null) { UiHelper.ShowError("Select a request first."); return; }
            if (!UiHelper.Confirm("Cancel supply request #" + selected.RequestId + " (" + selected.ItemName + ")?")) return;

            SupplyRequestDto updated;
            if (ServiceCaller.TryCall(c => c.CancelSupplyRequest(Session.FarmerId, selected.RequestId), out updated))
                RefreshData();
        }
    }
}
