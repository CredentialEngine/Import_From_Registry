using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class BaseObject
	{
		public BaseObject()
		{
			RowId = new Guid(); //Will be all 0s, which is probably desirable
			//DateEffective = new DateTime();
			Created = new DateTime();
			LastUpdated = new DateTime();
			//IsNewVersion = true;
			HasCompetencies = false;
			ChildHasCompetencies = false;
			//Publish_Type = "ceterms:entity";
			StatusMessage = "";
		}
		public int Id { get; set; }
        public bool IsReferenceVersion { get; set; }
        public bool CanEditRecord { get; set; }
		//public bool CanUserEditEntity { 
		//	get { return this.CanEditRecord; }
		//	set { 
		//		this.CanEditRecord = value; 
		//	}

		//}
		public bool CanViewRecord { get; set; }
		public Guid RowId { get; set; }
		public int ParentId { get; set; }
		public bool HasCompetencies { get; set; }
		public bool ChildHasCompetencies { get; set; }
		public string DateEffective { get; set; }
		public string StatusMessage { get; set; }

		public DateTime Created { get; set; }
		public int CreatedById { get; set; }
		public string CreatedBy { get; set; }
		public DateTime LastUpdated { get; set; }
		public string LastUpdatedDisplay
		{
			get
			{
				if ( LastUpdated == null )
				{
					if ( Created != null )
					{
						return Created.ToShortDateString();
					}
					return "";
				}
				return LastUpdated.ToShortDateString();
			}
		}
		public int LastUpdatedById { get; set; }
		public string LastUpdatedBy { get; set; }

		//Publishing properties
		//public string Publish_Type { get; set; }
		//public virtual Dictionary<string, object> Publish_GetPublishableVersion()
		//{
		//	return new Dictionary<string, object>()
		//	{
		//		{ "@type", Publish_Type },
		//		{ "@id", "http://credentialregistry.org/resource/" + RowId.ToString() }
		//	};
		//}
		//

	}
	//

}
