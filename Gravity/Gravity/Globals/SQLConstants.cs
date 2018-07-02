using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravity.Globals
{
    public static class SQLConstants
    {
        #region SqlGetConstants

        public const string sqlGetArtifactGuidsMappingToColumnNames = @"
SELECT artg.ArtifactGuid, art.TextIdentifier
  FROM [EDDSDBO].[Artifact] art (NOLOCK)
  JOIN [EDDSDBO].[ArtifactGuid] artg (NOLOCK) on art.ArtifactID = artg.ArtifactID
  WHERE artg.ArtifactGuid IN (
";

        public const string sqlGetArtifactGuidMappings = @"
SELECT [ArtifactID], [ArtifactGuid]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  WHERE [ArtifactID] IN (
";

        public const string sqlGetMultipleObjectArtifactIDs = @"
-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @MultipleObjectFieldArtifactGuid uniqueidentifier
--SET @ArtifactID = 1068172
--SET @MultipleObjectFieldArtifactGuid = 'd0770889-8a4d-436a-9647-33419b96e37e'

DECLARE @MultipleObjectFieldArtifactID int

SELECT @MultipleObjectFieldArtifactID=[ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  where ArtifactGuid=@MultipleObjectFieldArtifactGuid

-- SELECT @MultipleObjectFieldArtifactID

DECLARE @RelationalTableSchemaName nvarchar(100)
DECLARE @FieldArtifactIDColumnName nvarchar(100)
DECLARE @ChildArtifactIDsColumnName nvarchar(100)
DECLARE @Relation1to2 int

-- Get the multi-relation 1-to-2 or 2-to-1
SELECT
	@RelationalTableSchemaName = [RelationalTableSchemaName]
	,@FieldArtifactIDColumnName = [RelationalTableFieldColumnName1]
	,@ChildArtifactIDsColumnName = [RelationalTableFieldColumnName2]
	,@Relation1to2 = FieldArtifactId1 - @MultipleObjectFieldArtifactID
  FROM [EDDSDBO].[ObjectsFieldRelation]
  WHERE [FieldArtifactId1] = @MultipleObjectFieldArtifactID OR [FieldArtifactId2]=@MultipleObjectFieldArtifactId

If (@Relation1to2 <> 0)
BEGIN
	DECLARE @TempColumnName nvarchar(100)
	SET @TempColumnName = @FieldArtifactIDColumnName
	SET @FieldArtifactIDColumnName = @ChildArtifactIDsColumnName
	SET @ChildArtifactIDsColumnName = @TempColumnName
END

-- SELECT @RelationalTableSchemaName, @FieldArtifactIDColumnName ,@ChildArtifactIDsColumnName, @Relation1to2

-- Now that we know the relation table and the kyes, get the child artifact IDs
DECLARE @sqlGetChildrenArtifactIDsScript nvarchar(MAX)
SET @sqlGetChildrenArtifactIDsScript =
	N'SELECT [' + @ChildArtifactIDsColumnName  + '] 
	FROM  [EDDSDBO].[' + @RelationalTableSchemaName + '] (NOLOCK)
	WHERE [' + @FieldArtifactIDColumnName  + ']=' + CONVERT(nvarchar(20), @ArtifactID)

--SELECT @sqlGetChildrenArtifactIDsScript

EXECUTE sp_executesql @sqlGetChildrenArtifactIDsScript
";

        public const string sqlGetSingleObjectArtifactIDFormat = @"
SELECT [{0}]
  FROM [EDDSDBO].[{1}] (NOLOCK)
  WHERE ArtifactID=@ArtifactID
";

        public const string sqlGetChoicesArtifactIDs = @"
-- PARAMS:
--DECLARE @ArtifactID int
--DECLARE @ChoiceFieldArtifactGuid uniqueidentifier

DECLARE @ChoiceFieldArtifactID int
DECLARE @ChoiceFieldCodeTypeID int

SELECT @ChoiceFieldArtifactID=[ArtifactID]
  FROM [EDDSDBO].[ArtifactGuid] (NOLOCK)
  where ArtifactGuid=@ChoiceFieldArtifactGuid

SELECT @ChoiceFieldCodeTypeID=[CodeTypeID] FROM Field (NOLOCK)
	WHERE [ArtifactID] = @ChoiceFieldArtifactID

DECLARE @sqlGetChildrenArtifactIDs nvarchar(500)
SET @sqlGetChildrenArtifactIDs =
	N'SELECT [CodeArtifactID] 
	FROM [EDDSDBO].[ZCodeArtifact_' + CONVERT(nvarchar(20), @ChoiceFieldCodeTypeID) + '] (NOLOCK)
	WHERE [AssociatedArtifactID]=' + CONVERT(nvarchar(20), @ArtifactID)

print @sqlGetChildrenArtifactIDs

EXECUTE sp_executesql @sqlGetChildrenArtifactIDs
";

        public const string sqlGetChildrenArtifactIDs = @"
SELECT TOP 1000 [ArtifactID]
  FROM [EDDSDBO].[Artifact]
  where ParentArtifactID=@ArtifactID";

        public const string sqlGetChildrenArtifactIDsByParentAndType = @"
SELECT TOP 1000 [ArtifactID]
  FROM [EDDSDBO].[Artifact]
  WHERE ParentArtifactID = @ParentArtifactID AND ArtifactTypeID = @ArtifactTypeID";

        public const string sqlGetArtifactTypeIdByArtifactGuid = @"
SELECT OT.[DescriptorArtifactTypeID]
  FROM [EDDSDBO].[ArtifactGuid] as AG WITH (NOLOCK)
  INNER JOIN [EDDSDBO].[ObjectType] as OT WITH (NOLOCK)
  ON AG.ArtifactID = OT.ArtifactID 
  WHERE AG.ArtifactGuid = @ArtifactGuid
";

        public static readonly string sqlGetUserNamesByIdAndWorkspace = @"
SELECT TOP 1 *
  FROM [EDDSDBO].[User] as instUser WITH (NOLOCK)
  INNER JOIN [EDDSDBO].[UserCaseUser] as caseUser  WITH (NOLOCK)
  ON instUser.ArtifactID = caseUser.UserArtifactID
  WHERE caseUser.CaseUserArtifactID = @CaseUserArtifactId and caseUser.CaseArtifactID = @CaseArtifactId";

        public static readonly string sqlGetUserHeaderAndColumnNamesMapping = @"
SELECT [HeaderName]
      ,[ColumnName]
  FROM [eddsdbo].[ArtifactViewField] WITH (NOLOCK)
  WHERE ArtifactTypeID = 2 -- User";

        public static readonly string sqlGetChoicesValuesByArtifactIds = @"
SELECT [Name]
  FROM [EDDSDBO].[Code] WITH (NOLOCK)
  WHERE ArtifactID IN (%%ArtifactIds%%)";
        #endregion
    }
}
