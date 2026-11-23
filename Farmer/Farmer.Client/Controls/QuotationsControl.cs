using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client.Controls
{
    /// <summary>Compare supplier quotations for an officer-approved request, confirm the order, and list confirmed orders.</summary>
    internal class QuotationsControl : UserControl, IRefreshable
    {
        /// <summary>Combo-box item wrapper.</summary>
        private class RequestChoice
        {
            public SupplyRequestDto Request;
            public override string ToString()
            {
                return "#" + Request.RequestId + "  " + Request.ItemName + "  x" + Request.Quantity + "  (needed by " + Request.PreferredDate.ToString("d") + ")";
            }
        }

        private readonly ComboBox _requests = new ComboBox { Left = 150, Top = 14, Width = 520, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label _info = new Label { Left = 150, Top = 48, Width = 800, Height = 22, ForeColor = Color.DimGray };
        private readonly DataGridView _quotes = UiHelper.MakeGrid();
        private readonly DataGridView _orders = UiHelper.MakeGrid();
        private SupplyRequestDto _loadedRequest;

        public QuotationsControl()
        {
            var top = new Panel { Dock = DockStyle.Fill };
            top.Controls.Add(UiHelper.MakeLabel("Approved request", 12, 14, 135));
            top.Controls.Add(_requests);
            top.Controls.Add(UiHelper.MakeButton("Load quotations", 690, 11, 150, OnLoadQuotations, primary: true));
            top.Controls.Add(_info);

            UiHelper.AddColumn(_quotes, "Quotation", "QuotationId", 75);
            UiHelper.AddColumn(_quotes, "Supplier", "SupplierName", 170);
            UiHelper.AddColumn(_quotes, "Unit price", "UnitPrice", 90, "N2");
            UiHelper.AddColumn(_quotes, "Total price", "TotalPrice", 110, "N2");
            UiHelper.AddColumn(_quotes, "Stock", "StockQuantity", 70, "N0");
            UiHelper.AddColumn(_quotes, "Delivery (days)", "DeliveryDays", 100);
            UiHelper.AddColumn(_quotes, "Est. delivery", "EstimatedDeliveryDate", 100, "d");
            UiHelper.AddColumn(_quotes, "Stock OK", "MeetsStock", 70);
            UiHelper.AddColumn(_quotes, "On time", "MeetsPreferredDate", 70);
            UiHelper.AddColumn(_quotes, "Notes", "Notes", 280);
            _quotes.DataBindingComplete += (s, e) => HighlightQuotes();

            var confirmBar = new Panel { Dock = DockStyle.Fill };
            confirmBar.Controls.Add(UiHelper.MakeButton("Confirm order with selected supplier", 4, 6, 280, OnConfirm, primary: true));
            confirmBar.Controls.Add(new Label { Text = "Green = cheapest supplier that can fully supply the request. Grey = insufficient stock.", ForeColor = Color.Gray, Left = 300, Top = 12, Width = 600 });

            UiHelper.AddColumn(_orders, "Order", "OrderId", 55);
            UiHelper.AddColumn(_orders, "Request", "RequestId", 65);
            UiHelper.AddColumn(_orders, "Item", "ItemName", 190);
            UiHelper.AddColumn(_orders, "Supplier", "SupplierName", 160);
            UiHelper.AddColumn(_orders, "Qty", "Quantity", 60);
            UiHelper.AddColumn(_orders, "Unit price", "UnitPrice", 90, "N2");
            UiHelper.AddColumn(_orders, "Total", "TotalPrice", 110, "N2");
            UiHelper.AddColumn(_orders, "Est. delivery", "EstimatedDeliveryDate", 100, "d");
            UiHelper.AddColumn(_orders, "Order status", "OrderStatus", 100);
            UiHelper.AddColumn(_orders, "Confirmed on", "ConfirmedOn", 130, "g");

            var layout = UiHelper.MakeLayout(1,
                new RowStyle(SizeType.Absolute, 76), new RowStyle(SizeType.Percent, 55), new RowStyle(SizeType.Absolute, 44),
                new RowStyle(SizeType.Absolute, 26), new RowStyle(SizeType.Percent, 45));
            layout.Controls.Add(top, 0, 0);
            layout.Controls.Add(_quotes, 0, 1);
            layout.Controls.Add(confirmBar, 0, 2);
            layout.Controls.Add(new Label { Text = "My confirmed orders", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 3);
            layout.Controls.Add(_orders, 0, 4);
            Controls.Add(layout);
        }

        public void RefreshData()
        {
            List<SupplyRequestDto> approved;
            if (ServiceCaller.TryCall(c => c.GetSupplyRequests(Session.FarmerId, "Approved").ToList(), out approved))
            {
                _requests.Items.Clear();
                foreach (SupplyRequestDto r in approved) _requests.Items.Add(new RequestChoice { Request = r });
                if (_requests.Items.Count > 0) _requests.SelectedIndex = 0;
                _info.Text = approved.Count == 0 ? "No approved requests yet - wait for the Agriculture Officer's approval." : approved.Count + " approved request(s) ready.";
            }

            _quotes.DataSource = null;
            _loadedRequest = null;

            List<ConfirmedOrderDto> orders;
            if (ServiceCaller.TryCall(c => c.GetConfirmedOrders(Session.FarmerId).ToList(), out orders))
                _orders.DataSource = orders;
        }

        private void OnLoadQuotations(object sender, EventArgs e)
        {
            var choice = _requests.SelectedItem as RequestChoice;
            if (choice == null) { UiHelper.ShowError("There is no approved request to load quotations for."); return; }

            List<QuotationDto> quotes;
            if (!ServiceCaller.TryCall(c => c.GetQuotationsForRequest(choice.Request.RequestId).ToList(), out quotes)) return;

            _loadedRequest = choice.Request;
            _quotes.DataSource = quotes;

            string note = quotes.Any(q => q.IsPlaceholder) ? "  [Placeholder data: Supplier service not connected]" : "";
            _info.Text = quotes.Count + " quotation(s) for request #" + choice.Request.RequestId + note;
            _info.ForeColor = note.Length > 0 ? Color.DarkOrange : Color.DimGray;
        }

        /// <summary>Colour the cheapest eligible quotation green and grey out the ones that cannot cover the quantity.</summary>
        private void HighlightQuotes()
        {
            var rows = _quotes.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is QuotationDto).ToList();
            QuotationDto best = rows.Select(r => (QuotationDto)r.DataBoundItem).Where(q => q.MeetsStock).OrderBy(q => q.TotalPrice).FirstOrDefault();

            foreach (DataGridViewRow row in rows)
            {
                var q = (QuotationDto)row.DataBoundItem;
                if (!q.MeetsStock) row.DefaultCellStyle.ForeColor = Color.Gray;
                else if (best != null && q.QuotationId == best.QuotationId)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(200, 230, 201);
                    row.DefaultCellStyle.Font = new Font(_quotes.Font, FontStyle.Bold);
                }
            }
        }

        private void OnConfirm(object sender, EventArgs e)
        {
            var quote = _quotes.CurrentRow == null ? null : _quotes.CurrentRow.DataBoundItem as QuotationDto;
            if (_loadedRequest == null || quote == null) { UiHelper.ShowError("Load quotations and select one first."); return; }
            if (!quote.MeetsStock) { UiHelper.ShowError(quote.SupplierName + " does not have enough stock for this request."); return; }

            string message = "Confirm this order?\n\nItem: " + _loadedRequest.ItemName + " x" + _loadedRequest.Quantity +
                             "\nSupplier: " + quote.SupplierName + "\nTotal: " + UiHelper.Money(quote.TotalPrice) +
                             "\nEstimated delivery: " + quote.EstimatedDeliveryDate.ToString("d") +
                             (quote.MeetsPreferredDate ? "" : "\n\nNote: this is AFTER your preferred date.");
            if (!UiHelper.Confirm(message)) return;

            ConfirmedOrderDto order;
            var input = new OrderConfirmationInput { FarmerId = Session.FarmerId, RequestId = _loadedRequest.RequestId, QuotationId = quote.QuotationId };
            if (!ServiceCaller.TryCall(c => c.ConfirmOrder(input), out order)) return;

            UiHelper.ShowInfo("Order #" + order.OrderId + " confirmed with " + order.SupplierName + ".\nTotal: " + UiHelper.Money(order.TotalPrice) +
                              "\nSupplier notified: " + (order.SupplierNotified ? "yes" : "not yet") + ", Officer notified: " + (order.OfficerNotified ? "yes" : "not yet"));
            RefreshData();
        }
    }
}
