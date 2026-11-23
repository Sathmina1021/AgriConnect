using System.Collections.Generic;
using System.ServiceModel;

namespace Farmer.Contracts
{
    /// <summary>
    /// SOAP service contract for the Farmer role of AgriConnect.
    /// Every operation can return a <see cref="ServiceFault"/> (FaultContract) instead of leaking exceptions.
    /// </summary>
    [ServiceContract(Name = "IFarmerService", Namespace = FarmerNamespaces.Service)]
    public interface IFarmerService
    {
        // ---------- 1. Registration & login ----------
        [OperationContract, FaultContract(typeof(ServiceFault))]
        FarmerDto RegisterFarmer(FarmerRegistration registration);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        LoginResult Login(LoginCredentials credentials);

        // ---------- 2. Dashboard ----------
        [OperationContract, FaultContract(typeof(ServiceFault))]
        DashboardSummary GetDashboard(int farmerId);

        // ---------- 3. Supply requests ----------
        [OperationContract, FaultContract(typeof(ServiceFault))]
        SupplyRequestDto CreateSupplyRequest(SupplyRequestInput input);

        /// <param name="statusFilter">Null/empty = all statuses, otherwise one of RequestStatus.*</param>
        [OperationContract, FaultContract(typeof(ServiceFault))]
        List<SupplyRequestDto> GetSupplyRequests(int farmerId, string statusFilter);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        SupplyRequestDto GetSupplyRequest(int farmerId, int requestId);

        /// <summary>Pulls the Officer's decision (and retries failed notifications) for one request.</summary>
        [OperationContract, FaultContract(typeof(ServiceFault))]
        SupplyRequestDto RefreshRequestStatus(int farmerId, int requestId);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        SupplyRequestDto CancelSupplyRequest(int farmerId, int requestId);

        /// <summary>Called BY the Agriculture Officer service to push its approve/reject decision.</summary>
        [OperationContract, FaultContract(typeof(ServiceFault))]
        SupplyRequestDto ApplyOfficerDecision(OfficerDecisionInput decision);

        // ---------- 4. Quotations & order confirmation ----------
        /// <summary>Only allowed for requests that are Approved by the Agriculture Officer.</summary>
        [OperationContract, FaultContract(typeof(ServiceFault))]
        List<QuotationDto> GetQuotationsForRequest(int requestId);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        ConfirmedOrderDto ConfirmOrder(OrderConfirmationInput input);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        List<ConfirmedOrderDto> GetConfirmedOrders(int farmerId);

        // ---------- 5. Crop listings & marketplace ----------
        [OperationContract, FaultContract(typeof(ServiceFault))]
        CropListingDto CreateCropListing(CropListingInput input);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        CropListingDto UpdateCropListing(CropListingInput input);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        CropListingDto CloseCropListing(int farmerId, int listingId);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        void DeleteCropListing(int farmerId, int listingId);

        [OperationContract, FaultContract(typeof(ServiceFault))]
        List<CropListingDto> GetMyCropListings(int farmerId);

        /// <summary>Public marketplace: all Active listings, optional search on crop name / district.</summary>
        [OperationContract, FaultContract(typeof(ServiceFault))]
        List<CropListingDto> GetMarketplaceListings(string searchText);
    }
}
