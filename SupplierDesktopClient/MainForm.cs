using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Web.Services.Protocols;
using System.Windows.Forms;

namespace Supplier.Client
{
    /// <summary>Supplier portal: consumes the Supplier SOAP service (inventory, quotations, deliveries, dashboard).</summary>
    public class MainForm : Form
    {
        private static readonly string[] DeliveryFlow = { "Pending", "Dispatched", "InTransit", "Delivered" };
        private const int LowStockLimit = 25;

        private SupplierWS _ws;
        private SupplierInfo _supplier;

        // header
        private readonly TextBox txtUrl = new TextBox();
        private readonly ComboBox cboSupplier = new ComboBox();
        private readonly Label lblStatus = new Label();
        private readonly TabControl tabs = new TabControl();

        // dashboard
        private readonly Dictionary<string, Label> _stat = new Dictionary<string, Label>();

        // inventory
        private DataGridView gridInv;
        private TextBox txtItemName, txtStock, txtPrice;

        // requests + quotations
        private DataGridView gridReq, gridQuo;
        private TextBox txtQPrice, txtQDays;

        // deliveries
        private DataGridView gridDel;

        public MainForm()
        {
            Text = "AgriConnect - Supplier Portal (SOAP client)";
            Size = new Size(1150, 720);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            BuildLayout();
            Shown += delegate { AutoConnect(); };
        }

        // ================================================================ layout

        private void BuildLayout()
        {
            tabs.Dock = DockStyle.Fill;
            tabs.Enabled = false;
            tabs.TabPages.Add(BuildDashboardTab());
            tabs.TabPages.Add(BuildInventoryTab());
            tabs.TabPages.Add(BuildQuotationTab());
            tabs.TabPages.Add(BuildDeliveryTab());

            var top = NewBar();
            top.Dock = DockStyle.Top;
            txtUrl.Width = 360;
            txtUrl.Text = ConfigurationManager.AppSettings["ServiceUrl"];
            cboSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSupplier.Width = 260;
            cboSupplier.DisplayMember = "Name";
            cboSupplier.SelectedIndexChanged += delegate { SupplierChanged(); };
            top.Controls.Add(NewLabel("Service URL"));
            top.Controls.Add(txtUrl);
            top.Controls.Add(NewButton("Connect", delegate { ConnectWithMessage(); }));
            top.Controls.Add(NewLabel("Supplier"));
            top.Controls.Add(cboSupplier);
            top.Controls.Add(NewButton("Refresh all", delegate { Run(RefreshAll); }));

            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 24;
            lblStatus.Padding = new Padding(8, 4, 0, 0);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Text = "Not connected.";

            // fill control first, then the docked edges
            Controls.Add(tabs);
            Controls.Add(top);
            Controls.Add(lblStatus);
        }

        private TabPage BuildDashboardTab()
        {
            var page = new TabPage("Dashboard");
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true };
            flow.Controls.Add(NewCard("TotalInventoryItems", "Inventory items"));
            flow.Controls.Add(NewCard("LowStockItems", "Low stock items (below " + LowStockLimit + ")"));
            flow.Controls.Add(NewCard("TotalInventoryValue", "Stock value (LKR)"));
            flow.Controls.Add(NewCard("OpenRequests", "Open requests to quote"));
            flow.Controls.Add(NewCard("PendingQuotations", "Quotations waiting for officer"));
            flow.Controls.Add(NewCard("ApprovedQuotations", "Approved - waiting for farmer"));
            flow.Controls.Add(NewCard("AcceptedOrders", "Accepted orders"));
            flow.Controls.Add(NewCard("ActiveDeliveries", "Active deliveries"));
            flow.Controls.Add(NewCard("CompletedDeliveries", "Completed deliveries"));
            page.Controls.Add(flow);
            return page;
        }

        private TabPage BuildInventoryTab()
        {
            var page = new TabPage("Inventory");
            gridInv = NewGrid();
            gridInv.SelectionChanged += delegate { InventorySelected(); };
            gridInv.DataBindingComplete += delegate { HighlightLowStock(); };

            txtItemName = NewBox(220);
            txtStock = NewBox(70);
            txtPrice = NewBox(90);

            var bar = NewBar();
            bar.Controls.Add(NewLabel("Item name"));
            bar.Controls.Add(txtItemName);
            bar.Controls.Add(NewLabel("Stock qty"));
            bar.Controls.Add(txtStock);
            bar.Controls.Add(NewLabel("Unit price (LKR)"));
            bar.Controls.Add(txtPrice);
            bar.Controls.Add(NewButton("Add item", delegate { Run(AddItem); }));
            bar.Controls.Add(NewButton("Update selected", delegate { Run(UpdateItem); }));
            bar.Controls.Add(NewButton("Delete selected", delegate { Run(DeleteItem); }));
            bar.Controls.Add(NewButton("Clear", delegate { ClearInventoryFields(); }));

            var layout = NewLayout(2);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(bar, 0, 0);
            layout.Controls.Add(gridInv, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildQuotationTab()
        {
            var page = new TabPage("Requests & Quotations");
            gridReq = NewGrid();
            gridQuo = NewGrid();
            txtQPrice = NewBox(90);
            txtQDays = NewBox(60);

            var quoteBar = NewBar();
            quoteBar.Controls.Add(NewLabel("Unit price (LKR)"));
            quoteBar.Controls.Add(txtQPrice);
            quoteBar.Controls.Add(NewLabel("Delivery estimate (days)"));
            quoteBar.Controls.Add(txtQDays);
            quoteBar.Controls.Add(NewButton("Submit quotation for selected request", delegate { Run(SubmitQuotation); }));
            quoteBar.Controls.Add(NewLabel("(available stock is taken from your inventory)"));

            var demoBar = NewBar();
            demoBar.Controls.Add(NewLabel("Demo only - normally done by the other services:"));
            demoBar.Controls.Add(NewButton("Officer approves", delegate { Run(delegate { SetQuotationStatus("Approved"); }); }));
            demoBar.Controls.Add(NewButton("Officer rejects", delegate { Run(delegate { SetQuotationStatus("Rejected"); }); }));
            demoBar.Controls.Add(NewButton("Farmer accepts (creates delivery)", delegate { Run(delegate { SetQuotationStatus("Accepted"); }); }));

            var layout = NewLayout(6);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(NewHeading("Open supply requests in my region"), 0, 0);
            layout.Controls.Add(gridReq, 0, 1);
            layout.Controls.Add(quoteBar, 0, 2);
            layout.Controls.Add(NewHeading("My quotations"), 0, 3);
            layout.Controls.Add(gridQuo, 0, 4);
            layout.Controls.Add(demoBar, 0, 5);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildDeliveryTab()
        {
            var page = new TabPage("Deliveries");
            gridDel = NewGrid();

            var bar = NewBar();
            bar.Controls.Add(NewButton("Move selected delivery to next status", delegate { Run(AdvanceDelivery); }));
            bar.Controls.Add(NewLabel("Pending > Dispatched > InTransit > Delivered"));

            var layout = NewLayout(2);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(bar, 0, 0);
            layout.Controls.Add(gridDel, 0, 1);
            page.Controls.Add(layout);
            return page;
        }

        // ================================================================ connect

        private void AutoConnect()
        {
            try { Connect(); }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Firebrick;
                lblStatus.Text = "Not connected: " + ex.Message + "  (start Supplier.Service first, then press Connect)";
            }
        }

        private void ConnectWithMessage()
        {
            Run(Connect);
        }

        private void Connect()
        {
            _ws = new SupplierWS(txtUrl.Text.Trim())
            {
                AuthHeaderValue = new AuthHeader { ApiKey = ConfigurationManager.AppSettings["ApiKey"] },
                Timeout = 30000
            };

            string health = _ws.CheckHealth();
            lblStatus.Text = health;
            lblStatus.ForeColor = health.StartsWith("OK") ? Color.SeaGreen : Color.Firebrick;
            if (!health.StartsWith("OK"))
            {
                tabs.Enabled = false;
                MessageBox.Show(health + "\n\nFix the connection string in Services\\SupplierWebService\\Web.config (use the same server name as in SSMS) and run the service again.",
                    "Database problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SupplierInfo[] suppliers = _ws.GetSuppliers();
            cboSupplier.DataSource = suppliers;     // selecting the first supplier loads everything
            tabs.Enabled = suppliers.Length > 0;
        }

        private void SupplierChanged()
        {
            _supplier = cboSupplier.SelectedItem as SupplierInfo;
            if (_supplier == null || _ws == null) return;
            Run(RefreshAll);
        }

        // ================================================================ loading

        private void RefreshAll()
        {
            if (_supplier == null) return;
            LoadDashboard();
            LoadInventory();
            LoadRequests();
            LoadQuotations();
            LoadDeliveries();
            lblStatus.ForeColor = Color.SeaGreen;
            lblStatus.Text = "Connected - " + _supplier.Name + " (" + _supplier.Region + " region)   |   updated " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void LoadDashboard()
        {
            DashboardSummary s = _ws.GetDashboardSummary(_supplier.Id);
            _stat["TotalInventoryItems"].Text = s.TotalInventoryItems.ToString();
            _stat["LowStockItems"].Text = s.LowStockItems.ToString();
            _stat["TotalInventoryValue"].Text = s.TotalInventoryValue.ToString("N0");
            _stat["OpenRequests"].Text = s.OpenRequests.ToString();
            _stat["PendingQuotations"].Text = s.PendingQuotations.ToString();
            _stat["ApprovedQuotations"].Text = s.ApprovedQuotations.ToString();
            _stat["AcceptedOrders"].Text = s.AcceptedOrders.ToString();
            _stat["ActiveDeliveries"].Text = s.ActiveDeliveries.ToString();
            _stat["CompletedDeliveries"].Text = s.CompletedDeliveries.ToString();
        }

        private void LoadInventory()
        {
            gridInv.DataSource = _ws.GetInventory(_supplier.Id);
            FormatGrid(gridInv, "SupplierId");
            ClearInventoryFields();
        }

        private void LoadRequests()
        {
            gridReq.DataSource = _ws.GetOpenRequests(_supplier.Id);
            FormatGrid(gridReq, "Status");
        }

        private void LoadQuotations()
        {
            gridQuo.DataSource = _ws.GetMyQuotations(_supplier.Id);
            FormatGrid(gridQuo, "SupplierId");
        }

        private void LoadDeliveries()
        {
            gridDel.DataSource = _ws.GetDeliveries(_supplier.Id);
            FormatGrid(gridDel, "SupplierId");
        }

        // ================================================================ inventory actions

        private void InventorySelected()
        {
            InventoryItem item = SelectedItem<InventoryItem>(gridInv);
            if (item == null) return;
            txtItemName.Text = item.ItemName;
            txtStock.Text = item.StockQty.ToString();
            txtPrice.Text = item.UnitPrice.ToString("0.##");
        }

        private void ClearInventoryFields()
        {
            txtItemName.Clear();
            txtStock.Clear();
            txtPrice.Clear();
        }

        private InventoryItem ReadInventoryFields()
        {
            int qty;
            decimal price;
            if (!int.TryParse(txtStock.Text.Trim(), out qty)) throw new InvalidOperationException("Stock qty must be a whole number.");
            if (!decimal.TryParse(txtPrice.Text.Trim(), out price)) throw new InvalidOperationException("Unit price must be a number.");
            return new InventoryItem { SupplierId = _supplier.Id, ItemName = txtItemName.Text.Trim(), StockQty = qty, UnitPrice = price };
        }

        private void AddItem()
        {
            _ws.AddInventoryItem(ReadInventoryFields());
            LoadInventory();
            LoadDashboard();
        }

        private void UpdateItem()
        {
            InventoryItem selected = SelectedItem<InventoryItem>(gridInv);
            if (selected == null) throw new InvalidOperationException("Select an inventory row first.");
            InventoryItem item = ReadInventoryFields();
            item.Id = selected.Id;
            _ws.UpdateInventoryItem(item);
            LoadInventory();
            LoadDashboard();
        }

        private void DeleteItem()
        {
            InventoryItem selected = SelectedItem<InventoryItem>(gridInv);
            if (selected == null) throw new InvalidOperationException("Select an inventory row first.");
            if (MessageBox.Show("Delete \"" + selected.ItemName + "\"?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _ws.DeleteInventoryItem(selected.Id);
            LoadInventory();
            LoadDashboard();
        }

        // ================================================================ quotation actions

        private void SubmitQuotation()
        {
            SupplyRequest req = SelectedItem<SupplyRequest>(gridReq);
            if (req == null) throw new InvalidOperationException("Select a supply request first.");
            decimal price;
            int days;
            if (!decimal.TryParse(txtQPrice.Text.Trim(), out price)) throw new InvalidOperationException("Unit price must be a number.");
            if (!int.TryParse(txtQDays.Text.Trim(), out days)) throw new InvalidOperationException("Delivery estimate must be a whole number of days.");

            // stock quantity offered = what this supplier currently holds of the requested item
            int stock = 0;
            foreach (InventoryItem i in _ws.GetInventory(_supplier.Id))
                if (string.Equals(i.ItemName, req.ItemName, StringComparison.OrdinalIgnoreCase)) stock = i.StockQty;

            _ws.SubmitQuotation(new Quotation
            {
                SupplyRequestId = req.RequestId,
                SupplierId = _supplier.Id,
                UnitPrice = price,
                StockAvailable = stock,
                DeliveryDays = days
            });
            txtQPrice.Clear();
            txtQDays.Clear();
            LoadRequests();
            LoadQuotations();
            LoadDashboard();
        }

        private void SetQuotationStatus(string status)
        {
            Quotation q = SelectedItem<Quotation>(gridQuo);
            if (q == null) throw new InvalidOperationException("Select a quotation first.");
            _ws.UpdateQuotationStatus(q.Id, status);
            RefreshAll();
        }

        // ================================================================ delivery actions

        private void AdvanceDelivery()
        {
            Delivery d = SelectedItem<Delivery>(gridDel);
            if (d == null) throw new InvalidOperationException("Select a delivery first.");
            int index = Array.IndexOf(DeliveryFlow, d.Status);
            if (index < 0 || index >= DeliveryFlow.Length - 1) throw new InvalidOperationException("This delivery is already completed.");
            _ws.UpdateDeliveryStatus(d.Id, DeliveryFlow[index + 1]);
            LoadDeliveries();
            LoadDashboard();
        }

        // ================================================================ helpers

        private void Run(Action action)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                action();
            }
            catch (SoapException ex)
            {
                MessageBox.Show(ex.Message, "Supplier service", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (WebException ex)
            {
                MessageBox.Show("Cannot reach the Supplier service at " + txtUrl.Text + "\n\nStart the Supplier.Service project first (Ctrl+F5), then press Connect.\n\n" + ex.Message,
                    "Connection problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AgriConnect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private static T SelectedItem<T>(DataGridView grid) where T : class
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as T;
        }

        private void HighlightLowStock()
        {
            foreach (DataGridViewRow row in gridInv.Rows)
            {
                InventoryItem item = row.DataBoundItem as InventoryItem;
                if (item != null && item.StockQty < LowStockLimit)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 200);
            }
        }

        private static void FormatGrid(DataGridView grid, string hiddenColumn)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name == hiddenColumn) col.Visible = false;
                if (col.ValueType == typeof(decimal)) col.DefaultCellStyle.Format = "N2";
                if (col.ValueType == typeof(DateTime)) col.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            }
        }

        private Control NewCard(string key, string caption)
        {
            var card = new Panel { Width = 240, Height = 100, Margin = new Padding(8), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            var cap = new Label { Text = caption, Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 8, 0, 0), ForeColor = Color.DimGray };
            var val = new Label
            {
                Text = "-",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            card.Controls.Add(val);
            card.Controls.Add(cap);
            _stat[key] = val;
            return card;
        }

        private static DataGridView NewGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window
            };
        }

        private static TableLayoutPanel NewLayout(int rows)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return t;
        }

        private static FlowLayoutPanel NewBar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(4)
            };
        }

        private static Label NewLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(6, 8, 2, 0) };
        }

        private static Label NewHeading(string text)
        {
            return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(6, 8, 0, 2) };
        }

        private static TextBox NewBox(int width)
        {
            return new TextBox { Width = width, Margin = new Padding(2, 4, 6, 2) };
        }

        private static Button NewButton(string text, EventHandler onClick)
        {
            var b = new Button { Text = text, AutoSize = true, Margin = new Padding(4, 2, 4, 2), Padding = new Padding(6, 1, 6, 1) };
            b.Click += onClick;
            return b;
        }
    }
}
