﻿using kCura.Relativity.Client;
using kCura.Relativity.Client.DTOs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gravity.Base;
using Gravity.Exceptions;
using Gravity.Extensions;

namespace Gravity.DAL.RSAPI
{
	public partial class RsapiDao
	{
		#region RDO INSERT Protected Stuff
		protected int InsertRdo(RDO newRdo)
		{
			var resultArtifactId = rsapiProvider.CreateSingle(newRdo);

			if (resultArtifactId <= 0)
			{
				throw new ArgumentException("Was not able to insert new RDO with resultInt <= 0, with name " + newRdo.TextIdentifier);
			}

			return resultArtifactId;
		}

		protected void InsertUpdateFileFields(BaseDto objectToInsert, int parentId)
		{
			foreach (var propertyInfo in objectToInsert.GetType().GetProperties().Where(c => c.GetCustomAttribute<RelativityObjectFieldAttribute>()?.FieldType == RdoFieldType.File))
			{
				RelativityFile relativityFile = propertyInfo.GetValue(objectToInsert) as RelativityFile;
				InsertUpdateFileField(relativityFile, parentId);
			}
		}

		protected void InsertUpdateFileField(RelativityFile relativityFile, int parentId)
		{
			if (relativityFile?.FileValue == null)
			{
				return;
			}

			if (relativityFile.FileValue.Path != null)
			{
				rsapiProvider.UploadFile(relativityFile, parentId, relativityFile.FileValue.Path);
			}
			else if (!string.IsNullOrEmpty(relativityFile.FileMetadata.FileName))
			{
				string fileName = Path.GetTempPath() + relativityFile.FileMetadata.FileName;
				File.WriteAllBytes(fileName, relativityFile.FileValue.Data);

				try
				{
					rsapiProvider.UploadFile(relativityFile, parentId, fileName);
				}
				finally
				{
					invokeWithRetryService.InvokeVoidMethodWithRetry(() => File.Delete(fileName));
				}
			}
		}

		protected void InsertUpdateSingleObjectPropertiesBeforeRdoInsert(ref BaseDto parentObjectToInsert)
		{
			IEnumerable<PropertyInfo> singleObjectsPropertyInfo = parentObjectToInsert.GetType()
				.GetProperties()
				.Where(c => c.GetCustomAttribute<RelativitySingleObjectAttribute>() != null);

			foreach (var propertyInfo in singleObjectsPropertyInfo)
			{
				Type singleObjectType = propertyInfo.PropertyType;

				BaseDto singleObjectToInsert = propertyInfo.GetValue(parentObjectToInsert) as BaseDto;

				PropertyInfo singleObjectArtifactIdProperty = singleObjectType.GetProperty("ArtifactId");

				PropertyInfo singleObjectArtifactIdPropertyInParentObject = parentObjectToInsert.GetType().GetProperties()
					.Where(c =>
					{
						RelativityObjectFieldAttribute objectFieldAttribute = c.GetCustomAttribute<RelativityObjectFieldAttribute>();

						if (objectFieldAttribute?.FieldType == RdoFieldType.SingleObject && objectFieldAttribute?.ObjectFieldDTOType == propertyInfo.PropertyType)
						{
							return true;
						}
						return false;
					}).Single();

				if (singleObjectArtifactIdProperty == null || singleObjectToInsert == null)
				{
					continue;
				}

				int singleObjectArtifactIdValue = (int)singleObjectArtifactIdProperty.GetValue(singleObjectToInsert);

				try
				{
					if (singleObjectArtifactIdValue == 0)
					{
						singleObjectArtifactIdValue = (int)this.InvokeGenericMethod(singleObjectType, nameof(InsertRelativityObject), singleObjectToInsert);

						PropertyInfo artifactIdProperty = parentObjectToInsert.GetType()
							.GetProperties()
							.Where(c =>
							{
								RelativityObjectFieldAttribute objectFieldAttribute = c.GetCustomAttribute<RelativityObjectFieldAttribute>();

								if (objectFieldAttribute?.FieldType == RdoFieldType.SingleObject && objectFieldAttribute?.ObjectFieldDTOType == propertyInfo.PropertyType)
								{
									return true;
								}

								return false;
							}).Single();

						singleObjectArtifactIdPropertyInParentObject.SetValue(parentObjectToInsert, singleObjectArtifactIdValue);
					}
					else
					{
						this.InvokeGenericMethodWithExactParametersTypes(singleObjectType, nameof(UpdateRelativityObject), singleObjectToInsert);
					}
				}
				catch (Exception)
				{

					singleObjectArtifactIdPropertyInParentObject.SetValue(parentObjectToInsert, -1);
				}
			}
		}

		private void InsertChildListObjectsWithDynamicType(BaseDto theObjectToInsert, int resultArtifactId, PropertyInfo propertyInfo)
		{
			var childObjectsList = propertyInfo.GetValue(theObjectToInsert, null) as IList;
			if(childObjectsList == null)
			{
				return;
			}

			if (childObjectsList?.Count != 0)
			{
				var childType = propertyInfo.PropertyType.GetEnumerableInnerType();
				this.InvokeGenericMethod(childType, nameof(InsertChildListObjects), childObjectsList, resultArtifactId);
			}
		}

		private static void SetParentArtifactID<T>(T objectToBeInserted, int parentArtifactId) where T : BaseDto, new()
		{
			PropertyInfo parentArtifactIdProperty = objectToBeInserted.GetParentArtifactIdProperty();
			PropertyInfo ArtifactIdProperty = objectToBeInserted.GetType().GetProperty("ArtifactId");

			if (parentArtifactIdProperty == null)
			{
				return;
			}

			parentArtifactIdProperty.SetValue(objectToBeInserted, parentArtifactId);
			ArtifactIdProperty.SetValue(objectToBeInserted, 0);
		}
		#endregion

		public void InsertChildListObjects<T>(IList<T> objectsToInserted, int parentArtifactId)
			where T : BaseDto, new()
		{
			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();

			bool isFilePropertyPresent = typeof(T).GetProperties().ToList().Any(c => c.DeclaringType.IsAssignableFrom(typeof(RelativityFile)));

			if (childObjectsInfo.Any() || isFilePropertyPresent)
			{
				foreach (var objectToBeInserted in objectsToInserted)
				{
					SetParentArtifactID(objectToBeInserted, parentArtifactId);
					int insertedRdoArtifactID = InsertRdo(objectToBeInserted.ToRdo());
					InsertUpdateFileFields(objectToBeInserted, insertedRdoArtifactID);

					foreach (var childPropertyInfo in childObjectsInfo)
					{
						InsertChildListObjectsWithDynamicType(objectToBeInserted, insertedRdoArtifactID, childPropertyInfo);
					}
				}
			}
			else
			{

				foreach (var objectToBeInserted in objectsToInserted)
				{
					SetParentArtifactID(objectToBeInserted, parentArtifactId);
				}

				var rdosToBeInserted = objectsToInserted.Select(x => x.ToRdo()).ToArray();

				rsapiProvider.Create(rdosToBeInserted);
			}
		}

		public int InsertRelativityObject<T>(BaseDto theObjectToInsert)
		{
			InsertUpdateSingleObjectPropertiesBeforeRdoInsert(ref theObjectToInsert);

			int resultArtifactId = InsertRdo(theObjectToInsert.ToRdo());

			InsertUpdateFileFields(theObjectToInsert, resultArtifactId);

			var childObjectsInfo = BaseDto.GetRelativityObjectChildrenListProperties<T>();
			foreach (var childPropertyInfo in childObjectsInfo)
			{
				InsertChildListObjectsWithDynamicType(theObjectToInsert, resultArtifactId, childPropertyInfo);
			}

			return resultArtifactId;
		}
	}
}
