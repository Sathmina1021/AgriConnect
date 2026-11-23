using System.Drawing;
using System.Windows.Forms;

namespace Farmer.Client.Controls
{
    /// <summary>Shared colour-coding for status cells so every grid looks the same.</summary>
    internal static class StatusColors
    {
        public static void FormatRequestStatusCell(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = (DataGridView)sender;
            if (e.RowIndex < 0 || e.Value == null || grid.Columns[e.ColumnIndex].DataPropertyName != "Status") return;

            switch (e.Value.ToString())
            {
                case "Pending":   e.CellStyle.ForeColor = Color.DarkOrange; break;
                case "Approved":  e.CellStyle.ForeColor = Color.ForestGreen; break;
                case "Rejected":  e.CellStyle.ForeColor = Color.Firebrick; break;
                case "Confirmed": e.CellStyle.ForeColor = Color.RoyalBlue; break;
                case "Cancelled": e.CellStyle.ForeColor = Color.Gray; break;
            }
            e.CellStyle.Font = new Font(grid.Font, FontStyle.Bold);
        }
    }
}
