using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using Farmer.Service.Integration;

namespace Farmer.Service
{
    /// <summary>
    /// Self-hosts the Farmer WCF SOAP service in a console window (no IIS needed).
    /// Run it (Ctrl+F5), then browse to the base address + "?wsdl".
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            Console.Title = "AgriConnect - Farmer SOAP Service";
            Console.WriteLine("AgriConnect Farmer service starting...");
            Console.WriteLine("Integrations: " + (GatewayFactory.UseMocks ? "MOCK (standalone testing)" : "REAL SOAP endpoints"));

            try
            {
                using (var host = new ServiceHost(typeof(FarmerService)))
                {
                    host.Open();
                    Console.WriteLine();
                    foreach (ServiceEndpoint ep in host.Description.Endpoints)
                        Console.WriteLine("Endpoint : {0} ({1})", ep.Address.Uri, ep.Binding.Name);
                    foreach (Uri baseAddress in host.BaseAddresses)
                        Console.WriteLine("WSDL     : {0}?wsdl", baseAddress);
                    Console.WriteLine();
                    Console.WriteLine("Service is running. Press ENTER to stop.");
                    Console.ReadLine();
                    host.Close();
                }
                return 0;
            }
            catch (AddressAccessDeniedException)
            {
                Console.WriteLine("ACCESS DENIED for the HTTP address. Run Visual Studio as Administrator once, or use a base address");
                Console.WriteLine("under http://localhost:8733/Design_Time_Addresses/ (pre-reserved for normal users).");
            }
            catch (AddressAlreadyInUseException)
            {
                Console.WriteLine("The address/port is already in use. Close the other instance of the service and try again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service failed to start: " + ex.Message);
            }
            Console.WriteLine("Press ENTER to exit.");
            Console.ReadLine();
            return 1;
        }
    }
}
