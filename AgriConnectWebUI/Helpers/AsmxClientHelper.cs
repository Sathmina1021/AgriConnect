using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace AgriConnectWebUI.Helpers
{
    // Helper class to invoke ASMX Web Methods over HTTP and read XML responses into DataTable
    public class AsmxClientHelper
    {
        public static DataTable InvokeWebMethodTable(string serviceUrl, string methodName, Dictionary<string, string> parameters)
        {
            string xmlResponse = InvokeWebMethodRaw(serviceUrl, methodName, parameters);
            DataTable dt = new DataTable();

            if (string.IsNullOrEmpty(xmlResponse)) return dt;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            XmlNode root = doc.DocumentElement;
            if (root == null || root.ChildNodes.Count == 0) return dt;

            foreach (XmlNode itemNode in root.ChildNodes)
            {
                if (dt.Columns.Count == 0)
                {
                    foreach (XmlNode propNode in itemNode.ChildNodes)
                    {
                        dt.Columns.Add(propNode.LocalName);
                    }
                }

                DataRow row = dt.NewRow();
                foreach (XmlNode propNode in itemNode.ChildNodes)
                {
                    row[propNode.LocalName] = propNode.InnerText;
                }
                dt.Rows.Add(row);
            }

            return dt;
        }

        public static string InvokeWebMethodScalar(string serviceUrl, string methodName, Dictionary<string, string> parameters)
        {
            string xmlResponse = InvokeWebMethodRaw(serviceUrl, methodName, parameters);
            if (string.IsNullOrEmpty(xmlResponse)) return "";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlResponse);
            return doc.DocumentElement != null ? doc.DocumentElement.InnerText : "";
        }

        private static string InvokeWebMethodRaw(string serviceUrl, string methodName, Dictionary<string, string> parameters)
        {
            try
            {
                string fullUrl = serviceUrl.TrimEnd('/') + "/" + methodName;
                StringBuilder postData = new StringBuilder();

                if (parameters != null)
                {
                    foreach (var kv in parameters)
                    {
                        if (postData.Length > 0) postData.Append("&");
                        postData.Append(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value ?? ""));
                    }
                }

                byte[] byteArray = Encoding.UTF8.GetBytes(postData.ToString());
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                request.Timeout = 4000;

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
