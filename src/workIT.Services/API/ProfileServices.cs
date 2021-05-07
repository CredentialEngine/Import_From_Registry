using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;

using workIT.Models;
using workIT.Models.Common;
using MCD = workIT.Models.API;
using MPM=workIT.Models.ProfileModels;
using workIT.Models.Search;
using workIT.Factories;


namespace workIT.Services.API
{
	public class ProfileServices
	{

		public static List<MCD.ProcessProfile> HandleProcessProfiles( Guid parentUid, int processProfileTypeId )
		{
			//process profiles
			var output = new List<MCD.ProcessProfile>();
			//make a common method - then can pass parent to use for details
			var plist = Entity_ProcessProfileManager.GetAll( parentUid, processProfileTypeId );
			if (plist != null && plist.Any())
			{
				output = ServiceHelper.MapProcessProfile( "", plist );
			}

			return output;
		}
	}
}
