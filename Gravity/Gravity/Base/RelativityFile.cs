using kCura.Relativity.Client;
using System;

namespace Gravity.Base
{
	[Serializable]
	public class RelativityFile
	{
		public RelativityFile()
		{ }

		public RelativityFile(int fieldId)
		{
			this.FieldId = fieldId;
		}

		public RelativityFile(int fieldId, FileValue fieldValue, FileMetadata fileMetadata )
		{
			this.FieldId = fieldId;
			this.FileMetadata = fileMetadata;
			this.FileValue = fieldValue;
		}

		public int FieldId { get; set; }

		public FileValue FileValue { get; set; }

		public FileMetadata FileMetadata { get; set; }
	}
}
