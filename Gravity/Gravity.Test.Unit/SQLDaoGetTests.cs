using NUnit.Framework;
using Gravity.DAL.SQL;
using Gravity.Test.TestClasses;
using Gravity.Base;
using System;
using Relativity.API;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gravity.Test.Unit
{
    [TestClass]
    public class SQLDaoGetTests
    {
        private int _workspaceId = 1067048;

        private IHelper helper;
        private SqlDao sqlDao;

        private void Execute_TestFixtureSetup()
        {
            try
            {
                helper = new AppConfigConnectionHelper();

                sqlDao = new SqlDao(helper, _workspaceId);
            }
            catch (Exception ex)
            {
                throw new Exception("Error encountered while creating new SQL DAO.", ex);
            }
            finally
            {
                Console.WriteLine("Created new SQL DAO .....");
            }
        }

        [TestMethod]
        public void GetRelativityObjectFullyRecursiveTest()
        {
            Execute_TestFixtureSetup();

            int artifactId = 1040359;
            GravityLevelOne _object = sqlDao.GetRelativityObject<GravityLevelOne>(artifactId, ObjectFieldsDepthLevel.FullyRecursive);
        }
    }
}
