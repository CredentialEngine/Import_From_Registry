using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.API;
using workIT.Factories;
using MC = workIT.Models.Common;

namespace workIT.Services.API
{
	public class ConceptSchemeServices
	{
		public static ConceptScheme GetConceptSchemeOnlyByID( int id, bool skippincCache = false )
		{
			var rawData = ConceptSchemeManager.Get( id, false );
			return ConvertCommonModelsConceptSchemeToFinderAPIConceptScheme( rawData, "Unable to find concept scheme for ID: " + id );
		}
		//

		public static ConceptScheme GetConceptSchemeOnlyByCTID( string ctid, bool skippingCache = false )
		{
			var rawData = ConceptSchemeManager.GetByCtid( ctid, false );
			return ConvertCommonModelsConceptSchemeToFinderAPIConceptScheme( rawData, "Unable to find concept scheme for CTID: " + ctid );
		}
		//

		public static ConceptScheme ConvertCommonModelsConceptSchemeToFinderAPIConceptScheme( MC.ConceptScheme source, string nullErrorMessage = null )
		{
			var result = new ConceptScheme();

			if( source == null || source.Id == 0 )
			{
				result.Name = nullErrorMessage ?? "Error: Unable to find concept scheme";
			}
			else
			{
				//Meta properties
				result.Meta_Id = source.Id;
				result.Meta_Language = "en"; //Need a way to detect this
				result.EntityLastUpdated = source.LastUpdated;
				result.Meta_StateId = source.EntityStateId;
				result.CredentialRegistryURL = source.CredentialRegistryId; //Verify this
				result.RegistryData = ServiceHelper.FillRegistryData( source.CTID );
				result.CTDLType = source.EntityType;
				result.EntityTypeId = source.EntityTypeId;

				//Organization properties
				//Missing references for creator/publisher

				//Core properties
				result.CTID = source.CTID;
				result.Name = source.Name;
				result.Description = source.Description;
				result.SubjectWebpage = source.SubjectWebpage; //Not a property of Concept Scheme?
				
				//Extra properties
				result.Source = source.Source;
				result.Meta_FriendlyName = source.FriendlyName;

				//Concepts
				//Missing properties for these
			}

			return result;
		}
		//
	}
}
