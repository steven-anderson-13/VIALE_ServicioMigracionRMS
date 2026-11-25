using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SAPCONN
{
    public static OdbcConnection GetConnection()
    {
        string connStr = ConfigurationManager.AppSettings["Sap.OdbcConnection"];

        var conn = new OdbcConnection(connStr);
        conn.Open();

        return conn;
    }
}