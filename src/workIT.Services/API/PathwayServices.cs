using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;

using workIT.Models;
using workIT.Models.Common;
using WMA = workIT.Models.API;
using workIT.Models.Search;
using workIT.Factories;

using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;
using EntityHelper = workIT.Services.PathwayServices;

using ThisEntity = workIT.Models.Common.Pathway;
using ThisEntityDetail = workIT.Models.API.Pathway;

namespace workIT.Services.API
{
	public class PathwayServices
	{
		static string thisClassName = "API.PathwayServices";
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );
		public static string searchType = "pathway";

		public static ThisEntityDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetail( id, skippingCache );
			return MapToAPI( output );

		}
		public static ThisEntityDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetailByCtid( ctid, skippingCache );
			return MapToAPI( output );
		}
		private static ThisEntityDetail MapToAPI( ThisEntity input )
		{
			

			var output = new ThisEntityDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 8,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID )
			};

			return output;
		}
	}
}
