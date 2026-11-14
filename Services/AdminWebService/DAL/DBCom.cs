using System;
using System.Data;
using System.Data.SqlClient;

namespace AdminWebService.DAL
{
    // Data Access Layer Class (Based on Database Programming Lecture Notes)
    public class DBCom
    {
        public static int ExecNonQuery(string conString, string query)
        {
            SqlConnection con = new SqlConnection(conString);
            try
            {
                SqlCommand com = new SqlCommand(query, con);
                con.Open();
                return com.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        public static DataSet ExecSelectQuery(string conString, string query)
        {
            SqlConnection con = new SqlConnection(conString);
            try
            {
                SqlDataAdapter ad = new SqlDataAdapter(query, con);
                con.Open();
                DataSet ds = new DataSet();
                ad.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }
    }
}
