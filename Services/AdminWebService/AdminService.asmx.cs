using System.Collections.Generic;
using System.Web.Services;
using AdminWebService.BLL;
using AdminWebService.Models;

namespace AdminWebService
{
    [WebService(Namespace = "http://agriconnect.lk/services/admin")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class AdminService : WebService
    {
        private AdminLogic logic = new AdminLogic();

        [WebMethod]
        public List<UserDTO> GetUsers()
        {
            return logic.GetUsers();
        }

        [WebMethod]
        public bool AddUser(string fullName, string email, string role, string district)
        {
            return logic.AddUser(fullName, email, role, district);
        }

        [WebMethod]
        public bool ToggleUserStatus(int userId)
        {
            return logic.ToggleUserStatus(userId);
        }

        [WebMethod]
        public List<AuditLogDTO> GetAuditLogs()
        {
            return logic.GetAuditLogs();
        }

        [WebMethod]
        public bool AddAuditLog(string serviceName, string actionName, string details)
        {
            return logic.AddAuditLog(serviceName, actionName, details);
        }
    }
}
