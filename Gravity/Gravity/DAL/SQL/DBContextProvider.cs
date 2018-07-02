using kCura.Data.RowDataGateway;
using Relativity.API;
using kCura.Relativity.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.DAL.SQL
{
    public class DBContextProvider
    {
        private readonly string sqlServerHostName = ConfigurationManager.AppSettings["SQLServerHostName"];
        private readonly string sqlServerUsername = ConfigurationManager.AppSettings["SQLServerUsername"];
        private readonly string sqlServerPassword = ConfigurationManager.AppSettings["SQLServerPassword"];

        public IDBContext GetDBContext(int caseID)
        {
            if (caseID < 0)
            {
                return new DBContext(new Context(sqlServerHostName, "EDDS", sqlServerUsername, sqlServerPassword));
            }
            else
            {
                return new DBContext(new Context(sqlServerHostName, "EDDS" + caseID.ToString(), sqlServerUsername, sqlServerPassword));
            }
        }

        public void Dispose()
        {
            this.Dispose();
        }

    }
}
