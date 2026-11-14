using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web.UI.WebControls;
using AgriConnectWebUI.Helpers;

namespace AgriConnectWebUI
{
    public partial class Admin : System.Web.UI.Page
    {
        private string farmerUrl = ConfigurationManager.AppSettings["FarmerServiceUrl"];
        private string supplierUrl = ConfigurationManager.AppSettings["SupplierServiceUrl"];
        private string officerUrl = ConfigurationManager.AppSettings["OfficerServiceUrl"];
        private string adminUrl = ConfigurationManager.AppSettings["AdminServiceUrl"];

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindAdminData();
            }
        }

        private void BindAdminData()
        {
            DataTable dtUsers = AsmxClientHelper.InvokeWebMethodTable(adminUrl, "GetUsers", null);
            gvUsers.DataSource = dtUsers;
            gvUsers.DataBind();

            gvAuditLogs.DataSource = AsmxClientHelper.InvokeWebMethodTable(adminUrl, "GetAuditLogs", null);
            gvAuditLogs.DataBind();

            lblAdminHealth.Text = dtUsers.Rows.Count > 0 ? "Online" : "Offline";
            lblFarmerHealth.Text = AsmxClientHelper.InvokeWebMethodTable(farmerUrl, "GetAllFarmers", null).Rows.Count >= 0 ? "Online" : "Offline";
            lblSupplierHealth.Text = AsmxClientHelper.InvokeWebMethodTable(supplierUrl, "GetInventory", null).Rows.Count >= 0 ? "Online" : "Offline";
            lblOfficerHealth.Text = AsmxClientHelper.InvokeWebMethodTable(officerUrl, "GetAdvisories", null).Rows.Count >= 0 ? "Online" : "Offline";
        }

        protected void btnAddUser_Click(object sender, EventArgs e)
        {
            AsmxClientHelper.InvokeWebMethodScalar(adminUrl, "AddUser",
                new Dictionary<string, string>
                {
                    { "fullName", txtUserName.Text },
                    { "email", txtUserEmail.Text },
                    { "role", ddlUserRole.SelectedValue },
                    { "district", txtUserDistrict.Text }
                });

            txtUserName.Text = "";
            txtUserEmail.Text = "";
            lblStatus.Text = "User account created.";
            lblStatus.Visible = true;
            BindAdminData();
        }

        protected void gvUsers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ToggleUser")
            {
                AsmxClientHelper.InvokeWebMethodScalar(adminUrl, "ToggleUserStatus",
                    new Dictionary<string, string> { { "userId", e.CommandArgument.ToString() } });
                lblStatus.Text = "User status updated.";
                lblStatus.Visible = true;
                BindAdminData();
            }
        }
    }
}
