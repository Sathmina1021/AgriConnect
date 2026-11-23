using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using Farmer.Contracts;
using Farmer.Service.Data;
using Farmer.Service.Integration;
using Farmer.Service.Security;
using Farmer.Service.Validation;

namespace Farmer.Service
{
    /// <summary>
    /// Business-logic layer of the Farmer role: validation, workflow-state enforcement,
    /// persistence through <see cref="IFarmerRepository"/> and integration with the Supplier,
    /// Agriculture Officer and Administration services through gateways.
    /// A new instance is created per call (stateless, scalable).
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class FarmerService : IFarmerService
    {
        private readonly IFarmerRepository _repo;
        private readonly ISupplierGateway _supplier;
        private readonly IOfficerGateway _officer;
        private readonly IAdminGateway _admin;

        /// <summary>Used by WCF (parameterless constructor required) - wires the default implementations.</summary>
        public FarmerService()
            : this(new FarmerRepository(), GatewayFactory.CreateSupplierGateway(),
                   GatewayFactory.CreateOfficerGateway(), GatewayFactory.CreateAdminGateway())
        {
        }

        /// <summary>Constructor injection keeps the class unit-testable.</summary>
        public FarmerService(IFarmerRepository repo, ISupplierGateway supplier, IOfficerGateway officer, IAdminGateway admin)
        {
            _repo = repo;
            _supplier = supplier;
            _officer = officer;
            _admin = admin;
        }

        // =====================================================================
        // 1. Registration & login
        // =====================================================================
        public FarmerDto RegisterFarmer(FarmerRegistration registration)
        {
            return Execute("RegisterFarmer", () =>
            {
                List<string> errors = InputValidator.ValidateRegistration(registration);
                if (errors.Count > 0) throw ValidationFault(errors);

                registration.Email = registration.Email.Trim().ToLowerInvariant();
                registration.FullName = registration.FullName.Trim();
                registration.Phone = registration.Phone.Replace(" ", "").Replace("-", "");

                if (_repo.EmailExists(registration.Email))
                    throw ValidationFault("An account with this email already exists.");

                string salt = PasswordHasher.GenerateSalt();
                string hash = PasswordHasher.Hash(registration.Password, salt);

                int id;
                try { id = _repo.InsertFarmer(registration, hash, salt); }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)   // unique-key race
                {
                    throw ValidationFault("An account with this email already exists.");
                }

                FarmerDto farmer = _repo.GetFarmerById(id);
                SafeNotify(() => _admin.LogActivity("Farmer", id, "Registered", farmer.Email));
                return farmer;
            });
        }

        public LoginResult Login(LoginCredentials credentials)
        {
            return Execute("Login", () =>
            {
                if (credentials == null || string.IsNullOrWhiteSpace(credentials.Email) || string.IsNullOrEmpty(credentials.Password))
                    throw ValidationFault("Email and password are required.");

                FarmerAccount account = _repo.GetAccountByEmail(credentials.Email.Trim().ToLowerInvariant());
                bool ok = account != null && account.IsActive &&
                          PasswordHasher.Verify(credentials.Password, account.PasswordSalt, account.PasswordHash);

                if (!ok)
                    return new LoginResult { Success = false, Message = "Invalid email or password." };   // same message for both cases

                SafeNotify(() => _admin.LogActivity("Farmer", account.Profile.FarmerId, "Login", "Successful login"));
                return new LoginResult { Success = true, Message = "Welcome, " + account.Profile.FullName + "!", Profile = account.Profile };
            });
        }

        // =====================================================================
        // 2. Dashboard
        // =====================================================================
        public DashboardSummary GetDashboard(int farmerId)
        {
            return Execute("GetDashboard", () =>
            {
                FarmerDto farmer = RequireFarmer(farmerId);
                DashboardSummary summary = _repo.GetDashboardCounts(farmerId);
                summary.FarmerName = farmer.FullName;
                summary.RecentRequests = _repo.GetSupplyRequestsByFarmer(farmerId, null, 5);
                summary.RecentListings = _repo.GetCropListingsByFarmer(farmerId, 5);
                return summary;
            });
        }

        // =====================================================================
        // 3. Supply requests
        // =====================================================================
        public SupplyRequestDto CreateSupplyRequest(SupplyRequestInput input)
        {
            return Execute("CreateSupplyRequest", () =>
            {
                List<string> errors = InputValidator.ValidateSupplyRequest(input);
                if (errors.Count > 0) throw ValidationFault(errors);

                FarmerDto farmer = RequireFarmer(input.FarmerId);
                input.ItemName = input.ItemName.Trim();
                input.DeliveryLocation = input.DeliveryLocation.Trim();

                int requestId = _repo.InsertSupplyRequest(input);
                SupplyRequestDto request = _repo.GetSupplyRequestById(requestId);

                // Forward to the other roles; failures are recorded (flags) and can be retried via RefreshRequestStatus.
                bool supplierOk = SafeNotify(() => _supplier.NotifyNewSupplyRequest(request, farmer));
                bool officerOk = SafeNotify(() => _officer.SubmitForVerification(request, farmer));
                _repo.UpdateRequestNotificationFlags(requestId, supplierOk, officerOk);

                SafeNotify(() => _admin.LogActivity("Farmer", farmer.FarmerId, "SupplyRequestCreated", "Request #" + requestId + " " + request.ItemName));
                return _repo.GetSupplyRequestById(requestId);
            });
        }

        public List<SupplyRequestDto> GetSupplyRequests(int farmerId, string statusFilter)
        {
            return Execute("GetSupplyRequests", () =>
            {
                RequireFarmer(farmerId);
                if (!string.IsNullOrWhiteSpace(statusFilter) && !IsKnownRequestStatus(statusFilter))
                    throw ValidationFault("Unknown status filter '" + statusFilter + "'.");
                return _repo.GetSupplyRequestsByFarmer(farmerId, string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter, null);
            });
        }

        public SupplyRequestDto GetSupplyRequest(int farmerId, int requestId)
        {
            return Execute("GetSupplyRequest", () => LoadOwnedRequest(farmerId, requestId));
        }

        public SupplyRequestDto RefreshRequestStatus(int farmerId, int requestId)
        {
            return Execute("RefreshRequestStatus", () =>
            {
                SupplyRequestDto request = LoadOwnedRequest(farmerId, requestId);
                if (request.Status != RequestStatus.Pending) return request;   // nothing to pull

                FarmerDto farmer = RequireFarmer(farmerId);

                // Retry any notification that failed earlier (e.g. teammate's service was offline).
                bool supplierOk = request.SupplierNotified || SafeNotify(() => _supplier.NotifyNewSupplyRequest(request, farmer));
                bool officerOk = request.OfficerNotified || SafeNotify(() => _officer.SubmitForVerification(request, farmer));
                if (supplierOk != request.SupplierNotified || officerOk != request.OfficerNotified)
                    _repo.UpdateRequestNotificationFlags(requestId, supplierOk, officerOk);

                OfficerDecisionResult decision = null;
                try { decision = _officer.GetDecision(requestId); }
                catch (Exception ex) { ServiceLog.Warn("Officer decision lookup failed: " + ex.Message); }

                if (decision != null && decision.HasDecision)
                {
                    _repo.TryChangeRequestStatus(requestId, RequestStatus.Pending,
                        decision.Approved ? RequestStatus.Approved : RequestStatus.Rejected,
                        decision.OfficerId, decision.Remarks);
                }
                return _repo.GetSupplyRequestById(requestId);
            });
        }

        public SupplyRequestDto CancelSupplyRequest(int farmerId, int requestId)
        {
            return Execute("CancelSupplyRequest", () =>
            {
                SupplyRequestDto request = LoadOwnedRequest(farmerId, requestId);
                if (request.Status != RequestStatus.Pending && request.Status != RequestStatus.Approved)
                    throw WorkflowFault("A request in status '" + request.Status + "' cannot be cancelled.");

                if (!_repo.TryChangeRequestStatus(requestId, request.Status, RequestStatus.Cancelled, null, null))
                    throw WorkflowFault("The request changed state while cancelling. Please refresh and try again.");

                SafeNotify(() => _admin.LogActivity("Farmer", farmerId, "SupplyRequestCancelled", "Request #" + requestId));
                return _repo.GetSupplyRequestById(requestId);
            });
        }

        public SupplyRequestDto ApplyOfficerDecision(OfficerDecisionInput decision)
        {
            return Execute("ApplyOfficerDecision", () =>
            {
                if (decision == null) throw ValidationFault("Decision data is required.");

                string expectedKey = ConfigurationManager.AppSettings["IntegrationKey"];
                if (!string.IsNullOrEmpty(expectedKey) && !string.Equals(expectedKey, decision.IntegrationKey, StringComparison.Ordinal))
                    throw new FaultException<ServiceFault>(
                        new ServiceFault { ErrorCode = "Unauthorized", Message = "Invalid integration key." },
                        new FaultReason("Invalid integration key."));

                if (!string.IsNullOrEmpty(decision.Remarks) && decision.Remarks.Length > 300)
                    throw ValidationFault("Remarks are too long (max 300).");

                SupplyRequestDto request = _repo.GetSupplyRequestById(decision.RequestId);
                if (request == null) throw NotFoundFault("Supply request");

                string target = decision.Approved ? RequestStatus.Approved : RequestStatus.Rejected;
                if (request.Status == target) return request;                    // idempotent: same decision again
                if (request.Status != RequestStatus.Pending)
                    throw WorkflowFault("Request #" + request.RequestId + " is '" + request.Status + "' and can no longer be decided.");

                if (!_repo.TryChangeRequestStatus(request.RequestId, RequestStatus.Pending, target, decision.OfficerId, decision.Remarks))
                    throw WorkflowFault("The request changed state concurrently. Please retry.");

                SafeNotify(() => _admin.LogActivity("Officer", decision.OfficerId, "RequestDecision", "Request #" + request.RequestId + " -> " + target));
                return _repo.GetSupplyRequestById(request.RequestId);
            });
        }

        // =====================================================================
        // 4. Quotations & order confirmation
        // =====================================================================
        public List<QuotationDto> GetQuotationsForRequest(int requestId)
        {
            return Execute("GetQuotationsForRequest", () =>
            {
                SupplyRequestDto request = _repo.GetSupplyRequestById(requestId);
                if (request == null) throw NotFoundFault("Supply request");
                RequireApprovedForQuotations(request);
                return LoadQuotations(request);
            });
        }

        public ConfirmedOrderDto ConfirmOrder(OrderConfirmationInput input)
        {
            return Execute("ConfirmOrder", () =>
            {
                if (input == null || input.FarmerId <= 0 || input.RequestId <= 0 || input.QuotationId <= 0)
                    throw ValidationFault("FarmerId, RequestId and QuotationId are required.");

                SupplyRequestDto request = LoadOwnedRequest(input.FarmerId, input.RequestId);
                RequireApprovedForQuotations(request);       // workflow gate: officer approval first

                // Never trust prices sent by the client: re-read the quotation from the supplier side.
                QuotationDto quote = LoadQuotations(request).FirstOrDefault(q => q.QuotationId == input.QuotationId);
                if (quote == null) throw NotFoundFault("Quotation");
                if (!quote.MeetsStock)
                    throw WorkflowFault(quote.SupplierName + " only has " + quote.StockQuantity + " in stock but " + request.Quantity + " were requested.");

                var order = new ConfirmedOrderDto
                {
                    RequestId = request.RequestId,
                    FarmerId = request.FarmerId,
                    SupplierId = quote.SupplierId,
                    SupplierName = quote.SupplierName,
                    QuotationId = quote.QuotationId,
                    Quantity = request.Quantity,
                    UnitPrice = quote.UnitPrice,
                    TotalPrice = quote.UnitPrice * request.Quantity,
                    EstimatedDeliveryDate = quote.EstimatedDeliveryDate
                };

                int orderId;
                try { orderId = _repo.CreateConfirmedOrder(order); }
                catch (InvalidOperationException ex) { throw WorkflowFault(ex.Message); }
                catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
                {
                    throw WorkflowFault("An order already exists for this request.");
                }

                ConfirmedOrderDto saved = _repo.GetConfirmedOrderById(orderId);

                bool supplierOk = SafeNotify(() => _supplier.NotifyOrderConfirmed(saved));
                bool officerOk = SafeNotify(() => _officer.NotifyOrderConfirmed(saved));
                _repo.UpdateOrderNotificationFlags(orderId, supplierOk, officerOk);
                SafeNotify(() => _admin.LogActivity("Farmer", input.FarmerId, "OrderConfirmed", "Order #" + orderId + " total " + saved.TotalPrice));

                return _repo.GetConfirmedOrderById(orderId);
            });
        }

        public List<ConfirmedOrderDto> GetConfirmedOrders(int farmerId)
        {
            return Execute("GetConfirmedOrders", () =>
            {
                RequireFarmer(farmerId);
                return _repo.GetConfirmedOrdersByFarmer(farmerId);
            });
        }

        // =====================================================================
        // 5. Crop listings & marketplace
        // =====================================================================
        public CropListingDto CreateCropListing(CropListingInput input)
        {
            return Execute("CreateCropListing", () =>
            {
                List<string> errors = InputValidator.ValidateCropListing(input);
                if (errors.Count > 0) throw ValidationFault(errors);
                RequireFarmer(input.FarmerId);
                NormalizeListing(input);

                int id = _repo.InsertCropListing(input);
                SafeNotify(() => _admin.LogActivity("Farmer", input.FarmerId, "CropListingCreated", input.CropName));
                return _repo.GetCropListingById(id);
            });
        }

        public CropListingDto UpdateCropListing(CropListingInput input)
        {
            return Execute("UpdateCropListing", () =>
            {
                List<string> errors = InputValidator.ValidateCropListing(input);
                if (errors.Count > 0) throw ValidationFault(errors);

                CropListingDto existing = LoadOwnedListing(input.FarmerId, input.ListingId);
                if (existing.Status != ListingStatus.Active)
                    throw WorkflowFault("Closed listings cannot be edited. Create a new listing instead.");

                NormalizeListing(input);
                if (!_repo.UpdateCropListing(input)) throw NotFoundFault("Crop listing");
                return _repo.GetCropListingById(input.ListingId);
            });
        }

        public CropListingDto CloseCropListing(int farmerId, int listingId)
        {
            return Execute("CloseCropListing", () =>
            {
                CropListingDto existing = LoadOwnedListing(farmerId, listingId);
                if (existing.Status != ListingStatus.Closed)
                {
                    _repo.SetListingStatus(listingId, farmerId, ListingStatus.Closed);
                    SafeNotify(() => _admin.LogActivity("Farmer", farmerId, "CropListingClosed", existing.CropName));
                }
                return _repo.GetCropListingById(listingId);
            });
        }

        public void DeleteCropListing(int farmerId, int listingId)
        {
            Execute<object>("DeleteCropListing", () =>
            {
                LoadOwnedListing(farmerId, listingId);
                if (!_repo.DeleteCropListing(listingId, farmerId)) throw NotFoundFault("Crop listing");
                SafeNotify(() => _admin.LogActivity("Farmer", farmerId, "CropListingDeleted", "Listing #" + listingId));
                return null;
            });
        }

        public List<CropListingDto> GetMyCropListings(int farmerId)
        {
            return Execute("GetMyCropListings", () =>
            {
                RequireFarmer(farmerId);
                return _repo.GetCropListingsByFarmer(farmerId, null);
            });
        }

        public List<CropListingDto> GetMarketplaceListings(string searchText)
        {
            return Execute("GetMarketplaceListings", () => _repo.SearchActiveListings(searchText));
        }

        // =====================================================================
        // Private helpers
        // =====================================================================
        private static bool IsKnownRequestStatus(string s)
        {
            return s == RequestStatus.Pending || s == RequestStatus.Approved || s == RequestStatus.Rejected
                || s == RequestStatus.Confirmed || s == RequestStatus.Cancelled;
        }

        private FarmerDto RequireFarmer(int farmerId)
        {
            if (farmerId <= 0) throw ValidationFault("FarmerId is required.");
            FarmerDto farmer = _repo.GetFarmerById(farmerId);
            if (farmer == null) throw NotFoundFault("Farmer");
            return farmer;
        }

        /// <summary>Returns the request only if it exists AND belongs to the farmer (otherwise NotFound - no information leak).</summary>
        private SupplyRequestDto LoadOwnedRequest(int farmerId, int requestId)
        {
            SupplyRequestDto r = requestId > 0 ? _repo.GetSupplyRequestById(requestId) : null;
            if (r == null || r.FarmerId != farmerId) throw NotFoundFault("Supply request");
            return r;
        }

        private CropListingDto LoadOwnedListing(int farmerId, int listingId)
        {
            CropListingDto l = listingId > 0 ? _repo.GetCropListingById(listingId) : null;
            if (l == null || l.FarmerId != farmerId) throw NotFoundFault("Crop listing");
            return l;
        }

        private static void NormalizeListing(CropListingInput i)
        {
            i.CropName = i.CropName.Trim();
            i.Unit = i.Unit.Trim().ToLowerInvariant();
            i.ContactInfo = i.ContactInfo.Trim();
            i.Notes = string.IsNullOrWhiteSpace(i.Notes) ? null : i.Notes.Trim();
        }

        /// <summary>Workflow rule: quotations can only be viewed/confirmed after the Agriculture Officer approved the request.</summary>
        private static void RequireApprovedForQuotations(SupplyRequestDto request)
        {
            if (request.Status == RequestStatus.Approved) return;
            string hint = request.Status == RequestStatus.Pending ? " Wait for the Agriculture Officer's approval first."
                        : request.Status == RequestStatus.Confirmed ? " An order was already confirmed for it."
                        : "";
            throw WorkflowFault("Request #" + request.RequestId + " is '" + request.Status + "'; quotations are only available for Approved requests." + hint);
        }

        /// <summary>Gets quotations from the Supplier service; falls back to clearly-flagged placeholder data when it is unavailable.</summary>
        private List<QuotationDto> LoadQuotations(SupplyRequestDto request)
        {
            List<QuotationDto> quotes;
            try
            {
                quotes = _supplier.GetQuotations(request) ?? new List<QuotationDto>();
            }
            catch (Exception ex)
            {
                ServiceLog.Warn("Supplier quotations unavailable, using placeholder data. " + ex.Message);
                quotes = PlaceholderQuotationProvider.Generate(request);
            }

            foreach (QuotationDto q in quotes)
            {
                q.RequestId = request.RequestId;
                if (q.TotalPrice <= 0) q.TotalPrice = q.UnitPrice * request.Quantity;
                if (q.EstimatedDeliveryDate == default(DateTime)) q.EstimatedDeliveryDate = DateTime.Today.AddDays(q.DeliveryDays);
                q.MeetsStock = q.StockQuantity >= request.Quantity;
                q.MeetsPreferredDate = q.EstimatedDeliveryDate.Date <= request.PreferredDate.Date;
            }
            return quotes.OrderBy(q => q.TotalPrice).ToList();
        }

        private static bool SafeNotify(Func<bool> call)
        {
            try { return call(); }
            catch (Exception ex)
            {
                ServiceLog.Warn("Integration call failed: " + ex.Message);
                return false;
            }
        }

        // ---------- fault & error handling ----------
        private T Execute<T>(string operation, Func<T> body)
        {
            try
            {
                return body();
            }
            catch (FaultException)
            {
                throw;                       // already a well-formed SOAP fault
            }
            catch (SqlException ex)
            {
                ServiceLog.Error(operation + " - database error", ex);
                throw MakeFault("DatabaseError", "The database is currently unavailable. Please try again shortly.");
            }
            catch (Exception ex)
            {
                ServiceLog.Error(operation + " - unexpected error", ex);
                throw MakeFault("UnexpectedError", "An unexpected error occurred while processing your request.");
            }
        }

        private static FaultException<ServiceFault> MakeFault(string code, string message, List<string> details = null)
        {
            var fault = new ServiceFault { ErrorCode = code, Message = message, ValidationErrors = details ?? new List<string>() };
            return new FaultException<ServiceFault>(fault, new FaultReason(message));
        }

        private static FaultException<ServiceFault> ValidationFault(List<string> errors)
        {
            return MakeFault("ValidationError", "Please correct the highlighted input: " + string.Join(" ", errors), errors);
        }

        private static FaultException<ServiceFault> ValidationFault(string error)
        {
            return ValidationFault(new List<string> { error });
        }

        private static FaultException<ServiceFault> NotFoundFault(string what)
        {
            return MakeFault("NotFound", what + " was not found.");
        }

        private static FaultException<ServiceFault> WorkflowFault(string message)
        {
            return MakeFault("WorkflowViolation", message);
        }
    }
}
