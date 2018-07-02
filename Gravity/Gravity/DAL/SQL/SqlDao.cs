using Relativity.API;
using Gravity.Globals;
using Gravity.Utils;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao
	{
		protected int workspaceId;
		protected IHelper helper;

		protected IDBContext CreateDBContext()
		{
			return helper.GetDBContext(workspaceId);
		}

        protected IDBContext CreateMasterDBContext()
        {
            return helper.GetDBContext(-1);
        }

		private InvokeWithRetryService invokeWithRetryService;

		public SqlDao(IHelper helper, int workspaceId, InvokeWithRetrySettings invokeWithRetrySettings = null)
		{
			this.helper = helper;
			this.workspaceId = workspaceId;

			if (invokeWithRetrySettings == null)
			{
				InvokeWithRetrySettings defaultSettings = new InvokeWithRetrySettings(SharedConstants.retryAttempts, SharedConstants.sleepTimeInMiliseconds);
				this.invokeWithRetryService = new InvokeWithRetryService(defaultSettings);
			}
			else
			{
				this.invokeWithRetryService = new InvokeWithRetryService(invokeWithRetrySettings);
			}
		}
	}
}