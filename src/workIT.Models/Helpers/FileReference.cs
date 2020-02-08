using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace workIT.Models
{
	//Contains a set of files to save and deletes to make
	//Use this on other objects
	public class FileReferenceData
	{
		public FileReferenceData()
		{
			Files = new List<FileReference>();
			Deletes = new List<Guid>();
		}
		public List<FileReference> Files { get; set; }
		public List<Guid> Deletes { get; set; }
	}
	//
	public class FileReference: BaseEntity
	{
		public string Label { get; set; } //User-defined
		public string MimeType { get; set; } //e.g., application/json or image/png
		public string RawData { get; set; } //Data incoming from the client - should generally be Base64 encoded
		public Guid OwnerRowId { get; set; } //GUID of the org, group, announcement, etc., that owns this
		public string ValueForProperty { get; set; } //Indicates which property on the owner object this is a value for
		public string FileExtension { get; set; } //Extension, should not contain a dot
		//public bool FileExists { get { return File.Exists( SavedPath ); } } //Convenience
		//public string SavedUrl { get { return ConfigurationManager.AppSettings[ "UploadedFilesUrlRelativePath" ] + RowId.ToString(); } } //content controller retrieves the file by GUID, not filename

		//[JsonIgnore] //Don't expose this path to the client
		//public string SavedPath { get { return ConfigurationManager.AppSettings[ "UploadedFilesDirectory" ] + RowId.ToString() + "." + FileExtension; } }
	}

	[Serializable]
	public class BaseEntity
	{
		public int Id { get; set; }
		public Guid RowId { get; set; }
		public bool IsActive { get; set; }
		public DateTime Created { get; set; }
		public int CreatedById { get; set; }
		public DateTime LastUpdated { get; set; }
		public int LastUpdatedById { get; set; }
	}
}