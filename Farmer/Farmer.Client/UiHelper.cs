using System;
using System.Drawing;
using System.Windows.Forms;

namespace Farmer.Client
{
    /// <summary>Reusable factory/helpers so every screen has a consistent look with little code.</summary>
    internal static class UiHelper
    {
        public static readonly Color Primary = Color.FromArgb(46, 125, 50);
        public static readonly Color Light = Color.FromArgb(241, 248, 233);
        public static readonly Font BaseFont = new Font("Segoe UI", 9.5f);

        public static Label MakeLabel(string text, int x, int y, int width = 120)
        {
            return new Label { Text = text, Left = x, Top = y + 3, Width = width, AutoSize = false };
        }

        public static TextBox MakeText(int x, int y, int width, bool password = false)
        {
            return new TextBox { Left = x, Top = y, Width = width, UseSystemPasswordChar = password };
        }

        public static Button MakeButton(string text, int x, int y, int width, EventHandler onClick, bool primary = false)
        {
            var b = new Button
            {
                Text = text, Left = x, Top = y, Width = width, Height = 30,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = primary ? Primary : Color.White,
                ForeColor = primary ? Color.White : Color.Black
            };
            b.FlatAppearance.BorderColor = Primary;
            if (onClick != null) b.Click += onClick;
            return b;
        }

        public static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,          // generated proxy classes carry extra members (ExtensionData)
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                EnableHeadersVisualStyles = false
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
            g.AlternatingRowsDefaultCellStyle.BackColor = Light;
            return g;
        }

        public static void AddColumn(DataGridView grid, string header, string property, int width, string format = null)
        {
            var col = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, Width = width };
            if (format != null) col.DefaultCellStyle.Format = format;
            grid.Columns.Add(col);
        }

        /// <summary>A coloured KPI tile used on the dashboard; returns the value label so it can be updated.</summary>
        public static Panel MakeCard(string title, Color color, out Label valueLabel)
        {
            var panel = new Panel { Width = 200, Height = 90, BackColor = color, Margin = new Padding(8) };
            panel.Controls.Add(new Label { Text = title, ForeColor = Color.White, Left = 10, Top = 8, Width = 180, Height = 36, Font = new Font("Segoe UI", 9f) });
            valueLabel = new Label { Text = "-", ForeColor = Color.White, Left = 10, Top = 44, Width = 180, Height = 36, Font = new Font("Segoe UI", 18f, FontStyle.Bold) };
            panel.Controls.Add(valueLabel);
            return panel;
        }

        public static TableLayoutPanel MakeLayout(int columns, params RowStyle[] rows)
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows.Length };
            for (int c = 0; c < columns; c++) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
            foreach (RowStyle r in rows) t.RowStyles.Add(r);
            return t;
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "AgriConnect - Farmer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void ShowInfo(string message)
        {
            MessageBox.Show(message, "AgriConnect - Farmer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static bool Confirm(string message)
        {
            return MessageBox.Show(message, "AgriConnect - Farmer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static string Money(decimal value)
        {
            return "LKR " + value.ToString("N2");
        }
    }
}
