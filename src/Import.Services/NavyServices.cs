using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

using RatingEntity = RA.Models.Navy.Json.Rating;
using BNode = RA.Models.JsonV2.BlankNode;
using InputAddress = RA.Models.JsonV2.Place;
using workIT.Models;
using MC = workIT.Models.Common;

namespace Import.Services
{
	public class NavyServices : MappingHelperV3
	{
		public static MC.Enumeration MapRatingsListToEnumermation(List<string> input)
		{
			//TBD = do we need anything for emumeration, or just items?
			MC.Enumeration output = new workIT.Models.Common.Enumeration();
			if ( input == null || input.Count == 0 )
				return output;
			string ctdlType = "";
			string status = "";
			foreach ( var item in input )
			{
				string resource = RegistryServices.GetResourceByUrl( item, ref ctdlType, ref status );

				var rating = Newtonsoft.Json.JsonConvert.DeserializeObject<RatingEntity>( resource );

				if ( rating != null && ( rating.Name != null ) )
				{
					output.Items.Add( new workIT.Models.Common.EnumeratedItem()
					{
						SchemaName = rating.Type ?? "",
						Name = rating.Name.ToString(),
						Description = rating.Description.ToString(),
						
						URL = rating.CtdlId
					} ); ;
				}

			}
			return output;
		} //
		public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCAOProfileList(List<string> input, ref List<string> messages)
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
			MC.CredentialAlignmentObjectProfile entity = new MC.CredentialAlignmentObjectProfile();

			if ( input == null || input.Count == 0 )
				return output;
			string ctdlType = "";
			string status = "";
			foreach ( var item in input )
			{
				string resource = RegistryServices.GetResourceByUrl( item, ref ctdlType, ref status );
				if (string.IsNullOrWhiteSpace(resource))
				{
					messages.Add( string.Format( "NavyServices.MapCAOListToCAOProfileList. Unable to resolve Rating URL: {0}", item ) );
					continue;
				}
				var rating = Newtonsoft.Json.JsonConvert.DeserializeObject<RatingEntity>( resource );

				if ( rating != null && ( rating.Name != null ) )
				{

					entity = new MC.CredentialAlignmentObjectProfile()
					{
						TargetNode = rating.CtdlId,
						CodedNotation = rating.CodedNotation ?? ""
					};

					entity.TargetNodeName = HandleLanguageMap( rating.Name, currentBaseObject, "TargetNodeName" );
					entity.TargetNodeName_Map = lastLanguageMapString;
					entity.TargetNodeDescription = HandleLanguageMap( rating.Description, currentBaseObject, "TargetNodeDescription", false );
					entity.TargetNodeDescription_Map = lastLanguageMapString;

					//if ( !string.IsNullOrWhiteSpace( item.Framework ) )
					//{
					//	if ( item.Framework.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) == -1
					//		&& item.Framework.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) == -1 )
					//	{
					//		entity.SourceUrl = item.Framework;
					//	}
					//	else
					//	{
					//		entity.FrameworkUri = item.Framework;
					//	}
					//}
					output.Add( entity );
				}

			}
			return output;
		}

	}
}
