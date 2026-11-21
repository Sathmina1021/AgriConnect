using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Services;
using System.Web.Services.Protocols;
using SupplierWebService.BLL;
using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService
{
    /// <summary>
    /// AgriConnect - Supplier SOAP web service (ASMX).
    /// Owns inventory, quotations and deliveries for the Supplier role and uses its own database (AgriSupplierDb).
    /// Contract (WSDL): SupplierWS.asmx?WSDL
    /// </summary>
    [WebService(Namespace = "http://agriconnect/supplier", Description = "AgriConnect Supplier service: inventory, quotations, deliveries, dashboard.")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class SupplierWS : WebService
    {
        /// <summary>API key sent by every consumer in the SOAP header.</summary>
        public AuthHeader Auth;

        private readonly SupplierDAL _suppliers = new SupplierDAL();
        private readonly InventoryBLL _inventory = new InventoryBLL();
        private readonly RequestBLL _requests = new RequestBLL();
        private readonly QuotationBLL _quotations = new QuotationBLL();
        private readonly DeliveryBLL _deliveries = new DeliveryBLL();
        private readonly DashboardBLL _dashboard = new DashboardBLL();

        // ---------------------------------------------------------------- health (no API key needed)

        [WebMethod(Description = "Checks that the service is running and the database is reachable.")]
        public string CheckHealth()
        {
            if (!DbInitializer.EnsureCreated())
                return "ERROR: " + DbInitializer.LastError;
            try
            {
                return "OK - database reachable, " + _suppliers.GetAll().Count + " supplier(s) registered.";
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.Message;
            }
        }

        // ---------------------------------------------------------------- suppliers

        [WebMethod(Description = "Lists the registered suppliers.")]
        [SoapHeader("Auth")]
        public List<SupplierInfo> GetSuppliers()
        {
            return Run(() => _suppliers.GetAll());
        }

        // ---------------------------------------------------------------- inventory management

        [WebMethod(Description = "Returns the inventory of one supplier.")]
        [SoapHeader("Auth")]
        public List<InventoryItem> GetInventory(int supplierId)
        {
            return Run(() => _inventory.GetInventory(supplierId));
        }

        [WebMethod(Description = "Adds a new inventory item and returns its id.")]
        [SoapHeader("Auth")]
        public int AddInventoryItem(InventoryItem item)
        {
            return Run(() => _inventory.AddItem(item));
        }

        [WebMethod(Description = "Updates name, stock and price of an inventory item.")]
        [SoapHeader("Auth")]
        public void UpdateInventoryItem(InventoryItem item)
        {
            Run(() => { _inventory.UpdateItem(item); return 0; });
        }

        [WebMethod(Description = "Deletes an inventory item.")]
        [SoapHeader("Auth")]
        public void DeleteInventoryItem(int itemId)
        {
            Run(() => { _inventory.DeleteItem(itemId); return 0; });
        }

        // ---------------------------------------------------------------- supply requests / quotation processing

        [WebMethod(Description = "Called by the Farmer service to notify regional suppliers about a new supply request.")]
        [SoapHeader("Auth")]
        public void ReceiveSupplyRequest(SupplyRequest request)
        {
            Run(() => { _requests.Receive(request); return 0; });
        }

        [WebMethod(Description = "Open supply requests in the supplier's region that the supplier has not quoted yet.")]
        [SoapHeader("Auth")]
        public List<SupplyRequest> GetOpenRequests(int supplierId)
        {
            return Run(() => _requests.GetOpenRequests(supplierId));
        }

        [WebMethod(Description = "Submits a quotation (price, stock quantity, delivery estimate) and returns its id.")]
        [SoapHeader("Auth")]
        public int SubmitQuotation(Quotation quotation)
        {
            return Run(() => _quotations.Submit(quotation));
        }

        [WebMethod(Description = "Returns all quotations of one supplier.")]
        [SoapHeader("Auth")]
        public List<Quotation> GetMyQuotations(int supplierId)
        {
            return Run(() => _quotations.GetMyQuotations(supplierId));
        }

        [WebMethod(Description = "Called by the Officer service (Approved/Rejected) and the Farmer service (Accepted). Accepting creates the delivery.")]
        [SoapHeader("Auth")]
        public void UpdateQuotationStatus(int quotationId, string newStatus)
        {
            Run(() => { _quotations.UpdateStatus(quotationId, newStatus); return 0; });
        }

        // ---------------------------------------------------------------- delivery status

        [WebMethod(Description = "Returns the deliveries of one supplier.")]
        [SoapHeader("Auth")]
        public List<Delivery> GetDeliveries(int supplierId)
        {
            return Run(() => _deliveries.GetDeliveries(supplierId));
        }

        [WebMethod(Description = "Moves a delivery one step forward: Pending, Dispatched, InTransit, Delivered.")]
        [SoapHeader("Auth")]
        public void UpdateDeliveryStatus(int deliveryId, string newStatus)
        {
            Run(() => { _deliveries.UpdateStatus(deliveryId, newStatus); return 0; });
        }

        // ---------------------------------------------------------------- supplier dashboard

        [WebMethod(Description = "Summary numbers for the supplier dashboard.")]
        [SoapHeader("Auth")]
        public DashboardSummary GetDashboardSummary(int supplierId)
        {
            return Run(() => _dashboard.GetSummary(supplierId));
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Checks the API key, runs the operation and converts business errors into SOAP faults.</summary>
        private T Run<T>(Func<T> operation)
        {
            Authorize();
            try
            {
                if (!DbInitializer.EnsureCreated())
                    throw new SoapException("Database not available: " + DbInitializer.LastError, SoapException.ServerFaultCode);
                return operation();
            }
            catch (BusinessException ex)
            {
                throw new SoapException(ex.Message, SoapException.ClientFaultCode);
            }
            catch (SoapException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SoapException("Server error: " + ex.Message, SoapException.ServerFaultCode);
            }
        }

        private void Authorize()
        {
            string required = ConfigurationManager.AppSettings["ApiKey"];
            if (string.IsNullOrEmpty(required)) return;   // key checking switched off in Web.config
            if (Auth == null || Auth.ApiKey != required)
                throw new SoapException("Unauthorized: missing or invalid API key.", SoapException.ClientFaultCode);
        }
    }
}
