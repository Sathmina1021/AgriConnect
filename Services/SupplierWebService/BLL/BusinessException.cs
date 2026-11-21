using System;

namespace SupplierWebService.BLL
{
    /// <summary>A business-rule violation. The service turns it into a SOAP client fault with the same message.</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }
}
