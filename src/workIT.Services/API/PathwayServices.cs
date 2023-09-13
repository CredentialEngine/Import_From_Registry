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

using ThisResource = workIT.Models.Common.Pathway;
using ThisResourceDetail = workIT.Models.API.Pathway;
using PathwayComponent = workIT.Models.Common.PathwayComponent;
using PathwayComponentDetail = workIT.Models.API.PathwayComponent;

namespace workIT.Services.API
{
	public class PathwayServices
	{
		static string thisClassName = "API.PathwayServices";
		public static string searchType = "pathway";

		#region pathway
		public static ThisResourceDetail GetDetailForAPI( int id, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetail( id, skippingCache );
			return MapToAPI( output );

		}
		public static ThisResourceDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var output = EntityHelper.GetDetailByCtid( ctid, skippingCache );
			return MapToAPI( output );
		}
		private static ThisResourceDetail MapToAPI( ThisResource input )
		{
			

			var output = new ThisResourceDetail()
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
			output.EntityLastUpdated = input.EntityLastUpdated;
			output.Meta_StateId = input.EntityStateId;
            output.AllowUseOfPathwayDisplay = input.AllowUseOfPathwayDisplay;

            //output.HasDestinationComponent = ServiceHelper.MapToOutline( input.HasDestinationComponent );
            //			output.HasDestinationComponent = input.HasDestinationComponent;
            output.HasDestinationComponent = MapPathwayComponentToAJAXSettings( input.HasDestinationComponent, "Destination Component(s)" );
			//output.HasChild = ServiceHelper.MapToOutline( input.HasChild );
			output.HasChild = MapPathwayComponentToAJAXSettings( input.HasChild, "Has Child Component(s)" );
			//
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
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
            //
            if ( input.HasSupportService?.Count > 0 )
            {
                var work = new List<WMA.Outline>();
                foreach ( var target in input.HasSupportService )
                {
                    if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                        work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                }
                output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );
            }
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
        #endregion


        #region pathwayComponent
        public static PathwayComponentDetail PathwayComponentGetDetailForAPI( int id )
        {
            var output = EntityHelper.GetComponent( id, 2 );
            return MapToAPI( output );

        }
        public static PathwayComponentDetail PathwayComponentGetDetailByCtidForAPI( string ctid )
        {
            var output = EntityHelper.GetComponentByCtid( ctid, 2 );
            return MapToAPI( output );
        }
        private static PathwayComponentDetail MapToAPI( workIT.Models.Common.PathwayComponent input )
        {

			var output = new PathwayComponentDetail()
			{
				Meta_Id = input.Id,
				CTID = input.CTID,
				Name = input.Name,
				Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = input.EntityTypeId,
				PathwayComponentType = input.PathwayComponentType,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType )
			};
            output.EntityLastUpdated = input.EntityLastUpdated;
            output.Meta_StateId = 3;
            //hide N/A from base
            output.InLanguage = null;

            //output.AllComponents = null;

   //         output.Pathway = new MC.ResourceSummary()
			//{
			//	Type= "Pathway",
			//	Id = input.Pathway.Id,
			//	Name = input.Pathway.Name,
			//	Description = input.Pathway.Description,
			//	CTID = input.Pathway.CTID,
			//	URI = ServiceHelper.credentialFinderMainSite + "pathway/" + input.Pathway.CTID
			//};

            var work = new List<WMA.Outline>();
            if (input.Pathway != null && input.Pathway.Id > 0)
            {
                work.Add( ServiceHelper.MapToOutline( input.Pathway, "Pathway" ) );
                output.Pathway = ServiceHelper.MapOutlineToAJAX( work, "Part of Pathway" );

				if ( input.Pathway.HasDestinationComponent != null && input.Pathway.HasDestinationComponent.Any() )
				{
					if (input.Pathway.HasDestinationComponent[0].CTID == input.CTID )
					{

					}
				}
            }

            output.EntityLastUpdated = input.EntityLastUpdated;
            output.Meta_StateId = input.EntityStateId;
            output.AllowUseOfPathwayDisplay = input.Pathway.AllowUseOfPathwayDisplay;

			//MORE TO COME
			output.CodedNotation = input.CodedNotation;
			output.ComponentCategory = input.ComponentCategory;
			output.CredentialType = input.CredentialType;
            output.CreditValue = ServiceHelper.MapValueProfile( input.CreditValue, searchType );

            output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			output.PointValue = ServiceHelper.MapQuantitativeValue( input.PointValue );
			output.ProgramTerm = input.ProgramTerm;

			output.ProxyFor = null;
            if (input.ProxyForResource != null && input.ProxyForResource.Id > 0)
            {
                work.Add( ServiceHelper.MapToOutline( input.ProxyForResource, "Pathway Component" ) );
                output.ProxyFor = ServiceHelper.MapOutlineToAJAX( work, "Component Resource" );
            }

            output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
            output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );


            return output;
        }

        #endregion
    }
}
