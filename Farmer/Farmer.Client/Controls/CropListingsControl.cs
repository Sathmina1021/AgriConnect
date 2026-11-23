using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Controls
{
    /// <summary>Create/update/close/delete harvested crop listings and browse the public marketplace.</summary>
    internal class CropListingsControl : UserControl, IRefreshable
    {
        private readonly TextBox _crop = UiHelper.MakeText(120, 10, 190);
        private readonly NumericUpDown _qty = new NumericUpDown { Left = 120, Top = 46, Width = 190, DecimalPlaces = 2, Minimum = 0.01m, Maximum = 10000000m, Value = 1m, ThousandsSeparator = true };
        private readonly ComboBox _unit = new ComboBox { Left = 120, Top = 82, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly NumericUpDown _price = new NumericUpDown { Left = 120, Top = 118, Width = 190, DecimalPlaces = 2, Minimum = 0.01m, Maximum = 100000000m, Value = 100m, ThousandsSeparator = true };
        private readonly DateTimePicker _available = new DateTimePicker { Left = 120, Top = 154, Width = 190, Format = DateTimePickerFormat.Short };
        private readonly TextBox _contact = UiHelper.MakeText(120, 190, 190);
        private readonly TextBox _notes = new TextBox { Left = 120, Top = 226, Width = 190, Height = 70, Multiline = true, ScrollBars = ScrollBars.Vertical };
        private readonly Button _add, _save;
        private readonly RadioButton _mine = new RadioButton { Text = "My listings", Left = 6, Top = 8, Width = 100, Checked = true };
        private readonly RadioButton _market = new RadioButton { Text = "Marketplace (all farmers)", Left = 112, Top = 8, Width = 190 };
        private readonly TextBox _search = UiHelper.MakeText(330, 6, 160);
        private readonly DataGridView _grid = UiHelper.MakeGrid();
        private Button _close, _delete;
        private bool _loading;

        public CropListingsControl()
        {
            _unit.Items.AddRange(new object[] { "kg", "g", "piece", "bunch", "bag", "litre" });
            _unit.SelectedIndex = 0;
            _available.Value = DateTime.Today.AddDays(3);

            var form = new GroupBox { Text = "Listing details", Dock = DockStyle.Fill };
            form.Controls.Add(UiHelper.MakeLabel("Crop name", 10, 10, 105)); form.Controls.Add(_crop);
            form.Controls.Add(UiHelper.MakeLabel("Quantity", 10, 46, 105)); form.Controls.Add(_qty);
            form.Controls.Add(UiHelper.MakeLabel("Unit", 10, 82, 105)); form.Controls.Add(_unit);
            form.Controls.Add(UiHelper.MakeLabel("Price per unit", 10, 118, 105)); form.Controls.Add(_price);
            form.Controls.Add(UiHelper.MakeLabel("Available from", 10, 154, 105)); form.Controls.Add(_available);
            form.Controls.Add(UiHelper.MakeLabel("Contact info", 10, 190, 105)); form.Controls.Add(_contact);
            form.Controls.Add(UiHelper.MakeLabel("Notes", 10, 226, 105)); form.Controls.Add(_notes);
            _add = UiHelper.MakeButton("Add listing", 120, 310, 190, OnAdd, primary: true);
            _save = UiHelper.MakeButton("Save changes to selected", 120, 346, 190, OnSave);
            _save.Enabled = false;
            form.Controls.Add(_add);
            form.Controls.Add(_save);
            form.Controls.Add(UiHelper.MakeButton("Clear form", 120, 382, 190, (s, e) => ClearForm()));
            form.Padding = new Padding(8, 20, 8, 8);

            var modeBar = new Panel { Dock = DockStyle.Fill };
            modeBar.Controls.Add(_mine);
            modeBar.Controls.Add(_market);
            modeBar.Controls.Add(_search);
            modeBar.Controls.Add(UiHelper.MakeButton("Search / Refresh", 500, 3, 140, (s, e) => RefreshData()));
            _mine.CheckedChanged += (s, e) => { if (_mine.Checked) RefreshData(); };

            UiHelper.AddColumn(_grid, "ID", "ListingId", 40);
            UiHelper.AddColumn(_grid, "Crop", "CropName", 130);
            UiHelper.AddColumn(_grid, "Qty", "Quantity", 70, "N2");
            UiHelper.AddColumn(_grid, "Unit", "Unit", 50);
            UiHelper.AddColumn(_grid, "Price/unit", "UnitPrice", 80, "N2");
            UiHelper.AddColumn(_grid, "Available", "AvailabilityDate", 85, "d");
            UiHelper.AddColumn(_grid, "Status", "Status", 65);
            UiHelper.AddColumn(_grid, "Farmer", "FarmerName", 120);
            UiHelper.AddColumn(_grid, "District", "District", 90);
            UiHelper.AddColumn(_grid, "Contact", "ContactInfo", 130);
            UiHelper.AddColumn(_grid, "Notes", "Notes", 220);
            _grid.SelectionChanged += (s, e) => OnSelectionChanged();

            var actions = new Panel { Dock = DockStyle.Fill };
            _close = UiHelper.MakeButton("Close selected listing", 4, 8, 170, OnClose);
            _delete = UiHelper.MakeButton("Delete selected listing", 184, 8, 170, OnDelete);
            actions.Controls.Add(_close);
            actions.Controls.Add(_delete);

            var right = UiHelper.MakeLayout(1, new RowStyle(SizeType.Absolute, 40), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 48));
            right.Controls.Add(modeBar, 0, 0);
            right.Controls.Add(_grid, 0, 1);
            right.Controls.Add(actions, 0, 2);

            var layout = UiHelper.MakeLayout(2, new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Clear();
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 345));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(form, 0, 0);
            layout.Controls.Add(right, 1, 0);
            Controls.Add(layout);
        }

        private bool MarketplaceMode { get { return _market.Checked; } }

        private CropListingDto Selected
        {
            get { return _grid.CurrentRow == null ? null : _grid.CurrentRow.DataBoundItem as CropListingDto; }
        }

        public void RefreshData()
        {
            List<CropListingDto> listings;
            bool ok = MarketplaceMode
                ? ServiceCaller.TryCall(c => c.GetMarketplaceListings(_search.Text.Trim()).ToList(), out listings)
                : ServiceCaller.TryCall(c => c.GetMyCropListings(Session.FarmerId).ToList(), out listings);
            if (!ok) return;

            _loading = true;
            _grid.DataSource = listings;
            _loading = false;
            _close.Enabled = _delete.Enabled = !MarketplaceMode;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            if (_loading) return;
            CropListingDto l = Selected;
            bool editable = l != null && !MarketplaceMode && l.Status == "Active";
            _save.Enabled = editable;
            if (l == null || MarketplaceMode) return;

            _crop.Text = l.CropName;
            _qty.Value = Math.Max(_qty.Minimum, Math.Min(_qty.Maximum, l.Quantity));
            _unit.SelectedItem = _unit.Items.Contains(l.Unit) ? l.Unit : "kg";
            _price.Value = Math.Max(_price.Minimum, Math.Min(_price.Maximum, l.UnitPrice));
            _available.Value = l.AvailabilityDate;
            _contact.Text = l.ContactInfo;
            _notes.Text = l.Notes;
        }

        private CropListingInput ReadForm(int listingId)
        {
            return new CropListingInput
            {
                ListingId = listingId,
                FarmerId = Session.FarmerId,
                CropName = _crop.Text,
                Quantity = _qty.Value,
                Unit = Convert.ToString(_unit.SelectedItem),
                UnitPrice = _price.Value,
                AvailabilityDate = _available.Value.Date,
                ContactInfo = _contact.Text,
                Notes = _notes.Text
            };
        }

        private void ClearForm()
        {
            _crop.Clear(); _contact.Clear(); _notes.Clear();
            _qty.Value = 1; _price.Value = 100; _unit.SelectedIndex = 0;
            _available.Value = DateTime.Today.AddDays(3);
            _grid.ClearSelection();
            _save.Enabled = false;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            CropListingInput input = ReadForm(0);
            CropListingDto created;
            if (!ServiceCaller.TryCall(c => c.CreateCropListing(input), out created)) return;
            UiHelper.ShowInfo("Listing #" + created.ListingId + " (" + created.CropName + ") is now live in the marketplace.");
            _mine.Checked = true;
            ClearForm();
            RefreshData();
        }

        private void OnSave(object sender, EventArgs e)
        {
            CropListingDto selected = Selected;
            if (selected == null) { UiHelper.ShowError("Select one of your listings first."); return; }
            CropListingInput input = ReadForm(selected.ListingId);
            CropListingDto updated;
            if (ServiceCaller.TryCall(c => c.UpdateCropListing(input), out updated))
            {
                UiHelper.ShowInfo("Listing #" + updated.ListingId + " updated.");
                RefreshData();
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            CropListingDto selected = Selected;
            if (selected == null) { UiHelper.ShowError("Select a listing first."); return; }
            if (!UiHelper.Confirm("Close listing '" + selected.CropName + "'? Buyers will no longer see it.")) return;
            CropListingDto closed;
            if (ServiceCaller.TryCall(c => c.CloseCropListing(Session.FarmerId, selected.ListingId), out closed)) RefreshData();
        }

        private void OnDelete(object sender, EventArgs e)
        {
            CropListingDto selected = Selected;
            if (selected == null) { UiHelper.ShowError("Select a listing first."); return; }
            if (!UiHelper.Confirm("Permanently delete listing '" + selected.CropName + "'?")) return;
            if (ServiceCaller.TryCall(c => c.DeleteCropListing(Session.FarmerId, selected.ListingId))) { ClearForm(); RefreshData(); }
        }
    }
}
