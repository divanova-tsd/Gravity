using Gravity.DAL.SQL;
using Relativity.API;
using System;

namespace Gravity.Test
{
    public class AppConfigConnectionHelper : IHelper
    {
        public void Dispose()
        {
            this.Dispose();
        }

        public IDBContext GetDBContext(int caseID)
        {
            return new DBContextProvider().GetDBContext(caseID);
        }

        public Guid GetGuid(int workspaceID, int artifactID)
        {
            throw new NotImplementedException();
        }

        public ILogFactory GetLoggerFactory()
        {
            throw new NotImplementedException();
        }

        public string GetSchemalessResourceDataBasePrepend(IDBContext context)
        {
            throw new NotImplementedException();
        }

        public IServicesMgr GetServicesManager()
        {
            throw new NotImplementedException();
        }

        public IUrlHelper GetUrlHelper()
        {
            throw new NotImplementedException();
        }

        public string ResourceDBPrepend()
        {
            throw new NotImplementedException();
        }

        public string ResourceDBPrepend(IDBContext context)
        {
            throw new NotImplementedException();
        }
    }
}
