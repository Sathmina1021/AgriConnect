using System;
using System.Collections.Generic;
using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService.BLL
{
    public class RequestBLL
    {
        private readonly RequestDAL _dal = new RequestDAL();

        public List<SupplyRequest> GetOpenRequests(int supplierId)
        {
            return _dal.GetOpenForSupplier(supplierId);
        }

        /// <summary>Called by the Farmer service when a farmer submits a new supply request.</summary>
        public void Receive(SupplyRequest r)
        {
            if (r == null) throw new BusinessException("Supply request is required.");
            if (r.RequestId <= 0) throw new BusinessException("RequestId is required.");
            if (string.IsNullOrWhiteSpace(r.FarmerName)) throw new BusinessException("Farmer name is required.");
            if (string.IsNullOrWhiteSpace(r.ItemName)) throw new BusinessException("Item name is required.");
            if (r.Quantity <= 0) throw new BusinessException("Quantity must be greater than 0.");
            if (string.IsNullOrWhiteSpace(r.DeliveryLocation)) throw new BusinessException("Delivery location is required.");
            if (string.IsNullOrWhiteSpace(r.Region)) throw new BusinessException("Region is required.");
            if (r.PreferredDate == default(DateTime)) throw new BusinessException("Preferred date is required.");
            if (_dal.GetById(r.RequestId) != null) throw new BusinessException("This supply request was already received.");
            _dal.Insert(r);
        }
    }
}
