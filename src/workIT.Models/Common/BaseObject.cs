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
		/// <summary>
		/// ParentId will typically be the Entity.Id related to the base class
		/// </summary>
		public int ParentId { get; set; }
		public bool HasCompetencies { get; set; }
		public bool ChildHasCompetencies { get; set; }
		public string DateEffective { get; set; }
		public string StatusMessage { get; set; }

		public DateTime Created { get; set; }
		public int CreatedById { get; set; }
		public string CreatedBy { get; set; }
		public DateTime LastUpdated { get; set; }
		public string CreatedDisplay { get { return Created == null ? "" : Created.ToShortDateString(); } }
		public string LastUpdatedDisplay { get { return LastUpdated == null ? CreatedDisplay : LastUpdated.ToShortDateString(); } }
		public int LastUpdatedById { get; set; }
		public string LastUpdatedBy { get; set; }
		public DateTime EntityLastUpdated { get; set; }

		//store language map properties
		public List<EntityLanguageMap> LanguageMaps { get; set; } = new List<EntityLanguageMap>();
        public string FirstLanguage { get; set; }
    }
	//

}
