using System;
using SupplierWebService.DAL;

namespace SupplierWebService
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Create the database, tables and demo data on first run (errors are reported by CheckHealth).
            DbInitializer.EnsureCreated();
        }
    }
}
