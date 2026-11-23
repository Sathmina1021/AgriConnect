using System.Collections.Generic;
using Farmer.Contracts;

namespace Farmer.Service.Data
{
    /// <summary>Data-access abstraction. All SQL lives behind this interface (never in the service or UI).</summary>
    public interface IFarmerRepository
    {
        // Farmers
        bool EmailExists(string email);
        int InsertFarmer(FarmerRegistration registration, string passwordHash, string passwordSalt);
        FarmerAccount GetAccountByEmail(string email);
        FarmerDto GetFarmerById(int farmerId);

        // Dashboard
        DashboardSummary GetDashboardCounts(int farmerId);

        // Supply requests
        int InsertSupplyRequest(SupplyRequestInput input);
        SupplyRequestDto GetSupplyRequestById(int requestId);
        List<SupplyRequestDto> GetSupplyRequestsByFarmer(int farmerId, string statusFilter, int? top);
        void UpdateRequestNotificationFlags(int requestId, bool supplierNotified, bool officerNotified);
        /// <summary>Changes status only if the row is currently in <paramref name="expectedStatus"/>. Returns false otherwise.</summary>
        bool TryChangeRequestStatus(int requestId, string expectedStatus, string newStatus, int? officerId, string officerRemarks);

        // Orders
        /// <summary>Atomically: Approved -> Confirmed on the request AND insert of the order. Throws InvalidOperationException if state changed.</summary>
        int CreateConfirmedOrder(ConfirmedOrderDto order);
        ConfirmedOrderDto GetConfirmedOrderById(int orderId);
        List<ConfirmedOrderDto> GetConfirmedOrdersByFarmer(int farmerId);
        void UpdateOrderNotificationFlags(int orderId, bool supplierNotified, bool officerNotified);

        // Crop listings
        int InsertCropListing(CropListingInput input);
        bool UpdateCropListing(CropListingInput input);
        CropListingDto GetCropListingById(int listingId);
        List<CropListingDto> GetCropListingsByFarmer(int farmerId, int? top);
        List<CropListingDto> SearchActiveListings(string searchText);
        bool SetListingStatus(int listingId, int farmerId, string status);
        bool DeleteCropListing(int listingId, int farmerId);
    }
}
