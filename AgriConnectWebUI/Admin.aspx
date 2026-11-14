<%@ Page Title="Administration Portal" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="AgriConnectWebUI.Admin" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <asp:Label ID="lblStatus" runat="server" CssClass="status-msg" Visible="false"></asp:Label>

    <div class="panel-box">
        <h2>Distributed ASMX Services Health Status</h2>
        <div class="row-flex">
            <div class="col-quarter"><strong>FarmerService (5001):</strong> <asp:Label ID="lblFarmerHealth" runat="server"></asp:Label></div>
            <div class="col-quarter"><strong>SupplierService (5002):</strong> <asp:Label ID="lblSupplierHealth" runat="server"></asp:Label></div>
            <div class="col-quarter"><strong>OfficerService (5003):</strong> <asp:Label ID="lblOfficerHealth" runat="server"></asp:Label></div>
            <div class="col-quarter"><strong>AdminService (5004):</strong> <asp:Label ID="lblAdminHealth" runat="server"></asp:Label></div>
        </div>
    </div>

    <div class="row-flex">
        <div class="col-half panel-box">
            <h2>Add User Account</h2>
            <table class="form-table">
                <tr>
                    <td>Full Name:</td>
                    <td><asp:TextBox ID="txtUserName" runat="server" CssClass="txt-input"></asp:TextBox></td>
                </tr>
                <tr>
                    <td>Email:</td>
                    <td><asp:TextBox ID="txtUserEmail" runat="server" CssClass="txt-input"></asp:TextBox></td>
                </tr>
                <tr>
                    <td>Role:</td>
                    <td>
                        <asp:DropDownList ID="ddlUserRole" runat="server" CssClass="txt-input">
                            <asp:ListItem Text="Farmer" Value="Farmer" />
                            <asp:ListItem Text="Supplier" Value="Supplier" />
                            <asp:ListItem Text="Officer" Value="Officer" />
                            <asp:ListItem Text="Admin" Value="Admin" />
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td>District:</td>
                    <td><asp:TextBox ID="txtUserDistrict" runat="server" CssClass="txt-input" Text="Colombo"></asp:TextBox></td>
                </tr>
                <tr>
                    <td></td>
                    <td><asp:Button ID="btnAddUser" runat="server" Text="Create User" CssClass="btn-blue" OnClick="btnAddUser_Click" /></td>
                </tr>
            </table>
        </div>

        <div class="col-half panel-box">
            <h2>Platform User Accounts</h2>
            <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="False" CssClass="grid-view" OnRowCommand="gvUsers_RowCommand">
                <Columns>
                    <asp:BoundField DataField="FullName" HeaderText="Name" />
                    <asp:BoundField DataField="Email" HeaderText="Email" />
                    <asp:BoundField DataField="Role" HeaderText="Role" />
                    <asp:BoundField DataField="IsActive" HeaderText="Active" />
                    <asp:TemplateField HeaderText="Action">
                        <ItemTemplate>
                            <asp:Button ID="btnToggle" runat="server" Text="Toggle Status" CssClass="btn-blue" CommandName="ToggleUser" CommandArgument='<%# Eval("UserId") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>
    </div>

    <div class="panel-box">
        <h2>Cross-Service System Audit Logs</h2>
        <asp:GridView ID="gvAuditLogs" runat="server" AutoGenerateColumns="False" CssClass="grid-view">
            <Columns>
                <asp:BoundField DataField="LogTime" HeaderText="Timestamp" />
                <asp:BoundField DataField="ServiceName" HeaderText="Service" />
                <asp:BoundField DataField="ActionName" HeaderText="Action" />
                <asp:BoundField DataField="Details" HeaderText="Details" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
