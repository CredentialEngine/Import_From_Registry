using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web;

using workIT.Models;
using MC=workIT.Models.Common;
using WMA = workIT.Models.API;
using WMS = workIT.Models.Search;
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
				Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 8,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType )
			};
			//output.HasDestinationComponent = ServiceHelper.MapToOutline( input.HasDestinationComponent );
			//			output.HasDestinationComponent = input.HasDestinationComponent;
			output.HasDestinationComponent = MapPathwayComponentToAJAXSettings( input.HasDestinationComponent, "Destination Component(s)" );
			//output.HasChild = ServiceHelper.MapToOutline( input.HasChild );
			output.HasChild = MapPathwayComponentToAJAXSettings( input.HasChild, "Has Child Component(s)" );
			//
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			//output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if ( input.Subject != null && input.Subject.Any() )
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
			//
			//var meta_HasPart = ServiceHelper.MapToOutline( input.HasPart );
			output.Meta_HasPart = MapPathwayComponentToAJAXSettings( input.HasPart, "Has Part(s)" );
			//hide N/A from base
			output.InLanguage = null;
			return output;
		}

		public static WMS.AJAXSettings MapPathwayComponentToAJAXSettings( List<MC.PathwayComponent> input, string label )
		{

			if ( input == null || !input.Any() )
				return null;
			try
			{
				//var work = new List<WMA.Outline>();
				//foreach ( var target in input )
				//{
				//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
				//		work.Add( MapToOutline( target, "PathwayComponent" ) );
				//}
				var output = new WMS.AJAXSettings()
				{
					//Type=null,
					Label = string.Format( label, input.Count ),
					Total = input.Count
				};
				List<object> obj = input.Select( f => ( object )f ).ToList();
				output.Values = obj;
				return output;
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "ServiceHelper.MapPathwayToAJAXSettings" );
				return null;
			}

		}
	}
}
