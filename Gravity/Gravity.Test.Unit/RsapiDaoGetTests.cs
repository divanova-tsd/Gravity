using Gravity.Base;
using Gravity.DAL.RSAPI;
using Gravity.DAL.RSAPI.Tests;
using Gravity.Test.Helpers;
using Gravity.Test.TestClasses;
using kCura.Relativity.Client.DTOs;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DownloadResponse = kCura.Relativity.Client.DownloadResponse;
using FileMetadata = kCura.Relativity.Client.FileMetadata;

namespace Gravity.Test.Unit
{
	public class RsapiDaoGetTests
	{
		private const int RootArtifactID = 1111111;

		[Test]
		public void GetHydratedDTO_BlankRDO()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, null));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(RootArtifactID, dto.ArtifactId);
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_MultiObject_Recursive()
		{
			//test MultiObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_ChildObjectList_Recursive()
		{
			//test ChildObject fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		[Ignore("TODO: Implement")]
		public void GetHydratedDTO_SingleObject_Recursive()
		{
			//test single object fields with varying degrees of recursion
			throw new NotImplementedException();
		}

		[Test]
		public void GetHydratedDTO_DownloadsFileContents()
		{
			const int FieldId = 2;

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			var fileGuid = GetFieldGuid<GravityLevelOne>(nameof(GravityLevelOne.FileField));
			var fileField = rdo.Fields.Single(x => x.Guids.Contains(fileGuid));
			rdo[fileGuid].ArtifactID = FieldId;
			rdo[fileGuid].ValueAsFixedLengthText = "SimilarToFileName";

			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).Returns(new[] { new RDO[0].ToSuccessResultSet<QueryResultSet<RDO>>() });

			providerMock.Setup(x => x.ReadSingle(RootArtifactID)).Returns(rdo);
			providerMock.Setup(x => x.DownloadFile(FieldId, RootArtifactID)).Returns(
				new KeyValuePair<DownloadResponse, Stream>(
					new DownloadResponse { Metadata = new FileMetadata { FileName = "FileName" } },
					new MemoryStream(Encoding.UTF8.GetBytes("Test Message"))
				));


			var rsapiProvider = new RsapiDao(providerMock.Object);
			var dto = rsapiProvider.GetRelativityObject<GravityLevelOne>(RootArtifactID, ObjectFieldsDepthLevel.FirstLevelOnly);

			var file = dto.FileField;

			Assert.AreEqual("FileName", file.FileMetadata.FileName);
			Assert.AreEqual("Test Message", Encoding.UTF8.GetString(file.FileValue.Data));
		}

		[Test]
		public void GetHydratedDTO_DoesNotDownloadFileContentsWithoutRecursion()
		{
			const int FieldId = 2;

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			var fileGuid = GetFieldGuid<GravityLevelOne>(nameof(GravityLevelOne.FileField));
			var fileField = rdo.Fields.Single(x => x.Guids.Contains(fileGuid));
			rdo[fileGuid].ArtifactID = FieldId;
			rdo[fileGuid].ValueAsFixedLengthText = "SimilarToFileName";

			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);
			providerMock.Setup(x => x.Query(It.IsAny<Query<RDO>>())).Returns(new[] { new RDO[0].ToSuccessResultSet<QueryResultSet<RDO>>() });

			providerMock.Setup(x => x.ReadSingle(RootArtifactID)).Returns(rdo);
			
			var rsapiProvider = new RsapiDao(providerMock.Object);
			var dto = rsapiProvider.GetRelativityObject<GravityLevelOne>(RootArtifactID, ObjectFieldsDepthLevel.OnlyParentObject);

			// TODO: Is this really what we want? Maybe whole field should be null. See #102
			Assert.Null(dto.FileField.FileValue);
			Assert.Null(dto.FileField.FileMetadata);
		}

		[Test]
		public void GetHydratedDTO_SingleChoice_InEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(2, null));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			Assert.AreEqual(SingleChoiceFieldChoices.SingleChoice2, dto.SingleChoice);
		}

		[Test]
		public void GetHydratedDTO_SingleChoice_NotInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(5, null));
			Assert.Throws<InvalidOperationException>(() => dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject));
		}

		[Test]
		public void GetHydratedDTO_MultipleChoice_AllInEnum()
		{
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 11, 13 }));
			var dto = dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject);
			CollectionAssert.AreEquivalent(
				new[] { MultipleChoiceFieldChoices.MultipleChoice1, MultipleChoiceFieldChoices.MultipleChoice3 },
				dto.MultipleChoiceFieldChoices
			);
		}

		[Test]
		public void GetHydratedDTO_MultipleChoice_NotAllInEnum()
		{
			//first item is in an enum, but not in our enum
			var dao = new RsapiDao(GetChoiceRsapiProvider(null, new[] { 3, 13 }));
			Assert.Throws<InvalidOperationException>(() => dao.GetRelativityObject<GravityLevelOne>(RootArtifactID, Base.ObjectFieldsDepthLevel.OnlyParentObject));

		}

		private IRsapiProvider GetChoiceRsapiProvider(int? singleChoiceId, int[] multipleChoiceIds)
		{
			var providerMock = new Mock<IRsapiProvider>(MockBehavior.Strict);

			// setup the RDO Read

			var multipleGuid = GetFieldGuid<GravityLevelOne>(nameof(GravityLevelOne.MultipleChoiceFieldChoices));
			var singleGuid = GetFieldGuid<GravityLevelOne>(nameof(GravityLevelOne.SingleChoice));

			var rdo = TestObjectHelper.GetStubRDO<GravityLevelOne>(RootArtifactID);
			rdo[singleGuid].ValueAsSingleChoice = singleChoiceId == null ? null : new Choice(singleChoiceId.Value);
			rdo[multipleGuid].ValueAsMultipleChoice = multipleChoiceIds?.Select(x => new Choice(x)).ToList() ?? new List<Choice>();

			providerMock.Setup(x => x.ReadSingle(RootArtifactID)).Returns(rdo);
			
			// setup the choice query

			// results in ArtifactIDs 1, 2, 3
			var singleChoiceGuids = ChoiceCacheTests.GetOrderedGuids<SingleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(singleChoiceGuids)).Returns(ChoiceCacheTests.GetResults(singleChoiceGuids, 1));
			// results in ArtifactIDs 11, 12, 13
			var multiChoiceGuids = ChoiceCacheTests.GetOrderedGuids<MultipleChoiceFieldChoices>();
			providerMock.Setup(ChoiceCacheTests.SetupExpr(multiChoiceGuids)).Returns(ChoiceCacheTests.GetResults(multiChoiceGuids, 11));

			return providerMock.Object;
		}

		private static Guid GetFieldGuid<T>(string fieldName) where T : BaseDto
		{
			return typeof(T)
				.GetProperty(fieldName)
				.GetCustomAttribute<RelativityObjectFieldAttribute>()
				.FieldGuid;
		}
	}
}
