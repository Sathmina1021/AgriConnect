using System;
using System.Collections.Generic;
using SupplierWebService.DAL;
using SupplierWebService.Models;

namespace SupplierWebService.BLL
{
    public class DeliveryBLL
    {
        private static readonly string[] Flow = { "Pending", "Dispatched", "InTransit", "Delivered" };

        private readonly DeliveryDAL _dal = new DeliveryDAL();

        public List<Delivery> GetDeliveries(int supplierId)
        {
            return _dal.GetBySupplier(supplierId);
        }

        /// <summary>A delivery can only move one step forward: Pending -> Dispatched -> InTransit -> Delivered.</summary>
        public void UpdateStatus(int deliveryId, string newStatus)
        {
            var d = _dal.GetById(deliveryId);
            if (d == null) throw new BusinessException("Delivery not found.");
            int current = Array.IndexOf(Flow, d.Status);
            int target = Array.IndexOf(Flow, newStatus);
            if (target < 0) throw new BusinessException("Unknown delivery status. Use: " + string.Join(", ", Flow) + ".");
            if (target != current + 1)
                throw new BusinessException("A delivery that is " + d.Status + " cannot change to " + newStatus + ".");
            _dal.SetStatus(deliveryId, newStatus);
        }
    }
}
