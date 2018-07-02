using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using Gravity.Base;
using Gravity.Extensions;
using Gravity.Globals;
using kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client.DTOs.Attributes;

namespace Gravity.DAL.SQL
{
	public partial class SqlDao
	{
		private Dictionary<Guid, string> GetArtifactGuidsMappingsToColumnNames(Guid[] guids)
		{
			var returnDictionary = new Dictionary<Guid, string>();

			StringBuilder sqlStringBuilder = new StringBuilder(SQLConstants.sqlGetArtifactGuidsMappingToColumnNames);

			foreach (Guid guid in guids)
			{
				sqlStringBuilder.Append("'");
				sqlStringBuilder.Append(guid.ToString());
				sqlStringBuilder.Append("',");
			}

			string sql = sqlStringBuilder.ToString();
			sql = sql.Trim(',');
			sql += ")";

            DataTable dtTable = CreateDBContext().ExecuteSqlStatementAsDataTable(sql);

            if (dtTable.Rows.Count > 0)
			{
				foreach (DataRow dataRow in dtTable.Rows)
				{
					returnDictionary.Add((Guid)dataRow[0], (dataRow[1] as string).Replace(" ", "").Replace("-",""));
				}
			}

			return returnDictionary;
		}

		private Dictionary<int, Guid> GetArtifactIdGuidMappings(int[] artifactIds)
		{
			var returnDictionary = new Dictionary<int, Guid>();

			StringBuilder sqlStringBuilder = new StringBuilder(SQLConstants.sqlGetArtifactGuidMappings);

			foreach (int artifactId in artifactIds)
			{
				sqlStringBuilder.Append("'");
				sqlStringBuilder.Append(artifactId.ToString());
				sqlStringBuilder.Append("',");
			}

			string sql = sqlStringBuilder.ToString();
			sql = sql.Trim(',');
			sql += ")";

			DataTable dtTable = CreateDBContext().ExecuteSqlStatementAsDataTable(sql);

			if (dtTable.Rows.Count > 0)
			{
				foreach (DataRow dataRow in dtTable.Rows)
				{
					returnDictionary.Add((int)dataRow[0], (Guid)dataRow[1]);
				}
			}

			return returnDictionary;
		}


		// Get the multipleObjectFieldArtifactId of the MultipleObject field
		// Get the associativeArtifactTypeID for this field from the Field table
		// search the "f{multipleObjectFieldArtifactId}f{associativeArtifactTypeID}" table for "f{multipleObjectFieldArtifactId}" = artifactId
		// Returns the children's artifact IDs
		private IEnumerable<int> GetMultipleObjectChildrenArtifactIds(int artifactId, Guid multipleObjectFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("MultipleObjectFieldArtifactGuid", multipleObjectFieldArtifactGuid));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = CreateDBContext().ExecuteParameterizedSQLStatementAsReader(SQLConstants.sqlGetMultipleObjectArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private int? GetSingleObjectArtifactId<T>(int artifactId, Guid singleObjectFieldArtifactGuid) where T : BaseDto, new()
		{
			int? singleObjectArtifactId = null;

			if (artifactId != 0)
			{
				Guid tableNameGuid = BaseDto.GetObjectTypeGuid<T>();
				var guids = GetArtifactGuidsMappingsToColumnNames(new Guid[] { tableNameGuid, singleObjectFieldArtifactGuid });
				string tableName = guids[tableNameGuid];
				string columnName = guids[singleObjectFieldArtifactGuid];

				string sql = string.Format(SQLConstants.sqlGetArtifactGuidMappings, columnName, tableName);

				List<SqlParameter> sqlParameters = new List<SqlParameter>();
				sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));

				object singleObjectArtifactIdObj = CreateDBContext().ExecuteSqlStatementAsScalar(sql, sqlParameters.ToArray());
				if (singleObjectArtifactIdObj != System.DBNull.Value)
				{
					singleObjectArtifactId = (int)singleObjectArtifactIdObj;
				}
			}
			return singleObjectArtifactId;
		}

		// Returns the ArtifactIDs of the choices for the given field of the boject with artifactId
		// Finds the CodeTypeID for the field
		// Searches the ZCodeArtifact_XXXXXX table for associations
		private IEnumerable<int> GetChoicesArtifactIds(int artifactId, Guid choiceFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("ChoiceFieldArtifactGuid", choiceFieldArtifactGuid));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = CreateDBContext().ExecuteParameterizedSQLStatementAsReader(SQLConstants.sqlGetChoicesArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private IEnumerable<int> GetChildrenArtifactIds(int artifactId, Guid childrenFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ArtifactID", artifactId));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = CreateDBContext().ExecuteParameterizedSQLStatementAsReader(SQLConstants.sqlGetChildrenArtifactIDs, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private IEnumerable<int> GetChildrenArtifactIdsByParentAndType(int artifactId, int artifactTypeID, Guid childrenFieldArtifactGuid)
		{
			List<SqlParameter> sqlParameters = new List<SqlParameter>();
			sqlParameters.Add(new SqlParameter("ParentArtifactID", artifactId));
			sqlParameters.Add(new SqlParameter("ArtifactTypeID", artifactTypeID));

			List<int> returnList = new List<int>();
			using (SqlDataReader reader = CreateDBContext().ExecuteParameterizedSQLStatementAsReader(SQLConstants.sqlGetChildrenArtifactIDsByParentAndType, sqlParameters))
			{
				while (reader.Read() == true)
				{
					returnList.Add(reader.GetInt32(0));
				}
			}

			return returnList;
		}

		private int GetArtifactTypeIdByArtifactGuid(Guid artifactGuid)
		{
			SqlParameter[] sqlParameters = new SqlParameter[] { new SqlParameter("ArtifactGuid", artifactGuid) };

			int returnId;
			returnId = (int)CreateDBContext().ExecuteSqlStatementAsScalar(SQLConstants.sqlGetArtifactTypeIdByArtifactGuid, sqlParameters);
			
			return returnId;
		}

		private string GenerateSelectStatementForObject(Guid objectArtifactTypeGuid, int artifactId, Dictionary<Guid, string> guidToNameMappings,
			Dictionary<PropertyInfo, RelativityObjectFieldAttribute> propertyFieldMappings)
		{
			StringBuilder selectBuilder = new StringBuilder();

			selectBuilder.Append("SELECT TOP 1 * ");
			selectBuilder.Append(string.Format("FROM [EDDSDBO].[{0}] ", guidToNameMappings[objectArtifactTypeGuid]));
			selectBuilder.Append("WHERE ArtifactID=");
			selectBuilder.Append(artifactId.ToString());

			return selectBuilder.ToString();
		}

		public T GetRelativityObject<T>(int artifactId, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto, new()
		{
			return GetRelativityObjectWithParent<T>(artifactId, depthLevel, null);
		}

		public T GetRelativityObjectWithParent<T>(int artifactId, ObjectFieldsDepthLevel depthLevel, int? parentArtifactId)
			where T : BaseDto, new()
		{
			T returnObject = new T();

			List<Guid> guidsToMapToName = new List<Guid>();

            // Get the object GUID from the BaseDto object mapping
            Guid artifactTypeGuid = BaseDto.GetObjectTypeGuid<T>();
            guidsToMapToName.Add(artifactTypeGuid);

            var propertyFieldMappings = BaseDto.GetRelativityObjectFieldListInfos<T>();
            guidsToMapToName.AddRange(propertyFieldMappings.Select(x => x.Value.FieldGuid));

            var guidToNameMappings = GetArtifactGuidsMappingsToColumnNames(guidsToMapToName.ToArray());

            Guid parentFieldGuid = BaseDto.GetParentArtifactIdFieldGuid<T>(); // Could be all zeros if not present

            string selectSql = GenerateSelectStatementForObject(artifactTypeGuid, artifactId, guidToNameMappings, propertyFieldMappings);

            DataTable dtTable = CreateDBContext().ExecuteSqlStatementAsDataTable(selectSql);
            if (dtTable.Rows.Count > 0)
            {
            	foreach (DataRow dataRow in dtTable.Rows)
            	{
            		// Set the base fields
            		returnObject.ArtifactId = Convert.ToInt32(dataRow["ArtifactID"]);

            		string columnName;
            		PropertyInfo propertyInfo;
            		RelativityObjectFieldAttribute fieldAttribute;
            		foreach (var fieldPropertyInfoMapping in BaseDto.GetRelativityObjectFieldListInfos<T>())
            		{
            			object newValue = null;

            			propertyInfo = fieldPropertyInfoMapping.Key;
            			fieldAttribute = fieldPropertyInfoMapping.Value;

            			if (fieldAttribute.FieldGuid == parentFieldGuid)
            			{
            				// This is a parent field, set the property to the passed-in parentArtifactId
            				propertyInfo.SetValue(returnObject, parentArtifactId);
            				continue;
            			}

            			columnName = guidToNameMappings.FirstOrDefault(x => x.Key == fieldAttribute.FieldGuid).Value;

            			switch (fieldAttribute.FieldType)
            			{
            				case RdoFieldType.Currency:
            				case RdoFieldType.Decimal:
            					if (dataRow.IsNull(columnName) == true)
            					{
            						newValue = null;
            					}
            					else
            					{
            						newValue = Convert.ToDecimal(dataRow[columnName]);
            					}
                                break;
                            case RdoFieldType.Date:
                                if (dataRow.IsNull(columnName) == true)
                                {
                                    newValue = null;
                                }
                                else
                                {
                                    newValue = Convert.ToDateTime(dataRow[columnName]);
                                }
                                break;
                            case RdoFieldType.File:
                                // TODO: Figure out this here as we may have needed a JOIN on the SELECT, or get the file stuff with new SELECT here, won't be that bad???
                                break;
                            case RdoFieldType.FixedLengthText:
                            case RdoFieldType.LongText:
                                newValue = Convert.ToString(dataRow[columnName]);
                                break;
                            case RdoFieldType.MultipleChoice:
                                var multipleChoices = GetChoicesArtifactIds(artifactId, fieldAttribute.FieldGuid);
                                if (multipleChoices.Count() > 0)
                                {
                                    Type propertyInnerType = propertyInfo.PropertyType.GetEnumerableInnerType();
                                    newValue = this.InvokeGenericMethod(propertyInnerType,nameof(GetChoicesValuesByArtifactIds),multipleChoices);
                                }
                                break;
                            case RdoFieldType.MultipleObject:
                                IEnumerable<int> multipleObjectsArtifactIds = GetMultipleObjectChildrenArtifactIds(artifactId, fieldAttribute.FieldGuid);
                                if(multipleObjectsArtifactIds.Count() > 0)
                                {
                                    newValue = this.InvokeGenericMethod(propertyInfo.PropertyType.GetEnumerableInnerType(), nameof(GetMultipleChildObjectsByArtifactIds), multipleObjectsArtifactIds, depthLevel);
                                }
                                break;
                            case RdoFieldType.SingleChoice:
                                int choiceArtifactId = GetChoicesArtifactIds(artifactId, fieldAttribute.FieldGuid).Single();
                                if (choiceArtifactId > 0)
                                {
                                    newValue = this.InvokeGenericMethod(propertyInfo.PropertyType, nameof(GetChoiceValueByArtifactId), choiceArtifactId);
                                }
                                break;
                            case RdoFieldType.SingleObject:
                                int singleObjectArtifactId = dataRow[columnName] != DBNull.Value ? Convert.ToInt32(dataRow[columnName]) : 0;
                                newValue = this.InvokeGenericMethod(propertyInfo.PropertyType,nameof(GetRelativityObject),singleObjectArtifactId,depthLevel);
                                break;
                            case RdoFieldType.User:
                                int userArtifactId = dataRow[columnName] != DBNull.Value ? Convert.ToInt32(dataRow[columnName]) : 0;
                                newValue = GetUserNamesByArtifactId(userArtifactId);
                                break;
                            case RdoFieldType.WholeNumber:
                                newValue = dataRow[columnName] != DBNull.Value ? Convert.ToInt32(dataRow[columnName]) : 0;
                                break;
                            case RdoFieldType.YesNo:
                                newValue = dataRow[columnName] != DBNull.Value ?
                                    Convert.ToBoolean(dataRow[columnName]) :
                                    newValue;
                                break;
                            //case SharedConstants.FieldTypeCustomListInt:
                            //    newValue = Convert.ToString(dataRow[columnName]).ToListInt(SharedConstants.ListIntSeparatorChar);
                            //    break;
                            //case SharedConstants.FieldTypeByteArray:
                            //    string valueAsString = Convert.ToString(dataRow[columnName]);
                            //    if (valueAsString != null)
                            //    {
                            //        newValue = Convert.FromBase64String(valueAsString);
                            //    }
                            //    break;
                        }

                        propertyInfo.SetValue(returnObject, newValue);
                    }
                }
            }

            if (depthLevel != ObjectFieldsDepthLevel.OnlyParentObject)
            {
                PopulateChildrenRecursively<T>(returnObject, depthLevel);
            }

            return returnObject;
		}

        private List<T> GetChoicesValuesByArtifactIds<T>(List<int> choiceArtifactIds)
        {
            List<T> returnList = new List<T>();

            Type enumType = typeof(T).IsGenericType == true && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)? typeof(T).GetGenericArguments()[0] : typeof(T);
            IEnumerable<MemberInfo> typeMembersCollection = enumType.GetMembers().Where(member => member.GetCustomAttribute<RelativityObjectAttribute>() != null);

            string artifactsStringSequence = string.Join(",", choiceArtifactIds.ToArray());
            string sqlStatement = SQLConstants.sqlGetArtifactGuidMappings + artifactsStringSequence + ")";

            using (SqlDataReader choicesReader = CreateDBContext().ExecuteSQLStatementAsReader(sqlStatement))
            {
                while(choicesReader.Read())
                {
                    MemberInfo choiceTypeMember = typeMembersCollection.Single(member => member.GetCustomAttribute<RelativityObjectAttribute>()?.ObjectTypeGuid == choicesReader.GetGuid(1));
                    T choiceValue = (T)Enum.Parse(enumType, choiceTypeMember.Name);
                    returnList.Add(choiceValue);
                }
            }
                return returnList;
        }

        private T GetChoiceValueByArtifactId<T>(int choiceArtifactId)
        {
            return ((List<T>)this.InvokeGenericMethod(typeof(T), nameof(GetChoicesValuesByArtifactIds), new List<int>() { choiceArtifactId })).Single();
        }

        private User GetUserNamesByArtifactId(int artifactId)
        {
            User user = new User(artifactId); 

            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter("CaseUserArtifactId", artifactId));
            sqlParameters.Add(new SqlParameter("CaseArtifactId", workspaceId));

            DataTable userDataTable = CreateMasterDBContext().ExecuteSqlStatementAsDataTable(SQLConstants.sqlGetUserNamesByIdAndWorkspace, sqlParameters);
            DataRow userData = userDataTable.Rows[0];

            Dictionary<string, string> userFieldAndColumnNamesMapping = GetUserColumnAndFieldNameMapping();

            foreach (PropertyInfo propertyInfo in typeof(User).GetProperties())
            {
                var fieldAttribute = propertyInfo.GetCustomAttribute<FieldAttribute>();
                if(userFieldAndColumnNamesMapping.TryGetValue(fieldAttribute.FieldName, out string columnName) == false)
                {
                    continue;
                }

                //
                int columnIndex = fieldAttribute.Type == kCura.Relativity.Client.FieldType.SingleChoice ? userDataTable.Columns[columnName.Substring(0,columnName.Length - 4)].Ordinal : userDataTable.Columns[columnName].Ordinal;

                switch (fieldAttribute.Type)
                {
                    case kCura.Relativity.Client.FieldType.Currency:
                    case kCura.Relativity.Client.FieldType.Decimal:
                        if (userData.IsNull(propertyInfo.Name) == true)
                        {
                            user.SetValueByPropertyName(propertyInfo.Name, null);
                        }
                        else
                        {
                            user.SetValueByPropertyName(propertyInfo.Name, Convert.ToDecimal(userData[columnIndex]));
                        }
                        break;
                    case kCura.Relativity.Client.FieldType.Date:
                        if (userData.IsNull(columnName) == true)
                        {
                            user.SetValueByPropertyName(propertyInfo.Name, null);
                        }
                        else
                        {
                            user.SetValueByPropertyName(propertyInfo.Name, Convert.ToDateTime(userData[columnIndex]));
                        }
                        break;
                    case kCura.Relativity.Client.FieldType.File:
                        // TODO: Figure out this here as we may have needed a JOIN on the SELECT, or get the file stuff with new SELECT here, won't be that bad???
                        break;
                    case kCura.Relativity.Client.FieldType.FixedLengthText:
                    case kCura.Relativity.Client.FieldType.LongText:
                        user.SetValueByPropertyName(propertyInfo.Name, Convert.ToString(userData[columnIndex]));
                        break;
                    case kCura.Relativity.Client.FieldType.MultipleChoice:
                        //var multipleChoices = GetChoicesArtifactIds(artifactId, fieldAttribute.FieldGuid);
                        //if (multipleChoices.Count() > 0)
                        //{
                        //    Type propertyInnerType = propertyInfo.PropertyType.GetEnumerableInnerType();
                        //    user.SetValueByPropertyName(propertyInfo.Name, this.InvokeGenericMethod(propertyInnerType, nameof(GetChoicesValuesByArtifactIds), multipleChoices));
                        //}
                        break;
                    case kCura.Relativity.Client.FieldType.MultipleObject:
                        //IEnumerable<int> multipleObjectsArtifactIds = GetMultipleObjectChildrenArtifactIds(artifactId, fieldAttribute.FieldGuid);
                        //if (multipleObjectsArtifactIds.Count() > 0)
                        //{
                        //    user.SetValueByPropertyName(propertyInfo.Name, this.InvokeGenericMethod(propertyInfo.PropertyType.GetEnumerableInnerType(), nameof(GetMultipleChildObjectsByArtifactIds), multipleObjectsArtifactIds, depthLevel));
                        //}
                        break;
                    case kCura.Relativity.Client.FieldType.SingleChoice:
                        //int choiceArtifactId = GetChoicesArtifactIds(artifactId, fieldAttribute.FieldGuid).Single();
                        //if (choiceArtifactId > 0)
                        //{
                        //    user.SetValueByPropertyName(propertyInfo.Name, this.InvokeGenericMethod(propertyInfo.PropertyType, nameof(GetChoiceValueByArtifactId), choiceArtifactId));
                        //}
                        break;
                    case kCura.Relativity.Client.FieldType.SingleObject:
                        int singleObjectArtifactId = userData[columnIndex] != DBNull.Value ? Convert.ToInt32(userData[columnIndex]) : 0;
                        user.SetValueByPropertyName(propertyInfo.Name, this.InvokeGenericMethod(propertyInfo.PropertyType, nameof(GetRelativityObject), singleObjectArtifactId, ObjectFieldsDepthLevel.FirstLevelOnly));
                        break;
                    case kCura.Relativity.Client.FieldType.User:
                        int userArtifactId = userData[propertyInfo.Name] != DBNull.Value ? Convert.ToInt32(userData[columnIndex]) : 0;
                        user.SetValueByPropertyName(propertyInfo.Name, GetUserNamesByArtifactId(userArtifactId));
                        break;
                    case kCura.Relativity.Client.FieldType.WholeNumber:
                        user.SetValueByPropertyName(propertyInfo.Name, userData[columnIndex] != DBNull.Value ? Convert.ToInt32(userData[columnIndex]) : 0);
                        break;
                    case kCura.Relativity.Client.FieldType.YesNo:
                        user.SetValueByPropertyName(propertyInfo.Name, userData[columnIndex] != DBNull.Value ?
                            Convert.ToBoolean(userData[columnIndex]) : userData[columnIndex]);
                        break;
                }
            }

            return user;
        }

        private Dictionary<string, string> GetUserColumnAndFieldNameMapping()
        {
            Dictionary<string, string> fieldColumnNamesMapping = new Dictionary<string, string>();

            using (var mappingReader = CreateMasterDBContext().ExecuteSQLStatementAsReader(SQLConstants.sqlGetUserHeaderAndColumnNamesMapping))
            {
                while(mappingReader.Read())
                {
                    fieldColumnNamesMapping.Add(mappingReader[0].ToString(), mappingReader[1].ToString());
                }
            }

            return fieldColumnNamesMapping;
        }

        private List<T> GetMultipleChildObjectsByArtifactIds<T>(IEnumerable<int> multipleObjectsArtifactIds,ObjectFieldsDepthLevel depthLevel) where T : BaseDto, new()
        {
            List<T> multipleObjectsList = new List<T>();

            foreach (int artifactId in multipleObjectsArtifactIds)
            {

                T childObject = (T)this.InvokeGenericMethod(typeof(T),nameof(GetRelativityObject),artifactId,depthLevel);
                multipleObjectsList.Add(childObject);
            }

            return multipleObjectsList;
        }

        internal void PopulateChildrenRecursively<T>(BaseDto baseDto, ObjectFieldsDepthLevel depthLevel)
			where T : BaseDto
		{
			//foreach (var objectPropertyInfo in BaseDto.GetRelativityMultipleObjectPropertyInfos<T>())
			//{
			//	var propertyInfo = objectPropertyInfo.Key;
			//	var theMultipleObjectAttribute = objectPropertyInfo.Value;

			//	Type childType = objectPropertyInfo.Value.ChildType;

			//	// Note: This repeats the getting of the artifactIDs, which are already gotten in the FieldType.MultipleObject mapping
			//	// But since we do not require folks to map the IList<int> property as well, this one is needed
			//	IEnumerable<int> childArtifactIds = GetMultipleObjectChildrenArtifactIds(baseDto.ArtifactId, theMultipleObjectAttribute.FieldGuid);

			//	MethodInfo method = GetType().GetMethod("GetRelativityObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });

			//	// Enhancement: Make this faster one day by allowing for multiple artifactIds to be passed
			//	var listType = typeof(List<>).MakeGenericType(theMultipleObjectAttribute.ChildType);
			//	IList returnList = (IList)Activator.CreateInstance(listType);

			//	foreach (int childArtifactId in childArtifactIds)
			//	{
			//		object childArtifact = method.Invoke(this, new object[] { childArtifactId, depthLevel });
			//		returnList.Add(childArtifact);
			//	}

			//	propertyInfo.SetValue(baseDto, returnList);
			//}

			//foreach (var objectPropertyInfo in BaseDto.GetRelativitySingleObjectPropertyInfos<T>())
			//{
			//	var propertyInfo = objectPropertyInfo.Key;
			//	var theSingleObjectAttribute = objectPropertyInfo.Value;

			//	Type childType = objectPropertyInfo.Value.ChildType;
			//	var singleObject = Activator.CreateInstance(childType);

			//	// Note: This repeats the getting of the artifactIDs, which are already gotten in the FieldType.MultipleObject mapping
			//	// But since we do not require folks to map the IList<int> property as well, this one is needed
			//	int? childArtifactId = GetSingleObjectArtifactId<T>(baseDto.ArtifactId, theSingleObjectAttribute.FieldGuid);

			//	if (childArtifactId.HasValue == true)
			//	{
			//		MethodInfo method = GetType().GetMethod("GetRelativityObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { childType });
			//		singleObject = method.Invoke(this, new object[] { childArtifactId, depthLevel });

			//		propertyInfo.SetValue(baseDto, singleObject);
			//	}
			//}
			
			//foreach (var childPropertyInfo in BaseDto.GetRelativityObjectChildrenListInfos<T>())
			//{
			//	var propertyInfo = childPropertyInfo.Key;
			//	var theChildAttribute = childPropertyInfo.Value;

			//	Guid parentFieldGuid = theChildAttribute.ChildType.GetRelativityObjectGuidForParentField();

			//	int currentChildArtifactTypeID = GetArtifactTypeIdByArtifactGuid(childPropertyInfo.Value.ChildType.GetCustomAttribute<RelativityObjectAttribute>().ObjectTypeGuid);

			//	var childArtifactIds = GetChildrenArtifactIdsByParentAndType(baseDto.ArtifactId, currentChildArtifactTypeID, parentFieldGuid);

			//	MethodInfo method = GetType().GetMethod("GetRelativityObjectWithParent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(new Type[] { theChildAttribute.ChildType });

			//	var listType = typeof(List<>).MakeGenericType(theChildAttribute.ChildType);
			//	IList returnList = (IList)Activator.CreateInstance(listType);

			//	foreach (int childArtifactId in childArtifactIds)
			//	{
			//		object childArtifact = method.Invoke(this, new object[] { childArtifactId, depthLevel, baseDto.ArtifactId });
			//		returnList.Add(childArtifact);
			//	}

			//	propertyInfo.SetValue(baseDto, returnList);
			//}
		}
	}
}
