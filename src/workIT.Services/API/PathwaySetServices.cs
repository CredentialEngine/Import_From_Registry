using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Utilities;

using EntityHelper = workIT.Services.PathwayServices;
using ThisEntity = workIT.Models.Common.PathwaySet;
using ThisEntityDetail = workIT.Models.API.PathwaySet;
using WMA = workIT.Models.API;

namespace workIT.Services.API
{
	public class PathwaySetServices
	{
		static string thisClassName = "API.PathwaySetServices";
		public static string searchType = "pathwaySet";

		public static ThisEntityDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var output = EntityHelper.PathwaySetGetDetail( id );
			return MapToAPI( output );

		}
		public static ThisEntityDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var output = EntityHelper.PathwaySetGetByCtid( ctid );
			return MapToAPI( output );
		}
		private static ThisEntityDetail MapToAPI( ThisEntity input )
		{


			var output = new ThisEntityDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 23,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType )
			};
			//TODO - add pathway
			if ( input.Pathways != null && input.Pathways.Any() )
			{
				output.HasPathways = new List<WMA.Outline>();
				foreach ( var target in input.Pathways )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						output.HasPathways.Add( ServiceHelper.MapToOutline( target, searchType ) );
				}
				output.HasPathway = ServiceHelper.MapOutlineToAJAX( output.HasPathways, "Has {0} Pathways(s)" );
				output.HasPathways = null;

			}

			return output;
		}
	}
}
