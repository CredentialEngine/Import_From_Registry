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
	public class ProgressionModelServices
	{
		public static ProgressionModel GetDetailForAPI( int id, bool skippincCache = false )
		{
			var rawData = ProgressionModelManager.Get( id, false );
			return MapToAPI( rawData, "Unable to find progression model for ID: " + id );
		}
		//

		public static ProgressionModel GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var rawData = ProgressionModelManager.GetByCtid( ctid, false );
			return MapToAPI( rawData, "Unable to find progression model for CTID: " + ctid );
		}
		//

		public static ProgressionModel MapToAPI( MC.ProgressionModel input, string nullErrorMessage = null )
		{
			var result = new ProgressionModel();

			if( input == null || input.Id == 0 )
			{
				result.Name = nullErrorMessage ?? "Error: Unable to find progression model";
			}
			else
			{
				//Meta properties
				result.Meta_Id = input.Id;
				result.Meta_Language = "en"; //Need a way to detect this
				result.EntityLastUpdated = input.LastUpdated;
				result.Meta_StateId = input.EntityStateId;
				result.CredentialRegistryURL = input.CredentialRegistryId; //Verify this
				result.RegistryData = ServiceHelper.FillRegistryData( input.CTID );
				result.CTDLType = input.EntityType;
				result.EntityTypeId = input.EntityTypeId;

				//Organization properties
				//Missing references for creator/publisher

				//Core properties
				result.CTID = input.CTID;
				result.Name = input.Name;
				result.Description = input.Description;
				if ( input.Source  != null & input.Source.Any())
					result.SubjectWebpage = input.Source[0]; //Not a property of progression model?
				
				//Extra properties
				result.Source = input.Source;
				result.Meta_FriendlyName = input.FriendlyName;

				//Concepts
				//Missing properties for these
			}

			return result;
		}
		//
	}
}
