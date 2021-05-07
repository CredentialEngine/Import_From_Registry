using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.QData;

namespace workIT.Models.Common
{
	public class OutcomesBaseObject : BaseObject
	{
		public string CTID { get; set; }
		public int EntityStateId { get; set; }
		/// <summary>
		/// Name of the outcomes document
		/// NOTE: HoldersProfile doesn't include a name
		/// </summary>
		public string Name { get; set; }
		public string Description { get; set; }
		/// <summary>
		/// Jurisdiction Profile
		/// Geo-political information about applicable geographic areas and their exceptions.
		/// <see cref="https://credreg.net/ctdl/terms/JurisdictionProfile"/>
		/// </summary>
		public List<JurisdictionProfile> Jurisdiction { get; set; } = new List<JurisdictionProfile>();


		/// <summary>
		/// Authoritative source of an entity's information.
		/// URL
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// Relevant Data Set
		/// Data Set on which earnings or employment data is based.
		/// qdata:DataSetProfile
		/// </summary>
		public List<DataSetProfile> RelevantDataSet { get; set; } = new List<DataSetProfile>();

		//import only-maybe
		public List<string> RelevantDataSetList { get; set; } = new List<string>();
		//
		public List<Guid> PublishedBy { get; set; }
	}
}
