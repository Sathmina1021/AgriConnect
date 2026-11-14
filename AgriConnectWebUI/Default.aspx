<%@ Page Title="Home" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="AgriConnectWebUI.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="panel-box">
        <h2>AgriConnect Service-Oriented Ecosystem</h2>
        <p>Select one of the 4 stakeholder roles below. Each role consumes its own independent ASMX Web Service and SQL Server Database.</p>
    </div>

    <div class="row-flex">
        <div class="col-quarter panel-box">
            <h3>1. Farmer Portal</h3>
            <p>Register farmer profiles, submit supply requests, compare supplier quotations, confirm orders, and post crop listings.</p>
            <p><small>Service: FarmerService.asmx (Port 5001)</small></p>
            <asp:Button ID="btnGoFarmer" runat="server" Text="Open Farmer UI" CssClass="btn-green" PostBackUrl="~/Farmer.aspx" CausesValidation="false" />
        </div>

        <div class="col-quarter panel-box">
            <h3>2. Supplier Portal</h3>
            <p>Manage warehouse inventory, view open farmer requests, send quotations, and update delivery status.</p>
            <p><small>Service: SupplierService.asmx (Port 5002)</small></p>
            <asp:Button ID="btnGoSupplier" runat="server" Text="Open Supplier UI" CssClass="btn-blue" PostBackUrl="~/Supplier.aspx" CausesValidation="false" />
        </div>

        <div class="col-quarter panel-box">
            <h3>3. Agriculture Officer</h3>
            <p>Verify compliance of supply requests and quotations, approve/reject requests, and publish recommendations.</p>
            <p><small>Service: OfficerService.asmx (Port 5003)</small></p>
            <asp:Button ID="btnGoOfficer" runat="server" Text="Open Officer UI" CssClass="btn-green" PostBackUrl="~/Officer.aspx" CausesValidation="false" />
        </div>

        <div class="col-quarter panel-box">
            <h3>4. Administration</h3>
            <p>Manage user accounts, monitor service status, and inspect cross-service audit logs.</p>
            <p><small>Service: AdminService.asmx (Port 5004)</small></p>
            <asp:Button ID="btnGoAdmin" runat="server" Text="Open Admin UI" CssClass="btn-blue" PostBackUrl="~/Admin.aspx" CausesValidation="false" />
        </div>
    </div>
</asp:Content>
