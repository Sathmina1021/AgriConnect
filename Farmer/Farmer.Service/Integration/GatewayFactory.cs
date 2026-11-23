using System;
using System.Configuration;

namespace Farmer.Service.Integration
{
    /// <summary>Chooses mock or real SOAP gateways from App.config (UseMockIntegrations).</summary>
    public static class GatewayFactory
    {
        public static bool UseMocks
        {
            get { return !string.Equals(ConfigurationManager.AppSettings["UseMockIntegrations"], "false", StringComparison.OrdinalIgnoreCase); }
        }

        public static ISupplierGateway CreateSupplierGateway()
        {
            return UseMocks ? (ISupplierGateway)new MockSupplierGateway() : new WcfSupplierGateway();
        }

        public static IOfficerGateway CreateOfficerGateway()
        {
            bool autoDecide = !string.Equals(ConfigurationManager.AppSettings["MockOfficerAutoApprove"], "false", StringComparison.OrdinalIgnoreCase);
            return UseMocks ? (IOfficerGateway)new MockOfficerGateway(autoDecide) : new WcfOfficerGateway();
        }

        public static IAdminGateway CreateAdminGateway()
        {
            return UseMocks ? (IAdminGateway)new MockAdminGateway() : new WcfAdminGateway();
        }
    }
}
