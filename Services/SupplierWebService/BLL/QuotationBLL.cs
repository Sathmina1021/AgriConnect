using System.Collections.Generic;
using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService.BLL
{
    public class QuotationBLL
    {
        private readonly QuotationDAL _dal = new QuotationDAL();
        private readonly RequestDAL _requests = new RequestDAL();
        private readonly SupplierDAL _suppliers = new SupplierDAL();

        public List<Quotation> GetMyQuotations(int supplierId)
        {
            return _dal.GetBySupplier(supplierId);
        }

        public int Submit(Quotation q)
        {
            if (q == null) throw new BusinessException("Quotation is required.");
            var supplier = _suppliers.GetById(q.SupplierId);
            if (supplier == null) throw new BusinessException("Supplier not found.");
            var request = _requests.GetById(q.SupplyRequestId);
            if (request == null) throw new BusinessException("Supply request not found.");
            if (request.Status != "Open") throw new BusinessException("This supply request is no longer open.");
            if (!string.Equals(request.Region, supplier.Region, System.StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("This request belongs to another region.");
            if (_dal.Exists(q.SupplyRequestId, q.SupplierId)) throw new BusinessException("You have already quoted this request.");
            if (q.UnitPrice <= 0) throw new BusinessException("Unit price must be greater than 0.");
            if (q.StockAvailable <= 0) throw new BusinessException("Available stock must be greater than 0.");
            if (q.StockAvailable < request.Quantity)
                throw new BusinessException("Available stock (" + q.StockAvailable + ") is less than the requested quantity (" + request.Quantity + ").");
            if (q.DeliveryDays <= 0) throw new BusinessException("Delivery estimate must be at least 1 day.");

            q.TotalPrice = q.UnitPrice * request.Quantity;
            return _dal.Insert(q);
        }

        /// <summary>
        /// Called by the Agriculture Officer service (Approved / Rejected) and the Farmer service (Accepted).
        /// Allowed flow: Submitted -> Approved | Rejected,  Approved -> Accepted | Rejected.
        /// </summary>
        public void UpdateStatus(int quotationId, string newStatus)
        {
            var q = _dal.GetById(quotationId);
            if (q == null) throw new BusinessException("Quotation not found.");

            bool allowed =
                (q.Status == "Submitted" && (newStatus == "Approved" || newStatus == "Rejected")) ||
                (q.Status == "Approved" && (newStatus == "Accepted" || newStatus == "Rejected"));
            if (!allowed)
                throw new BusinessException("A quotation that is " + q.Status + " cannot change to " + newStatus + ".");

            if (newStatus == "Accepted")
            {
                var request = _requests.GetById(q.SupplyRequestId);
                if (request == null || request.Status != "Open") throw new BusinessException("The supply request is already closed.");
                _dal.Accept(q);
            }
            else
            {
                _dal.SetStatus(quotationId, newStatus);
            }
        }
    }
}
