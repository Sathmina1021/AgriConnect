using System;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Protocols;
using System.Xml.Serialization;

// Client-side proxy for the Supplier SOAP service (same shape as the code "Add Web Reference" / wsdl.exe generates
// from http://localhost:5002/SupplierWS.asmx?WSDL). It is kept in source so the project builds without any extra step.
namespace Supplier.Client
{
    internal static class Ns
    {
        public const string Value = "http://agriconnect/supplier";
    }

    [XmlType(Namespace = Ns.Value)]
    [XmlRoot(Namespace = Ns.Value, IsNullable = false)]
    public class AuthHeader : SoapHeader
    {
        public string ApiKey;
    }

    [XmlType(Namespace = Ns.Value)]
    public class SupplierInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    [XmlType(Namespace = Ns.Value)]
    public class InventoryItem
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }
        public int StockQty { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [XmlType(Namespace = Ns.Value)]
    public class SupplyRequest
    {
        public int RequestId { get; set; }
        public string FarmerName { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string DeliveryLocation { get; set; }
        public string Region { get; set; }
        public DateTime PreferredDate { get; set; }
        public string Status { get; set; }
    }

    [XmlType(Namespace = Ns.Value)]
    public class Quotation
    {
        public int Id { get; set; }
        public int SupplyRequestId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockAvailable { get; set; }
        public int DeliveryDays { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [XmlType(Namespace = Ns.Value)]
    public class Delivery
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }
        public int SupplyRequestId { get; set; }
        public int SupplierId { get; set; }
        public string ItemName { get; set; }
        public string DeliveryLocation { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    [XmlType(Namespace = Ns.Value)]
    public class DashboardSummary
    {
        public int TotalInventoryItems { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int OpenRequests { get; set; }
        public int PendingQuotations { get; set; }
        public int ApprovedQuotations { get; set; }
        public int AcceptedOrders { get; set; }
        public int ActiveDeliveries { get; set; }
        public int CompletedDeliveries { get; set; }
    }

    [WebServiceBinding(Name = "SupplierWSSoap", Namespace = Ns.Value)]
    public class SupplierWS : SoapHttpClientProtocol
    {
        public AuthHeader AuthHeaderValue;

        public SupplierWS(string url)
        {
            Url = url;
        }

        [SoapDocumentMethod(Ns.Value + "/CheckHealth", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public string CheckHealth()
        {
            return (string)Invoke("CheckHealth", new object[0])[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetSuppliers", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public SupplierInfo[] GetSuppliers()
        {
            return (SupplierInfo[])Invoke("GetSuppliers", new object[0])[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetInventory", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public InventoryItem[] GetInventory(int supplierId)
        {
            return (InventoryItem[])Invoke("GetInventory", new object[] { supplierId })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/AddInventoryItem", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public int AddInventoryItem(InventoryItem item)
        {
            return (int)Invoke("AddInventoryItem", new object[] { item })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/UpdateInventoryItem", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public void UpdateInventoryItem(InventoryItem item)
        {
            Invoke("UpdateInventoryItem", new object[] { item });
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/DeleteInventoryItem", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public void DeleteInventoryItem(int itemId)
        {
            Invoke("DeleteInventoryItem", new object[] { itemId });
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/ReceiveSupplyRequest", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public void ReceiveSupplyRequest(SupplyRequest request)
        {
            Invoke("ReceiveSupplyRequest", new object[] { request });
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetOpenRequests", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public SupplyRequest[] GetOpenRequests(int supplierId)
        {
            return (SupplyRequest[])Invoke("GetOpenRequests", new object[] { supplierId })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/SubmitQuotation", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public int SubmitQuotation(Quotation quotation)
        {
            return (int)Invoke("SubmitQuotation", new object[] { quotation })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetMyQuotations", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public Quotation[] GetMyQuotations(int supplierId)
        {
            return (Quotation[])Invoke("GetMyQuotations", new object[] { supplierId })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/UpdateQuotationStatus", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public void UpdateQuotationStatus(int quotationId, string newStatus)
        {
            Invoke("UpdateQuotationStatus", new object[] { quotationId, newStatus });
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetDeliveries", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public Delivery[] GetDeliveries(int supplierId)
        {
            return (Delivery[])Invoke("GetDeliveries", new object[] { supplierId })[0];
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/UpdateDeliveryStatus", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public void UpdateDeliveryStatus(int deliveryId, string newStatus)
        {
            Invoke("UpdateDeliveryStatus", new object[] { deliveryId, newStatus });
        }

        [SoapHeader("AuthHeaderValue")]
        [SoapDocumentMethod(Ns.Value + "/GetDashboardSummary", RequestNamespace = Ns.Value, ResponseNamespace = Ns.Value,
            Use = SoapBindingUse.Literal, ParameterStyle = SoapParameterStyle.Wrapped)]
        public DashboardSummary GetDashboardSummary(int supplierId)
        {
            return (DashboardSummary)Invoke("GetDashboardSummary", new object[] { supplierId })[0];
        }
    }
}
