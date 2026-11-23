using System;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using Farmer.Client.FarmerServiceReference;

namespace Farmer.Client
{
    /// <summary>
    /// Single place where the UI calls the Farmer SOAP service. It creates the generated proxy
    /// (FarmerServiceClient), closes/aborts it correctly and turns every failure into a friendly message,
    /// so the forms stay free of WCF plumbing and try/catch noise.
    /// </summary>
    internal static class ServiceCaller
    {
        public static bool TryCall<T>(Func<FarmerServiceClient, T> action, out T result)
        {
            result = default(T);
            FarmerServiceClient client = null;
            Cursor previous = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                client = new FarmerServiceClient();
                result = action(client);
                client.Close();
                return true;
            }
            catch (FaultException<ServiceFault> ex)          // business / validation errors from the service
            {
                Abort(client);
                UiHelper.ShowError(FormatFault(ex.Detail));
            }
            catch (EndpointNotFoundException)
            {
                Abort(client);
                UiHelper.ShowError("Cannot reach the Farmer service.\n\nStart the Farmer.Service project (console window) and make sure the address in App.config is correct.");
            }
            catch (TimeoutException)
            {
                Abort(client);
                UiHelper.ShowError("The Farmer service took too long to respond. Please try again.");
            }
            catch (CommunicationException ex)
            {
                Abort(client);
                UiHelper.ShowError("Communication with the Farmer service failed:\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Abort(client);
                UiHelper.ShowError("Unexpected error: " + ex.Message);
            }
            finally
            {
                Cursor.Current = previous;
            }
            return false;
        }

        /// <summary>Overload for operations that return nothing.</summary>
        public static bool TryCall(Action<FarmerServiceClient> action)
        {
            object ignored;
            return TryCall<object>(c => { action(c); return null; }, out ignored);
        }

        private static void Abort(FarmerServiceClient client)
        {
            if (client != null) client.Abort();
        }

        private static string FormatFault(ServiceFault fault)
        {
            if (fault.ValidationErrors != null && fault.ValidationErrors.Any())
                return "Please fix the following:\n- " + string.Join("\n- ", fault.ValidationErrors);
            return fault.Message;
        }
    }
}
