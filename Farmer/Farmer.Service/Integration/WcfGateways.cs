using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using Farmer.Contracts;

namespace Farmer.Service.Integration
{
    // =====================================================================
    // REAL SOAP gateways (used when appSettings UseMockIntegrations = false).
    //
    // The ISupplierRemote / IOfficerRemote / IAdminRemote contracts below are the
    // operations the Farmer service EXPECTS from teammates. Agree the names with
    // them (or adapt only THIS file to their real WSDL / Service Reference).
    // Endpoint addresses live in App.config -> <client>, never in code.
    // =====================================================================

    [DataContract(Namespace = "http://agriconnect.lk/integration/2026")]
    public class RemoteRequestNotice
    {
        [DataMember] public int FarmerRequestId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public string FarmerName { get; set; }
        [DataMember] public string FarmerPhone { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public string DeliveryLocation { get; set; }
        [DataMember] public DateTime PreferredDate { get; set; }
    }

    [DataContract(Namespace = "http://agriconnect.lk/integration/2026")]
    public class RemoteOrderNotice
    {
        [DataMember] public int FarmerOrderId { get; set; }
        [DataMember] public int FarmerRequestId { get; set; }
        [DataMember] public int FarmerId { get; set; }
        [DataMember] public int SupplierId { get; set; }
        [DataMember] public int QuotationId { get; set; }
        [DataMember] public int Quantity { get; set; }
        [DataMember] public decimal TotalPrice { get; set; }
        [DataMember] public DateTime EstimatedDeliveryDate { get; set; }
    }

    [DataContract(Namespace = "http://agriconnect.lk/integration/2026")]
    public class RemoteDecision
    {
        [DataMember] public bool HasDecision { get; set; }
        [DataMember] public bool Approved { get; set; }
        [DataMember] public int OfficerId { get; set; }
        [DataMember] public string Remarks { get; set; }
    }

    [ServiceContract(Namespace = "http://agriconnect.lk/supplier/2026")]
    public interface ISupplierRemote
    {
        [OperationContract] bool ReceiveSupplyRequest(RemoteRequestNotice notice);
        [OperationContract] List<QuotationDto> GetQuotationsForFarmerRequest(int farmerRequestId);
        [OperationContract] bool ReceiveOrderConfirmation(RemoteOrderNotice notice);
    }

    [ServiceContract(Namespace = "http://agriconnect.lk/officer/2026")]
    public interface IOfficerRemote
    {
        [OperationContract] bool ReceiveRequestForVerification(RemoteRequestNotice notice);
        [OperationContract] RemoteDecision GetDecisionForFarmerRequest(int farmerRequestId);
        [OperationContract] bool ReceiveOrderConfirmation(RemoteOrderNotice notice);
    }

    [ServiceContract(Namespace = "http://agriconnect.lk/admin/2026")]
    public interface IAdminRemote
    {
        [OperationContract] bool LogActivity(string actorType, int actorId, string action, string details);
    }

    /// <summary>Shared helper: open channel, call, always Close/Abort correctly.</summary>
    internal static class RemoteCall
    {
        public static TResult Invoke<TChannel, TResult>(string endpointName, Func<TChannel, TResult> call)
        {
            var factory = new ChannelFactory<TChannel>(endpointName);
            var channel = factory.CreateChannel();
            var comm = (ICommunicationObject)channel;
            try
            {
                TResult result = call(channel);
                comm.Close();
                factory.Close();
                return result;
            }
            catch
            {
                comm.Abort();
                factory.Abort();
                throw;
            }
        }

        public static RemoteRequestNotice ToNotice(SupplyRequestDto r, FarmerDto f)
        {
            return new RemoteRequestNotice
            {
                FarmerRequestId = r.RequestId, FarmerId = f.FarmerId, FarmerName = f.FullName, FarmerPhone = f.Phone,
                ItemName = r.ItemName, Quantity = r.Quantity, DeliveryLocation = r.DeliveryLocation, PreferredDate = r.PreferredDate
            };
        }

        public static RemoteOrderNotice ToNotice(ConfirmedOrderDto o)
        {
            return new RemoteOrderNotice
            {
                FarmerOrderId = o.OrderId, FarmerRequestId = o.RequestId, FarmerId = o.FarmerId, SupplierId = o.SupplierId,
                QuotationId = o.QuotationId, Quantity = o.Quantity, TotalPrice = o.TotalPrice, EstimatedDeliveryDate = o.EstimatedDeliveryDate
            };
        }

        /// <summary>Runs a best-effort notification: failures are logged and reported as false, never thrown.</summary>
        public static bool Notify<TChannel>(string role, string endpointName, Func<TChannel, bool> call)
        {
            try { return Invoke(endpointName, call); }
            catch (Exception ex)
            {
                ServiceLog.Warn(role + " service unreachable (" + endpointName + "): " + ex.Message);
                return false;
            }
        }
    }

    public class WcfSupplierGateway : ISupplierGateway
    {
        private const string Endpoint = "SupplierEndpoint";

        public bool NotifyNewSupplyRequest(SupplyRequestDto request, FarmerDto farmer)
        {
            return RemoteCall.Notify<ISupplierRemote>("Supplier", Endpoint, c => c.ReceiveSupplyRequest(RemoteCall.ToNotice(request, farmer)));
        }

        public List<QuotationDto> GetQuotations(SupplyRequestDto request)
        {
            // Throws on failure on purpose: FarmerService falls back to placeholder data and flags it.
            return RemoteCall.Invoke<ISupplierRemote, List<QuotationDto>>(Endpoint, c => c.GetQuotationsForFarmerRequest(request.RequestId));
        }

        public bool NotifyOrderConfirmed(ConfirmedOrderDto order)
        {
            return RemoteCall.Notify<ISupplierRemote>("Supplier", Endpoint, c => c.ReceiveOrderConfirmation(RemoteCall.ToNotice(order)));
        }
    }

    public class WcfOfficerGateway : IOfficerGateway
    {
        private const string Endpoint = "OfficerEndpoint";

        public bool SubmitForVerification(SupplyRequestDto request, FarmerDto farmer)
        {
            return RemoteCall.Notify<IOfficerRemote>("Officer", Endpoint, c => c.ReceiveRequestForVerification(RemoteCall.ToNotice(request, farmer)));
        }

        public OfficerDecisionResult GetDecision(int requestId)
        {
            try
            {
                RemoteDecision d = RemoteCall.Invoke<IOfficerRemote, RemoteDecision>(Endpoint, c => c.GetDecisionForFarmerRequest(requestId));
                if (d == null || !d.HasDecision) return new OfficerDecisionResult { HasDecision = false };
                return new OfficerDecisionResult { HasDecision = true, Approved = d.Approved, OfficerId = d.OfficerId, Remarks = d.Remarks };
            }
            catch (Exception ex)
            {
                ServiceLog.Warn("Officer service unreachable: " + ex.Message);
                return new OfficerDecisionResult { HasDecision = false };
            }
        }

        public bool NotifyOrderConfirmed(ConfirmedOrderDto order)
        {
            return RemoteCall.Notify<IOfficerRemote>("Officer", Endpoint, c => c.ReceiveOrderConfirmation(RemoteCall.ToNotice(order)));
        }
    }

    public class WcfAdminGateway : IAdminGateway
    {
        public bool LogActivity(string actorType, int actorId, string action, string details)
        {
            return RemoteCall.Notify<IAdminRemote>("Admin", "AdminEndpoint", c => c.LogActivity(actorType, actorId, action, details));
        }
    }
}
