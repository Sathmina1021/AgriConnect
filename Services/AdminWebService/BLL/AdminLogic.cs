using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using AdminWebService.DAL;
using AdminWebService.Models;

namespace AdminWebService.BLL
{
    public class AdminLogic
    {
        private string conStr = ConfigurationManager.ConnectionStrings["AdminDBConn"].ConnectionString;

        public List<UserDTO> GetUsers()
        {
            List<UserDTO> list = new List<UserDTO>();
            DataSet ds = DBCom.ExecSelectQuery(conStr, "SELECT * FROM Users ORDER BY UserId DESC");
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                list.Add(new UserDTO
                {
                    UserId = Convert.ToInt32(row["UserId"]),
                    FullName = row["FullName"].ToString(),
                    Email = row["Email"].ToString(),
                    Role = row["Role"].ToString(),
                    District = row["District"].ToString(),
                    IsActive = Convert.ToBoolean(row["IsActive"])
                });
            }
            return list;
        }

        public bool AddUser(string fullName, string email, string role, string district)
        {
            string sql = "INSERT INTO Users (FullName, Email, Role, District, IsActive) VALUES ('"
                + fullName.Replace("'", "''") + "', '" + email.Replace("'", "''") + "', '"
                + role.Replace("'", "''") + "', '" + district.Replace("'", "''") + "', 1)";
            bool ok = DBCom.ExecNonQuery(conStr, sql) > 0;
            if (ok) AddAuditLog("AdminWebService", "UserCreated", "Created " + role + " account for " + fullName);
            return ok;
        }

        public bool ToggleUserStatus(int userId)
        {
            string sql = "UPDATE Users SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE UserId = " + userId;
            bool ok = DBCom.ExecNonQuery(conStr, sql) > 0;
            if (ok) AddAuditLog("AdminWebService", "UserStatusChanged", "Updated status for User ID #" + userId);
            return ok;
        }

        public List<AuditLogDTO> GetAuditLogs()
        {
            List<AuditLogDTO> list = new List<AuditLogDTO>();
            DataSet ds = DBCom.ExecSelectQuery(conStr, "SELECT TOP 40 * FROM AuditLogs ORDER BY LogId DESC");
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                list.Add(new AuditLogDTO
                {
                    LogId = Convert.ToInt32(row["LogId"]),
                    ServiceName = row["ServiceName"].ToString(),
                    ActionName = row["ActionName"].ToString(),
                    Details = row["Details"].ToString(),
                    LogTime = Convert.ToDateTime(row["LogTime"]).ToString("yyyy-MM-dd HH:mm")
                });
            }
            return list;
        }

        public bool AddAuditLog(string serviceName, string actionName, string details)
        {
            string sql = "INSERT INTO AuditLogs (ServiceName, ActionName, Details) VALUES ('"
                + serviceName.Replace("'", "''") + "', '" + actionName.Replace("'", "''") + "', '"
                + details.Replace("'", "''") + "')";
            return DBCom.ExecNonQuery(conStr, sql) > 0;
        }
    }
}
