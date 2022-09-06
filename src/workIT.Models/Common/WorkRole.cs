using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using workIT.Models.ProfileModels;

namespace workIT.Models.Common
{
	/// <summary>
	/// Profession, trade, or career field that may involve training and/or a formal qualification.
	/// </summary>
	public class WorkRole : BaseEmploymentObject
	{
		/// <summary>
		///  type
		/// </summary>
		public string Type { get; set; } = "ceterms:WorkRole";


		/// <summary>
		/// Task related to this resource.
		/// <see cref="https://credreg.net/ctdl/terms/hasTask"/>
		/// ceterms:hasSpecialization
		/// </summary>
		public List<int> HasTask { get; set; }


		#region 

		#endregion
	}
}
