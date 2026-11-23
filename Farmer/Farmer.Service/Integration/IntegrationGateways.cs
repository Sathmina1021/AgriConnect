using System;
using System.Collections.Generic;
using Farmer.Contracts;

namespace Farmer.Service.Integration
{
    // =====================================================================
    // Gateways = the ONLY place where the Farmer service talks to the other
    // roles' SOAP services (Supplier, Agriculture Officer, Administration).
    // The business logic depends on these interfaces, so the real WCF
    // implementation can be swapped for a mock without touching FarmerService.
    // =====================================================================

    public class OfficerDecisionResult
    {
        public bool HasDecision { get; set; }
        public bool Approved { get; set; }
        public int OfficerId { get; set; }
        public string Remarks { get; set; }
    }

    public interface ISupplierGateway
    {
        /// <summary>Tells regional suppliers a new request exists. Returns true if the call succeeded.</summary>
        bool NotifyNewSupplyRequest(SupplyRequestDto request, FarmerDto farmer);
        /// <summary>Fetches quotations. May throw if the Supplier service is unreachable.</summary>
        List<QuotationDto> GetQuotations(SupplyRequestDto request);
        bool NotifyOrderConfirmed(ConfirmedOrderDto order);
    }

    public interface IOfficerGateway
    {
        bool SubmitForVerification(SupplyRequestDto request, FarmerDto farmer);
        /// <summary>Returns null/HasDecision=false while the officer has not decided yet.</summary>
        OfficerDecisionResult GetDecision(int requestId);
        bool NotifyOrderConfirmed(ConfirmedOrderDto order);
    }

    public interface IAdminGateway
    {
        /// <summary>Audit/monitoring event for the Administration service. Best-effort.</summary>
        bool LogActivity(string actorType, int actorId, string action, string details);
    }

    // =====================================================================
    // MOCK implementations (default for standalone testing)
    // =====================================================================

    /// <summary>Generates deterministic placeholder quotations (same request => same quotation IDs).</summary>
    public static class PlaceholderQuotationProvider
    {
        private static readonly object[][] Suppliers =
        {
            new object[] { 101, "GreenGrow Supplies",    1.00m, 2.0, 3 },   // id, name, price factor, stock factor, delivery days
            new object[] { 102, "Lanka AgroMart",        0.94m, 1.0, 5 },
            new object[] { 103, "Ceylon Agro Traders",   1.07m, 0.6, 2 }    // intentionally short on stock to demo validation
        };

        public static List<QuotationDto> Generate(SupplyRequestDto request)
        {
            var list = new List<QuotationDto>();
            decimal basePrice = 120 + StableSum(request.ItemName) % 380;
            for (int i = 0; i < Suppliers.Length; i++)
            {
                object[] s = Suppliers[i];
                decimal unit = Math.Round(basePrice * (decimal)s[2], 2);
                int days = (int)s[4];
                list.Add(new QuotationDto
                {
                    QuotationId = request.RequestId * 10 + (i + 1),
                    RequestId = request.RequestId,
                    SupplierId = (int)s[0],
                    SupplierName = (string)s[1],
                    UnitPrice = unit,
                    TotalPrice = unit * request.Quantity,
                    StockQuantity = (int)Math.Floor(request.Quantity * (double)s[3]),
                    DeliveryDays = days,
                    EstimatedDeliveryDate = DateTime.Today.AddDays(days),
                    Notes = "Placeholder quotation (Supplier service not connected).",
                    IsPlaceholder = true
                });
            }
            return list;
        }

        private static int StableSum(string text)
        {
            int sum = 0;
            foreach (char c in text ?? "") sum += c;
            return sum;
        }
    }

    public class MockSupplierGateway : ISupplierGateway
    {
        public bool NotifyNewSupplyRequest(SupplyRequestDto request, FarmerDto farmer)
        {
            ServiceLog.Info("[MOCK Supplier] New supply request #" + request.RequestId + " (" + request.ItemName + ") from " + farmer.FullName);
            return true;
        }

        public List<QuotationDto> GetQuotations(SupplyRequestDto request)
        {
            return PlaceholderQuotationProvider.Generate(request);
        }

        public bool NotifyOrderConfirmed(ConfirmedOrderDto order)
        {
            ServiceLog.Info("[MOCK Supplier] Order #" + order.OrderId + " confirmed with supplier " + order.SupplierName);
            return true;
        }
    }

    public class MockOfficerGateway : IOfficerGateway
    {
        private readonly bool _autoDecide;
        public MockOfficerGateway(bool autoDecide) { _autoDecide = autoDecide; }

        public bool SubmitForVerification(SupplyRequestDto request, FarmerDto farmer)
        {
            ServiceLog.Info("[MOCK Officer] Request #" + request.RequestId + " submitted for verification.");
            return true;
        }

        public OfficerDecisionResult GetDecision(int requestId)
        {
            if (!_autoDecide) return new OfficerDecisionResult { HasDecision = false };
            return new OfficerDecisionResult
            {
                HasDecision = true,
                Approved = true,
                OfficerId = 9000,
                Remarks = "Auto-approved by MOCK Agriculture Officer (standalone testing)."
            };
        }

        public bool NotifyOrderConfirmed(ConfirmedOrderDto order)
        {
            ServiceLog.Info("[MOCK Officer] Order #" + order.OrderId + " reported to officer.");
            return true;
        }
    }

    public class MockAdminGateway : IAdminGateway
    {
        public bool LogActivity(string actorType, int actorId, string action, string details)
        {
            ServiceLog.Info("[MOCK Admin] " + actorType + "#" + actorId + " " + action + " - " + details);
            return true;
        }
    }
}
