using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

using MJ = RA.Models.JsonV2;
using MJQ = RA.Models.JsonV2.QData;
using BNode = RA.Models.JsonV2.BlankNode;
using InputAddress = RA.Models.JsonV2.Place;
using workIT.Models;
using MC = workIT.Models.Common;
using MCQ = workIT.Models.QData;
using WPM = workIT.Models.ProfileModels;
using workIT.Factories;
using workIT.Utilities;
using workIT.Services;
using workIT.Data.Tables;
using Nest;
using System.Web.UI.HtmlControls;

namespace Import.Services
{
	public class MappingHelperV3
	{
		public List<BNode> entityBlankNodes = new List<BNode>();
		public string DefaultLanguage = "en";
		public List<MC.EntityLanguageMap> LanguageMaps = new List<MC.EntityLanguageMap>();
		public MC.EntityLanguageMap LastEntityLanguageMap = new MC.EntityLanguageMap();
		public string lastLanguageMapString = "";
		public string lastLanguageMapListString = "";
		public MC.BaseObject currentBaseObject = new MC.BaseObject();
		public Guid CurrentOwningAgentUid;
		public int CurrentEntityTypeId = 0;
		public string CurrentEntityCTID = "";
		public string CurrentEntityName = "";
		public MappingHelperV3()
		{
		}
		public MappingHelperV3( int entityTypeId )
		{
			CurrentEntityTypeId = entityTypeId;
			entityBlankNodes = new List<BNode>();
		}

		#region handle blank nodes
		public MJ.BlankNode GetBlankNode( string idurl )
		{
			if ( string.IsNullOrWhiteSpace( idurl ) )
				return null;
			var node = entityBlankNodes.FirstOrDefault( s => s.BNodeId == idurl );
			return node;
		}
		public int GetBlankNodeEntityType( BNode node, ref bool isQAOrgType )
		{
			int entityTypeId = 0;
			isQAOrgType = false;

			switch ( node.Type.ToLower() )
			{
				case "ceterms:credentialorganization":
					entityTypeId = 2;
					break;
				case "ceterms:qacredentialorganization":
					//what distinctions do we need for QA orgs?
					entityTypeId = 2;
					isQAOrgType = true;

					break;
				case "ceterms:organization":
					entityTypeId = 2;
					break;
				case "ceterms:assessmentprofile":
					entityTypeId = 3;
					break;
				case "ceterms:learningopportunityprofile":
					entityTypeId = 7;
					break;
				case "ceterms:conditionmanifest":
					entityTypeId = 19;
					break;
				case "ceterms:costmanifest":
					entityTypeId = 20;
					break;
				case "ceasn:competencyframework":
					entityTypeId = 10;
					break;
				case "skos:conceptscheme":
					entityTypeId = 11;
					break;
				default:
					//default to credential
					entityTypeId = 1;
					break;
			}
			return entityTypeId;
		}

		#endregion


		#region  mapping Language maps
		public string HandleBNodeLanguageMap( MJ.LanguageMap map, string property, bool savingLastEntityMap = false, string languageCode = "en" )
		{
			string output = "";
			lastLanguageMapString = "";
			LastEntityLanguageMap = new MC.EntityLanguageMap();
			if ( map == null || map.Count == 0 )
			{
				return "";
			}

			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( map.Count == 1 )
			{
				output = map.FirstOrDefault().Value;
			}
			else
			{
				elm.HasMultipleLanguages = true;
				//eventually do a search, start with taking first
				//test ToString which checks for partial matches
				output = map.ToString( languageCode );
				if ( !string.IsNullOrWhiteSpace( output ) )
				{

				}
				else
				{
					foreach ( var item in map )
					{
						cntr++;
						if ( cntr == 1 )
						{
							//may want to do something with the language - as could be non-english
							output = item.Value;
							//bo.FirstLanguage = item.Key;
							//map.Remove( item.Key );
							break;
						}
					}
				}
			}
			return output;
		}
		public string HandleLanguageMap( MJ.LanguageMap map, string property, bool savingLastEntityMap = true, string languageCode = "en-US" )
		{
			return HandleLanguageMap( map, currentBaseObject, property, savingLastEntityMap, languageCode );
		}
		public string HandleLanguageMap( MJ.LanguageMap map, MC.BaseObject bo, string property, bool savingLastEntityMap = true, string languageCode = "en-US" )
		{
			string output = "";
			lastLanguageMapString = "";
			LastEntityLanguageMap = new MC.EntityLanguageMap();
			if ( map == null || map.Count == 0 )
			{
				return "";
			}

			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( map.Count == 1 )
			{
				output = map.FirstOrDefault().Value;
				bo.FirstLanguage = map.FirstOrDefault().Key;
				//map.Remove( map.FirstOrDefault().Key );

			}
			else
			{
				elm.HasMultipleLanguages = true;
				//eventually do a search, start with taking first
				//test ToString which checks for partial matches
				output = map.ToString( languageCode );
				if ( !string.IsNullOrWhiteSpace( output ) )
				{
					//not useful
					bo.FirstLanguage = languageCode;
					//this won't work if found with a partial check!
					//map.Remove( languageCode );
				}
				else
				{
					foreach ( var item in map )
					{
						cntr++;
						if ( cntr == 1 )
						{
							//may want to do something with the language - as could be non-english
							output = item.Value;
							bo.FirstLanguage = item.Key;
							//map.Remove( item.Key );
							break;
						}
					}
				}
			}
			if ( map.Count > 0 )
			{
				elm.EntityUid = bo.RowId;
				elm.Property = property;
				//don't have to convert if just serializing
				lastLanguageMapString = elm.LanguageMapString = JsonConvert.SerializeObject( map, GetJsonSettings() );
				elm.LanguageMap = ConvertLanguageMap( map );
				LastEntityLanguageMap = elm;
				//if removing the default item, this will be always true
				//elm.HasMultipleLanguages = cntr > 1;
				if ( savingLastEntityMap )
				{
					bo.LanguageMaps.Add( elm );
					//OR - so don't have to pass the baseObject!
					LanguageMaps.Add( elm );
				}
			}

			return output;
		}
		public string HandleLanguageMap( MJ.LanguageMap map, MC.BaseObject bo, string property, ref string langMapString, bool savingLastEntityMap = true )
		{
			string output = "";
			langMapString = lastLanguageMapString = "";
			LastEntityLanguageMap = new MC.EntityLanguageMap();
			if ( map == null || map.Count == 0 )
			{
				return "";
			}

			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( map.Count == 1 )
			{
				output = map.FirstOrDefault().Value;
				bo.FirstLanguage = map.FirstOrDefault().Key;
				//map.Remove( map.FirstOrDefault().Key );

			}
			else
			{
				elm.HasMultipleLanguages = true;
				//eventually do a search, start with taking first
				//test ToString which checks for partial matches
				output = map.ToString( DefaultLanguage );
				if ( !string.IsNullOrWhiteSpace( output ) )
				{
					//not useful
					bo.FirstLanguage = DefaultLanguage;
					//this won't work if found with a partial check!
					//map.Remove( DefaultLanguage );
				}
				else
				{
					foreach ( var item in map )
					{
						cntr++;
						if ( cntr == 1 )
						{
							//may want to do something with the language - as could be non-english
							output = item.Value;
							bo.FirstLanguage = item.Key;
							//map.Remove( item.Key );
							break;
						}
					}
				}
			}
			if ( map.Count > 0 )
			{
				elm.EntityUid = bo.RowId;
				elm.Property = property;
				//don't have to convert if just serializing
				langMapString = lastLanguageMapString = elm.LanguageMapString = JsonConvert.SerializeObject( map, GetJsonSettings() );
				elm.LanguageMap = ConvertLanguageMap( map );
				LastEntityLanguageMap = elm;
				//if removing the default item, this will be always true
				//elm.HasMultipleLanguages = cntr > 1;
				if ( savingLastEntityMap )
				{
					bo.LanguageMaps.Add( elm );
					//OR - so don't have to pass the baseObject!
					LanguageMaps.Add( elm );
				}
			}

			return output;
		}

		public List<WPM.TextValueProfile> MapToTextValueProfile( MJ.LanguageMapList list, MC.BaseObject bo, string property, bool savingLastEntityMap = true, string languageCode = "en" )
		{
			List<WPM.TextValueProfile> output = new List<WPM.TextValueProfile>();
			lastLanguageMapListString = "";
			if ( list == null || list.Count < 1 )
			{
				return output;
			}

			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( list.Count > 1 )
				elm.HasMultipleLanguages = true;

			List<string> codes = list.ToList( languageCode );
			if ( codes != null && codes.Count > 0 )
			{
				foreach ( var item in codes )
				{
					var tvp = new WPM.TextValueProfile { TextValue = item };
					output.Add( tvp );
				}
			}
			else
			{
				foreach ( var item in list )
				{
					cntr++;
					if ( cntr == 1 )
					{
						codes = item.Value;
						foreach ( var c in codes )
						{
							var tvp2 = new WPM.TextValueProfile { TextValue = c };
							output.Add( tvp2 );
						}
						list.Remove( item.Key );
						break;
					}

				}
			}
			if ( elm.HasMultipleLanguages )
			{
				elm.EntityUid = bo.RowId;
				elm.Property = property;
				//don't have to convert if just serializing
				lastLanguageMapListString = elm.LanguageMapListString = JsonConvert.SerializeObject( list, GetJsonSettings() );
				elm.LanguageMapList = ConvertLanguageMapList( list );
				if ( savingLastEntityMap )
				{
					bo.LanguageMaps.Add( elm );
					//OR - so don't have to pass the baseObject!
					LanguageMaps.Add( elm );
				}
			}
			return output;
		}

		public List<string> HandleLanguageMapList( MJ.LanguageMapList list, MC.BaseObject bo, string languageCode = "en" )
		{
			var output = new List<string>();
			if ( list == null )
				return null;
			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( list.Count > 1 )
				elm.HasMultipleLanguages = true;
			List<string> codes = list.ToList( languageCode );
			if ( codes != null && codes.Count > 0 )
			{
				foreach ( var item in codes )
				{
					if ( !string.IsNullOrWhiteSpace( item ) )
						output.Add( item );
				}
			}
			else
			{
				foreach ( var item in list )
				{
					cntr++;
					if ( cntr == 1 )
					{
						codes = item.Value;
						foreach ( var c in codes )
						{
							if ( !string.IsNullOrWhiteSpace( c ) )
								output.Add( c );
						}
						list.Remove( item.Key );
						break;
					}

				}
			}

			//if ( elm.HasMultipleLanguages )
			//{
			//	elm.EntityUid = bo.RowId;
			//	elm.Property = property;
			//	//don't have to convert if just serializing
			//	lastLanguageMapListString = elm.LanguageMapListString = JsonConvert.SerializeObject( list, GetJsonSettings() );
			//	elm.LanguageMapList = ConvertLanguageMapList( list );
			//	if ( savingLastEntityMap )
			//	{
			//		bo.LanguageMaps.Add( elm );
			//		//OR - so don't have to pass the baseObject!
			//		LanguageMaps.Add( elm );
			//	}
			//}
			return output;
		}
		public MC.LanguageMap ConvertLanguageMap( MJ.LanguageMap map )
		{
			MC.LanguageMap output = new MC.LanguageMap();
			if ( map == null )
				return null;

			foreach ( var item in map )
			{
				output.Add( item.Key, item.Value );
			}

			return output;
		}
		public MC.LanguageMapList ConvertLanguageMapList( MJ.LanguageMapList map )
		{
			MC.LanguageMapList output = new MC.LanguageMapList();
			if ( map == null )
				return null;

			foreach ( var item in map )
			{
				output.Add( item.Key, item.Value );
			}

			return output;
		}
		#endregion

		#region QuantitativeValue 
		/// <summary>
		/// Special method to use with DataProfile
		/// </summary>
		/// <param name="input"></param>
		/// <param name="property"></param>
		/// <param name="label"></param>
		/// <param name="qdSummary"></param>
		/// <param name="isCreditValue"></param>
		/// <returns></returns>
		public List<MC.QuantitativeValue> HandleQuantitiveValueList( List<MJ.QuantitativeValue> input, string property, string label, ref MCQ.DataProfileJson qdSummary, bool isCreditValue = false )
		{
			var output = HandleQuantitiveValueList( input, property, isCreditValue );
			if ( output != null )
			{
				qdSummary.Outcomes.Add( new MCQ.DataProfileOutcomes()
				{
					Label = label,
					Outcome = output
				} );
			}
			return output;
		}
		/// <summary>
		/// Map QuantitativeValue
		/// </summary>
		/// <param name="input"></param>
		/// <param name="property"></param>
		/// <param name="isCreditValue"></param>
		/// <returns></returns>
		public List<MC.QuantitativeValue> HandleQuantitiveValueList( List<MJ.QuantitativeValue> input, string property, bool isCreditValue = true )
		{
			var list = new List<MC.QuantitativeValue>();
			MC.QuantitativeValue output = new MC.QuantitativeValue();
			if ( input == null || input.Count == 0 )
			{
				return null;
			}

			foreach ( var item in input )
			{
				output = new MC.QuantitativeValue();
				output = HandleQuantitiveValue( item, property, isCreditValue );
				if ( output != null )
					list.Add( output );
			}
			return list;
		}
		public MC.QuantitativeValue HandleQuantitiveValue( MJ.QuantitativeValue input, string property, bool isCreditValue = true )
		{
			MC.QuantitativeValue output = new MC.QuantitativeValue();
			if ( input == null )
			{
				return output;
			}

			if ( input.Value == 0 && input.MinValue == 0 && input.MaxValue == 0
				&& ( input.Description == null || input.Description.Count() == 0 )
				&& ( input.UnitText == null ) //unitText should not be present by itself anyway
				)
			{
				//should log - although API should not allow this?
				return output;
			}
			output.Value = input.Value;
			output.MinValue = input.MinValue;
			output.MaxValue = input.MaxValue;
			output.Percentage = input.Percentage;
			output.Description = HandleLanguageMap( input.Description, property );
			//need to distinguish QV that uses creditvalue
			if ( isCreditValue )
			{
				output.CreditUnitType = MapCAOToEnumermation( input.UnitText );
				if ( output.CreditUnitType.HasItems() )
				{
					output.UnitText = output.CreditUnitType.GetFirstItem().Name;
				}
			} else
			{
				output.UnitText = (input.UnitText ?? new MJ.CredentialAlignmentObject())	.TargetNodeName.ToString();
			}

			return output;
		}

		#endregion
		#region ValueProfile
		/// <summary>
		/// Temporary
		/// Do mapping of ValueProfile to QuantitativeValue 
		/// </summary>
		/// <param name="list"></param>
		/// <param name="property"></param>
		/// <param name="isCreditValue"></param>
		/// <returns></returns>
		public List<MC.QuantitativeValue> HandleValueProfileListToQVList( List<MJ.ValueProfile> list, string property, bool isCreditValue = true )
		{
			var output = new List<MC.QuantitativeValue>();
			MC.QuantitativeValue profile = new MC.QuantitativeValue();
			if ( list == null || list.Count == 0 )
			{
				return output;
			}

			foreach ( var input in list )
			{
				profile = new MC.QuantitativeValue();
				//
				if ( input.Value == 0 && input.MinValue == 0 && input.MaxValue == 0 && input.Percentage == 0
				&& ( input.Description == null || input.Description.Count() == 0 )
				)
				{
					//should log - although API should not allow this?
					return null;
				}
				profile.Value = input.Value;
				profile.MinValue = input.MinValue;
				profile.MaxValue = input.MaxValue;
				profile.Percentage = input.Percentage;
				profile.Description = HandleLanguageMap( input.Description, property );
				//
				profile.CreditUnitType = MapCAOListToEnumermation( input.CreditUnitType );
				//profile.CreditLevelType = MapCAOListToEnumermation( input.CreditLevelType );

				//
				if ( profile != null )
					output.Add( profile );
			}


			return output;
		}

		public List<MC.ValueProfile> HandleValueProfileList( List<MJ.ValueProfile> input, string property )
		{
			var list = new List<MC.ValueProfile>();
			MC.ValueProfile output = new MC.ValueProfile();
			if ( input == null || input.Count == 0 )
			{
				return list;
			}
			foreach ( var item in input )
			{
				output = new MC.ValueProfile();
				output = HandleValueProfile( item, property );
				if ( output != null )
					list.Add( output );
			}
			return list;
		}
		public MC.ValueProfile HandleValueProfile( MJ.ValueProfile input, string property )
		{
			MC.ValueProfile output = new MC.ValueProfile();
			if ( input == null )
			{
				return null;
			}

			if ( input.Value == 0 && input.MinValue == 0 && input.MaxValue == 0 && input.Percentage == 0
			&& ( input.Description == null || input.Description.Count() == 0 )
				)
			{
				//should log - although API should not allow this?
				return null;
			}
			output.Value = input.Value;
			output.MinValue = input.MinValue;
			output.MaxValue = input.MaxValue;
			output.Percentage = input.Percentage;
			output.Description = HandleLanguageMap( input.Description, property );
			//
			output.CreditUnitType = MapCAOListToEnumermation( input.CreditUnitType );
			output.CreditLevelType = MapCAOListToEnumermation( input.CreditLevelType );
			output.Subject = MapCAOListToEnumermation( input.Subject );
			return output;
		}

		#endregion
		#region  mapping IdProperty
		public string MapIdentityToString( MJ.IdProperty property )
		{
			if ( property == null )
				return "";
			else
				return property.Id;
		}
		public string MapIdentityListToString( List<MJ.IdProperty> property )
		{
			if ( property == null || property.Count == 0 )
				return "";
			else //assuming these types will only have one entry
				return property[ 0 ].Id;
		}

		public List<string> MapIdentityListToList( List<MJ.IdProperty> property )
		{
			List<string> list = new List<string>();
			if ( property == null || property.Count == 0 )
				return list;

			foreach ( var item in property )
			{
				list.Add( item.Id );
			}
			return list;
		} //
		#endregion

		#region  IdentifierValue
		public string MapIdentifierValueListToString( List<MJ.IdentifierValue> property )
		{
			if ( property == null || property.Count == 0 )
				return "";
			//assuming these types will only have one entry for now
			//if more than one, set message
			if (property.Count > 1)
			{

			}

			var input = property[ 0 ];
			
			if ( input.IdentifierTypeName != null )
				return HandleLanguageMap( input.IdentifierTypeName, "IdentifierTypeName",false ); 
			//else if ( !string.IsNullOrWhiteSpace( property[ 0 ].IdentifierTypeName ) )
			//	return property[ 0 ].IdentifierTypeName;
			else if ( !string.IsNullOrWhiteSpace( input.IdentifierValueCode ) )
				return input.IdentifierValueCode;
			else
				return "";
		}

		public List<WPM.Entity_IdentifierValue> MapIdentifierValueList( List<MJ.IdentifierValue> property )
		{
			List<WPM.Entity_IdentifierValue> list = new List<WPM.Entity_IdentifierValue>();
			if ( property == null || property.Count == 0 )
				return list;
			WPM.Entity_IdentifierValue iv = new WPM.Entity_IdentifierValue();
			foreach ( var item in property )
			{
				iv = new WPM.Entity_IdentifierValue()
				{
					IdentifierType = item.IdentifierType,
					IdentifierValueCode = item.IdentifierValueCode,
					Name = HandleLanguageMap( item.IdentifierTypeName, "IdentifierTypeName", false )
					//Description = HandleLanguageMap( item.Description, currentBaseObject, "IdentifierValue Description", true )
				};
				list.Add( iv );
			}

			return list;
		}
		public List<MC.IdentifierValue> MapIdentifierValueListInternal( List<MJ.IdentifierValue> property )
		{
			var list = new List<MC.IdentifierValue>();
			if ( property == null || property.Count == 0 )
				return list;
			MC.IdentifierValue iv = new MC.IdentifierValue();
			foreach ( var item in property )
			{
				iv = new MC.IdentifierValue()
				{
					IdentifierType = item.IdentifierType,
					IdentifierValueCode = item.IdentifierValueCode,
					IdentifierTypeName = HandleLanguageMap( item.IdentifierTypeName, "IdentifierTypeName", false )
					
				};
				list.Add( iv );
			}

			return list;
		}
		#endregion

		#region  TextValueProfile
		public List<WPM.TextValueProfile> MapToTextValueProfile( List<string> list )
		{
			List<WPM.TextValueProfile> output = new List<WPM.TextValueProfile>();
			if ( list == null || list.Count < 1 )
				return output;

			foreach ( var item in list )
			{
				var tvp = new WPM.TextValueProfile { TextValue = item };
				output.Add( tvp );
			}
			return output;
		}

		#endregion

		#region  CredentialAlignmentObject
		public MC.Enumeration MapCAOListToEnumermation( List<MJ.CredentialAlignmentObject> input )
		{
			//TBD = do we need anything for emumeration, or just items?
			MC.Enumeration output = new workIT.Models.Common.Enumeration();
			if ( input == null || input.Count == 0 )
				return output;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item == null )
					continue;
				//temp workaround, these would have to be handled very differently (i.e. retrieving from DB)
				//could add to the Enumeration
				string nodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );

				if ( item != null && ( item.TargetNode != null || !string.IsNullOrEmpty( nodeName ) ) )
				{
					output.Items.Add( new workIT.Models.Common.EnumeratedItem()
					{
						SchemaName = item.TargetNode ?? "",
						Name = nodeName ?? "",
						LanguageMapString = lastLanguageMapString
					} );
				}

			}
			return output;
		}
		public List<string> MapCAOToList( MJ.CredentialAlignmentObject input )
		{
			var output = new List<string>();
			if ( input == null )
				return output;

			if ( input != null
				&& ( input.TargetNode != null
				|| ( input.TargetNodeName != null && input.TargetNodeName.Count > 0 ) )
				)
			{
				string nodeName = HandleLanguageMap( input.TargetNodeName, currentBaseObject, "TargetNodeName", false );
				output.Add( nodeName );
			}
			return output;
		}
		public List<string> MapCAOListToList( List<MJ.CredentialAlignmentObject> input )
		{
			var output = new List<string>();
			if ( input == null || input.Count == 0 )
				return output;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item == null )
					continue;
				//temp workaround, these would have to be handled very differently (i.e. retrieving from DB)
				//could add to the Enumeration
				string nodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );

				if ( !string.IsNullOrEmpty( nodeName ) )
				{
					output.Add( nodeName );
				}
			}
			return output;
		}
		public MC.Enumeration MapCAOToEnumermation( MJ.CredentialAlignmentObject input )
		{
			//TBD = do we need anything for emumeration, or just items?
			MC.Enumeration output = new workIT.Models.Common.Enumeration();
			if ( input == null )
				return output;

			if ( input != null
				&& ( input.TargetNode != null
				|| ( input.TargetNodeName != null && input.TargetNodeName.Count > 0 ) )
				)
			{
				string nodeName = HandleLanguageMap( input.TargetNodeName, currentBaseObject, "TargetNodeName", false );
				output.Items.Add( new workIT.Models.Common.EnumeratedItem()
				{
					SchemaName = input.TargetNode ?? "",
					Name = nodeName ?? "",
					LanguageMapString = lastLanguageMapString
				} );
			}

			return output;
		}
		public string MapCAOToString( MJ.CredentialAlignmentObject input )
		{
			//
			if ( input == null )
				return "";
			var output = "";
			if ( input != null
				&& ( input.TargetNode != null
				|| ( input.TargetNodeName != null && input.TargetNodeName.Count > 0 ) )
				)
			{
				output = HandleLanguageMap( input.TargetNodeName, currentBaseObject, "TargetNodeName", false );
			}

			return output;
		}
		public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCAOProfileList( List<MJ.CredentialAlignmentObject> input )
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
			MC.CredentialAlignmentObjectProfile entity = new MC.CredentialAlignmentObjectProfile();

			if ( input == null || input.Count == 0 )
				return output;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item == null )
					continue;
				string targetNodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );
				//18-12-06 mp - not sure if we should skip if targetNode is missing? We don't do anything with it directly.
				//item.TargetNode != null &&
				if ( item != null && ( !string.IsNullOrEmpty( targetNodeName ) ) )
				{

					entity = new MC.CredentialAlignmentObjectProfile()
					{
						TargetNode = item.TargetNode ?? "",
						CodedNotation = item.CodedNotation ?? "",
						FrameworkName = HandleLanguageMap( item.FrameworkName, currentBaseObject, "FrameworkName" ),
						//won't know if url or registry uri
						//SourceUrl = item.Framework,
						Weight = item.Weight
						//Weight = StringtoDecimal( item.Weight )
					};

					//entity.TargetNodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", false );
					entity.TargetNodeName = targetNodeName;
					entity.TargetNodeName_Map = lastLanguageMapString;
					entity.TargetNodeDescription = HandleLanguageMap( item.TargetNodeDescription, currentBaseObject, "TargetNodeDescription", false );
					entity.TargetNodeDescription_Map = lastLanguageMapString;

					if ( !string.IsNullOrWhiteSpace( item.Framework ) )
					{
						if ( item.Framework.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) == -1
							&& item.Framework.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) == -1 )
						{
							entity.SourceUrl = item.Framework;
						}
						else
						{
							entity.FrameworkUri = item.Framework;
						}
					}
					output.Add( entity );
				}

			}
			return output;
		}

		public List<MC.CredentialAlignmentObjectProfile> AppendLanguageMapListToCAOProfileList( MJ.LanguageMapList input, string languageCode = "en" )
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
			MC.CredentialAlignmentObjectProfile entity = new MC.CredentialAlignmentObjectProfile();

			if ( input == null || input.Count == 0 )
				return output;
			int cntr = 0;
			MC.EntityLanguageMap elm = new MC.EntityLanguageMap();
			if ( input.Count > 1 )
				elm.HasMultipleLanguages = true;

			List<string> properties = input.ToList( languageCode );
			//focus on default language
			if ( properties != null && properties.Count > 0 )
			{
				foreach ( var item in properties )
				{
					if ( item == null )
						continue;
					if ( string.IsNullOrWhiteSpace( item ) )
						continue;
					entity = new MC.CredentialAlignmentObjectProfile()
					{
						TargetNodeName = item
					};
					output.Add( entity );
				}
			}
			else
			{
				foreach ( var item in input )
				{
					cntr++;
					if ( cntr == 1 )
					{
						properties = item.Value;
						foreach ( var c in properties )
						{
							if ( string.IsNullOrWhiteSpace( c ) )
								continue;
							entity = new MC.CredentialAlignmentObjectProfile()
							{
								TargetNodeName = c
							};
							output.Add( entity );
						}
						input.Remove( item.Key );
						break;
					}
				}
			}
			return output;
		}   //

		//seems same as MapCAOListToCAOProfileList, so using latter

		//public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCompetencies( List<MJ.CredentialAlignmentObject> input )
		//{
		//    List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
		//    MC.CredentialAlignmentObjectProfile cao = new MC.CredentialAlignmentObjectProfile();

		//    if ( input == null || input.Count == 0 )
		//        return output;

		//    foreach ( MJ.CredentialAlignmentObject item in input )
		//    {
		//        if ( item != null && !string.IsNullOrEmpty( item.TargetNodeName ) )
		//        {
		//            cao = new MC.CredentialAlignmentObjectProfile()
		//            {
		//                TargetNodeName = item.TargetNodeName,
		//                TargetNodeDescription = item.TargetNodeDescription,
		//                TargetNode = item.TargetNode,
		//                CodedNotation = item.CodedNotation,
		//                FrameworkName = item.FrameworkName,
		//                //FrameworkUrl = item.Framework,
		//                Weight = item.Weight
		//                //Weight = StringtoDecimal(item.Weight)
		//            };
		//            //Framework willl likely be a registry url, so should be saved as FrameworkUri. The SourceUrl will be added from a download of the actual framework
		//            if ( !string.IsNullOrWhiteSpace( item.Framework ) )
		//            {
		//                if ( item.Framework.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) == -1 )
		//                {
		//                    cao.SourceUrl = item.Framework;
		//                }
		//                else
		//                {
		//                    cao.FrameworkUri = item.Framework;
		//                }
		//            }
		//            output.Add( cao );
		//        }

		//    }
		//    return output;
		//}
		public List<WPM.TextValueProfile> MapCAOListToTextValueProfile( List<MJ.CredentialAlignmentObject> input, int categoryId )
		{
			List<WPM.TextValueProfile> list = new List<WPM.TextValueProfile>();
			if ( input == null || input.Count == 0 )
				return list;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item == null )
					continue;
				if ( item != null && item.TargetNodeName != null && item.TargetNodeName.Count > 0 )
				{
					string nodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", false );

					list.Add( new WPM.TextValueProfile()
					{
						CategoryId = categoryId,
						//TextTitle = item.TargetNodeName,
						TextValue = nodeName,
						TextValue_Map = lastLanguageMapString
					} );
				}

			}
			return list;
		}
		public string MapListToString( List<string> property )
		{

			if ( property == null || property.Count == 0 )
				return "";
			//assuming only handling first one
			return property[ 0 ];
		} //
		#endregion

		#region  Addresses, contact point, jurisdiction

		/// <summary>
		/// oCT. 20, 2017 - AvailableAt is now the same as address. 
		/// Not sure of its fate
		/// </summary>
		/// <param name="addresses"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<MC.Address> FormatAvailableAtAddresses( List<MJ.Place> addresses, ref SaveStatus status )
		{

			List<MC.Address> list = new List<MC.Address>();
			MC.Address output = new MC.Address();
			if ( addresses == null || addresses.Count == 0 )
				return list;

			MC.ContactPoint cp = new MC.ContactPoint();

			foreach ( var item in addresses )
			{
				output = MapAddress( item );

				if ( item.ContactPoint != null && item.ContactPoint.Count > 0 )
				{
					foreach ( var cpi in item.ContactPoint )
					{
						cp = new MC.ContactPoint
						{
							Name = HandleLanguageMap( cpi.Name, currentBaseObject, "ContactPointName", false ),
							Name_Map = lastLanguageMapString,
							ContactType = HandleLanguageMap( cpi.ContactType, currentBaseObject, "ContactPointContactType", false ),
							ContactType_Map = lastLanguageMapString,

							//cp.ContactOption = MapListToString( cpi.ContactOption );
							PhoneNumbers = cpi.PhoneNumbers,
							Emails = cpi.Emails,
							SocialMediaPages = cpi.SocialMediaPages
						};

						output.ContactPoint.Add( cp );
					}
				}
				list.Add( output );
				//}
			}

			return list;
		} //

		/// <summary>
		/// Same as FormatAvailableAtAddresses - removing
		/// </summary>
		/// <param name="addresses"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<MC.Address> FormatAddresses( List<InputAddress> addresses, ref SaveStatus status )
		{

			List<MC.Address> list = new List<MC.Address>();
			MC.Address output = new MC.Address();
			if ( addresses == null || addresses.Count == 0 )
				return list;

			MC.ContactPoint cp = new MC.ContactPoint();

			foreach ( var a in addresses )
			{
				output = MapAddress( a );

				if ( a.ContactPoint != null && a.ContactPoint.Count > 0 )
				{
					foreach ( var cpi in a.ContactPoint )
					{
						cp = new MC.ContactPoint()
						{
							Name = HandleLanguageMap( cpi.Name, currentBaseObject, "ContactPointName", false ),
							Name_Map = lastLanguageMapString,
							ContactType = HandleLanguageMap( cpi.ContactType, currentBaseObject, "ContactPointContactType", false ),
							ContactType_Map = lastLanguageMapString
						};
						//cp.ContactOption = MapListToString( cpi.ContactOption );
						cp.PhoneNumbers = cpi.PhoneNumbers;
						cp.Emails = cpi.Emails;
						cp.SocialMediaPages = cpi.SocialMediaPages;

						output.ContactPoint.Add( cp );
					}
				}
				list.Add( output );

			}

			return list;
		} //

		public List<MC.JurisdictionProfile> MapToJurisdiction( List<MJ.JurisdictionProfile> jps, ref SaveStatus status )
		{
			return MapToJurisdiction( "", jps, ref status );
		}
		public List<MC.JurisdictionProfile> MapToJurisdiction( string property, List<MJ.JurisdictionProfile> jps, ref SaveStatus status )
		{
			var list = new List<MC.JurisdictionProfile>();
			if ( jps == null || jps.Count == 0 )
				return list;

			var njp = new MC.JurisdictionProfile();
			var gc = new MC.GeoCoordinates();
			bool hasMainJurisdiction = false;
			foreach ( var jp in jps )
			{
				njp = new MC.JurisdictionProfile();
				hasMainJurisdiction = false;
				//string assertedByReference = "";
				if ( jp.AssertedBy != null )
				{
					//note CTDL has assertedBy as a list, but we are only taking the first one
					njp.AssertedByList = MapOrganizationReferenceGuids( property + ".JurisdictionProfile.AssertedBy", jp.AssertedBy, ref status );
				}

				//make sure there is data: one of description, global, or main jurisdiction
				njp.Description = HandleLanguageMap( jp.Description, currentBaseObject, "jp.Description", false );
				njp.Description_Map = lastLanguageMapString;

				//NEED to handle at least the main jurisdiction
				foreach ( var item in jp.MainJurisdiction )
				{
					gc = new MC.GeoCoordinates();

					//should only be one, just saving last one in case of unexpected data
					gc = ResolveGeoCoordinates( item );

					if ( !string.IsNullOrWhiteSpace( item.PostalCode ) )
					{
						gc.Address = MapAddress( item );
						//break;
					}

					njp.MainJurisdiction = gc;
					//njp.GlobalJurisdiction = null;
					hasMainJurisdiction = true;
				}

				if ( hasMainJurisdiction )
					njp.GlobalJurisdiction = false;

				else if ( jp.GlobalJurisdiction.HasValue )
				{
					njp.MainJurisdiction = null;
					njp.GlobalJurisdiction = jp.GlobalJurisdiction;
				}
				else
				{
					//must have a description
					if ( string.IsNullOrWhiteSpace( njp.Description ) )
					{
						status.AddWarning( "Warning - invalid/incomplete jurisdiction: " + property );
						break;
					}
				}

				//and exceptions
				if ( jp.JurisdictionException == null || jp.JurisdictionException.Count == 0 )
					njp.JurisdictionException = null;
				else
				{

					foreach ( var item in jp.JurisdictionException )
					{
						gc = new MC.GeoCoordinates();
						gc = ResolveGeoCoordinates( item );
						if ( !string.IsNullOrWhiteSpace( item.PostalCode ) )
						{
							gc.Address = MapAddress( item );
							//break;
						}
						njp.JurisdictionException.Add( gc );
					}
				}

				if ( njp != null )
					list.Add( njp );
			}

			return list;
		} //

		private MC.GeoCoordinates ResolveGeoCoordinates( MJ.Place input )
		{
			var gc = new MC.GeoCoordinates();
			gc.Name = HandleLanguageMap( input.Name, currentBaseObject, "PlaceName", false );
			gc.Name_Map = lastLanguageMapString;

			if ( !string.IsNullOrWhiteSpace( input.GeoURI ) )
			{
				gc.GeoURI = input.GeoURI;
				//check for existing GeoCoordinates with this GeoNameId				
				gc = Entity_JurisdictionProfileManager.GeoCoordinates_GetByUrl( input.GeoURI );
				if ( gc != null && gc.Id > 0 )
				{
					gc.Id = 0;
					gc.ParentId = 0;
					return gc;
				}
				else
				{
					//need to call geo API to resolve the endpoint
					string geoNamesId = UtilityManager.ExtractNameValue( input.GeoURI, ".org", "/", "/" );
					if ( BaseFactory.IsInteger( geoNamesId ) )
					{
						gc = ThirdPartyApiServices.GeoNamesGet( geoNamesId );
						if ( gc != null && gc.GeoNamesId > 0 )
						{
							gc.Id = 0;
							gc.ParentId = 0;
							return gc;
						}
					}
				}
				//else
				//{
				//	//gc = new MC.GeoCoordinates { GeoURI = input.GeoURI };
				//}


			}
			else
			{
				//can we have different lat/lng for the same geoUri?
				gc.Latitude = input.Latitude;
				gc.Longitude = input.Longitude;


			}
			return gc;
		}
		private MC.Address MapAddress( InputAddress input )
		{

			//20-08-23 NOTE - partial addresses are allowed
			MC.Address output = new MC.Address()
			{
				PostalCode = input.PostalCode,
				Latitude = input.Latitude,
				Longitude = input.Longitude
			};
			output.Name = HandleLanguageMap( input.Name, currentBaseObject, "PlaceName", false );
			output.Name_Map = lastLanguageMapString;

			output.Address1 = HandleLanguageMap( input.StreetAddress, currentBaseObject, "StreetAddress", false );
			output.Address1_Map = lastLanguageMapString;

			output.City = HandleLanguageMap( input.City, currentBaseObject, "City", false );
			output.City_Map = lastLanguageMapString;

			output.AddressRegion = HandleLanguageMap( input.AddressRegion, currentBaseObject, "AddressRegion", false );
			if ( output.AddressRegion.Length < 3 )
				output.HasShortRegion = true;

			output.AddressRegion_Map = lastLanguageMapString;
			output.SubRegion = HandleLanguageMap( input.SubRegion, currentBaseObject, "SubRegion", false );

			output.Country = HandleLanguageMap( input.Country, currentBaseObject, "Country", false );
			output.Country_Map = lastLanguageMapString;
			return output;
		}

		#endregion

		#region  Process profile
		public List<WPM.ProcessProfile> FormatProcessProfile( List<MJ.ProcessProfile> profiles, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var output = new List<WPM.ProcessProfile>();
			foreach ( var input in profiles )
			{
				Guid rowId = Guid.NewGuid();
				var profile = new WPM.ProcessProfile
				{
					RowId = rowId,
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status )
				};
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
				profile.ProcessFrequency = HandleLanguageMap( input.ProcessFrequency, profile, "ProcessFrequency", true );
				profile.ProcessMethodDescription = HandleLanguageMap( input.ProcessMethodDescription, profile, "ProcessMethodDescription", true );
				profile.ProcessStandardsDescription = HandleLanguageMap( input.ProcessStandardsDescription, profile, "ProcessStandardsDescription", true );
				profile.ScoringMethodDescription = HandleLanguageMap( input.ScoringMethodDescription, profile, "ScoringMethodDescription", true );
				profile.ScoringMethodExampleDescription = HandleLanguageMap( input.ScoringMethodExampleDescription, profile, "ScoringMethodExampleDescription", true );
				profile.VerificationMethodDescription = HandleLanguageMap( input.VerificationMethodDescription, profile, "VerificationMethodDescription", true );

				profile.SubjectWebpage = input.SubjectWebpage;
				profile.ExternalInputType = MapCAOListToEnumermation( input.ExternalInputType );
				profile.ProcessMethod = input.ProcessMethod ?? "";
				profile.ProcessStandards = input.ProcessStandards ?? "";
				profile.ScoringMethodExample = input.ScoringMethodExample ?? "";
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );

				//while the profiles is a list, we are only handling single
				profile.ProcessingAgentUid = MapOrganizationReferencesGuid( "ProcessProfile.ProcessingAgentUid", input.ProcessingAgent, ref status );

				//targets
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					profile.TargetCredentialIds = MapEntityReferences( "ProcessProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
					profile.TargetAssessmentIds = MapEntityReferences( "ProcessProfile.TargetAssessment", input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
				if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
					profile.TargetLearningOpportunityIds = MapEntityReferences( "ProcessProfile.TargetLearningOpportunity", input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				//if ( input.TargetCompetencyFramework != null && input.TargetCompetencyFramework.Count > 0 )
				//

				output.Add( profile );
			}

			return output;
		}
		#endregion
		#region  Earnings, Holders profile
		public List<MC.EarningsProfile> FormatEarningsProfile( List<MJ.EarningsProfile> profiles, OutcomesDTO outcomesDTO, List<BNode> bnodes, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var output = new List<MC.EarningsProfile>();
			foreach ( var input in profiles )
			{
				if ( input == null )
					continue;
				Guid rowId = Guid.NewGuid();
				var profile = new MC.EarningsProfile
				{
					RowId = rowId,
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					HighEarnings = input.HighEarnings,
					LowEarnings = input.LowEarnings,
					MedianEarnings = input.MedianEarnings,
					PostReceiptMonths = input.PostReceiptMonths
				};
				profile.Name = HandleLanguageMap( input.Name, profile, "Name", true );
				profile.Source = input.Source;
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );

				//handle: RelevantDataSet
				if ( input.RelevantDataSet != null && input.RelevantDataSet.Any() )
				{
					//this will be a list of URIs
					foreach ( var item in input.RelevantDataSet )
					{
						//could use URI, but will use ctid
						var ctid = ResolutionServices.ExtractCtid( item );
						if ( string.IsNullOrWhiteSpace( ctid ) )
						{
							status.AddError( string.Format( "Error: Unable to derive a ctid from the EarningsProfile.RelevantDataSet for EarningsProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.Ctid, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.Ctid == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.Ctid ) )
						{
							status.AddError( string.Format( "Error: Unable to find the DataSetProfile for EarningsProfile (HP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.Ctid, ctid ) );
							continue;
						}
						var dspo = FormatDataSetProfile( input.Ctid, dspi, outcomesDTO, ref status );
						//may want to store the ctid, although will likely use relationships, or store the json
						if ( dspo != null )
						{
							profile.RelevantDataSetList.Add( dspo.CTID );
							profile.RelevantDataSet.Add( dspo );
						}
					}
				}


				output.Add( profile );
			}

			return output;
		}

		public List<MC.HoldersProfile> FormatHoldersProfile( string parentCTID, List<MJ.HoldersProfile> profiles, OutcomesDTO outcomesDTO, List<BNode> bnodes, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;



			var output = new List<MC.HoldersProfile>();
			foreach ( var input in profiles )
			{
				if ( input == null )
					continue;
				Guid rowId = Guid.NewGuid();
				LoggingHelper.DoTrace( 7, string.Format( "FormatHoldersProfile. parentCTID: {0}, CTID: {1}, ", parentCTID, input.Ctid ) );

				//holders profile doesn't have a name
				var profile = new MC.HoldersProfile
				{
					RowId = rowId, //?????????????
					CTID = input.Ctid,
					Name = HandleLanguageMap( input.Name, "Name" ),
					Description = HandleLanguageMap( input.Description, "Description" ),
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					Source = input.Source,
					NumberAwarded = input.NumberAwarded
				};
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
				profile.Source = input.Source;

				//handle: RelevantDataSet
				if ( input.RelevantDataSet != null && input.RelevantDataSet.Any() )
				{
					//this will be a list of URIs
					foreach ( var item in input.RelevantDataSet )
					{
						//could use URI, but will use ctid
						var ctid = ResolutionServices.ExtractCtid( item );
						if ( string.IsNullOrWhiteSpace( ctid ) )
						{
							status.AddError( string.Format( "Error: Unable to derive a ctid from the HoldersProfile.RelevantDataSet for HoldersProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.Ctid, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.Ctid == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.Ctid) )
						{
							status.AddError( string.Format("Error: Unable to find the DataSetProfile for HoldersProfile (HP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.Ctid, ctid ));
							continue;
						}
						var dspo = FormatDataSetProfile( input.Ctid, dspi, outcomesDTO, ref status );
						//may want to store the ctid, although will likely use relationships, or store the json
						if ( dspo != null )
						{
							profile.RelevantDataSetList.Add( dspo.CTID );
							profile.RelevantDataSet.Add( dspo );
						}
					}
				}

				output.Add( profile );
			}

			return output;
		}
		//
		public MCQ.DataSetProfile FormatDataSetProfile( string parentCTID, MJ.QData.DataSetProfile input, OutcomesDTO outcomesDTO, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, string.Format( "FormatDataSetProfile. parentCTID: {0}, CTID: {1}, ", parentCTID, input.Ctid ) );


			var output = new MCQ.DataSetProfile()
			{
				CTID = input.Ctid,
				Name = HandleLanguageMap( input.Name, "Name" ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				DataSuppressionPolicy = HandleLanguageMap( input.DataSuppressionPolicy, "DataSuppressionPolicy" ),
				Source = input.Source,
				SubjectIdentification = HandleLanguageMap( input.SubjectIdentification, "SubjectIdentification" ),
			};
			output.AdministrationProcess = FormatProcessProfile( input.AdministrationProcess, ref status );
			if (input.DistributionFile != null && input.DistributionFile.Any() )
			{
				foreach(var item in input.DistributionFile)
				{
					if ( !string.IsNullOrWhiteSpace( item ) )
						output.DistributionFile.Add( item.Trim() );
				}
			}
			output.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
			output.InstructionalProgramTypes = MapCAOListToCAOProfileList( input.InstructionalProgramType );

			if ( input.DataSetTimePeriod != null && input.DataSetTimePeriod.Any() )
			{
				//this will be a list of Bnode URIs
				foreach ( var item in input.DataSetTimePeriod )
				{
					var dspi = outcomesDTO.DataSetTimeFrames.FirstOrDefault( s => s.CtdlId.ToLower() == item.ToLower() );
					if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CtdlId ) )
					{
						status.AddError( string.Format( "Error: Unable to find the DataSetTimeFrame for DataSetProfile (CTID: '{0}') using DataSetTimePeriod BNodeID: '{1}', parent CTID: '{2}'", input.Ctid, item, parentCTID ) );
						continue;
					}
					var dspo = FormatDataSetTimeFrame( input.Ctid, dspi, outcomesDTO, ref status );
					//maybe
					if ( dspo != null )
					{
						output.DataSetTimePeriodList.Add( dspo.bnID );
						output.DataSetTimePeriod.Add( dspo );
					}
				}
			}

			return output;
		}
		//
		public MCQ.DataSetTimeFrame FormatDataSetTimeFrame( string parentCTID, MJ.QData.DataSetTimeFrame input, OutcomesDTO outcomesDTO, ref SaveStatus status )
		{
			if ( input == null || string.IsNullOrWhiteSpace( input.CtdlId ) )
				return null;

			LoggingHelper.DoTrace( 7, string.Format( "FormatDataSetTimeFrame. parentCTID: {0}, bnID: {1}, ", parentCTID, input.CtdlId ) );

			var output = new MCQ.DataSetTimeFrame()
			{
				bnID = input.CtdlId,
				Name = HandleLanguageMap( input.Name, "Name" ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				StartDate = input.StartDate ?? "",
				EndDate = input.EndDate ?? "",

			};
			//to avoid more entity types, and/or codes.PropertyCategories, just store list of names (think this should be single
			//output.DataSourceCoverageType = MapCAOListToEnumermation( input.DataSourceCoverageType );
			output.DataSourceCoverageTypeList = MapCAOListToList( input.DataSourceCoverageType );

			if ( input.DataAttributes != null && input.DataAttributes.Any() )
			{
				//this will be a list bnode ids
				foreach ( var item in input.DataAttributes )
				{
					//
					var dspi = outcomesDTO.DataProfiles.FirstOrDefault( s => s.CtdlId.ToLower() == item.ToLower() );
					if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CtdlId ) )
					{
						status.AddError( string.Format( "Error: Unable to find the DataProfile for DataSetTimeFrame (BNID: '{0}') using DataProfile BNodeID: '{1}', parent CTID: '{2}'", input.CtdlId, item, parentCTID ) );
						continue;
					}
					var dspo = FormatDataProfiles( dspi, outcomesDTO, ref status );
					//may want to store the ctid, although will likely use relationships, or store the json
					if (dspo != null)
					{
						output.DataAttributesList.Add( dspo.bnID );
						output.DataAttributes.Add( dspo );
					}					
				}
			}
			return output;
		}
		//
		public MCQ.DataProfile FormatDataProfiles( MJ.QData.DataProfile input, OutcomesDTO outcomesDTO, ref SaveStatus status )
		{
			if ( input == null || string.IsNullOrWhiteSpace( input.CtdlId ) )
				return null;
			MCQ.DataProfileJson qdSummary = new MCQ.DataProfileJson();
			LoggingHelper.DoTrace( 7, "FormatDataProfiles. bnID: " + input.CtdlId );
			var output = new MCQ.DataProfile()
			{
				bnID = input.CtdlId,
				//Adjustment = HandleLanguageMap( input.Adjustment, "Adjustment" ),
				AdministrativeRecordType = MapCAOToEnumermation( input.AdministrativeRecordType ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				//EarningsDefinition = HandleLanguageMap( input.EarningsDefinition, "EarningsDefinition" ),
				//EarningsThreshold = HandleLanguageMap( input.EarningsThreshold, "EarningsThreshold" ),
				//EmploymentDefinition = HandleLanguageMap( input.EmploymentDefinition, "EmploymentDefinition" ),
				IncomeDeterminationType = MapCAOToEnumermation( input.IncomeDeterminationType ),
				//WorkTimeThreshold = HandleLanguageMap( input.WorkTimeThreshold, "WorkTimeThreshold" ),
			};
			//output.AdministrativeRecordType = MapCAOToEnumermation( input.AdministrativeRecordType );
			output.AdministrativeRecordTypeList = MapCAOToList( input.AdministrativeRecordType );
			//
			output.DataProfileAttributes = new MCQ.DataProfileAttributes()
			{
				Adjustment = HandleLanguageMap( input.Adjustment, "Adjustment" ),
				//AdministrativeRecordType = MapCAOToEnumermation( input.AdministrativeRecordType ),
				EarningsDefinition = HandleLanguageMap( input.EarningsDefinition, "EarningsDefinition" ),
				EarningsThreshold = HandleLanguageMap( input.EarningsThreshold, "EarningsThreshold" ),
				EmploymentDefinition = HandleLanguageMap( input.EmploymentDefinition, "EmploymentDefinition" ),
				//IncomeDeterminationType = MapCAOToEnumermation( input.IncomeDeterminationType ),
				WorkTimeThreshold = HandleLanguageMap( input.WorkTimeThreshold, "WorkTimeThreshold" ),
			};
			//EarningsAmount
			output.DataProfileAttributes.EarningsAmount = FormatMonetaryAmount( input.EarningsAmount, ref status );
			//EarningsDistribution
			output.DataProfileAttributes.EarningsDistribution = FormatMonetaryAmountDistribution( input.EarningsDistribution, ref status );

			//SubjectExcluded, SubjectIncluded
			//output.DataProfileAttributes.SubjectIncluded = FormatSubjectProfile( input.SubjectIncluded, ref status );
			//output.DataProfileAttributes.SubjectExcluded = FormatSubjectProfile( input.SubjectExcluded, ref status );

			//for now map to separate properties.
			output.DataProfileAttributes.DataAvailable=HandleQuantitiveValueList( input.DataAvailable, "DataProfile.DataAvailable", "Data Available", ref qdSummary, false );
			output.DataProfileAttributes.DataNotAvailable = HandleQuantitiveValueList( input.DataNotAvailable, "DataProfile.DataNotAvailable", "Data Not Available", ref qdSummary, false );

			output.DataProfileAttributes.DemographicEarningsRate = HandleQuantitiveValueList( input.DemographicEarningsRate, "DataProfile.DemographicEarningsRate", "Demographic Earnings Rate", ref qdSummary, false );
			output.DataProfileAttributes.DemographicEmploymentRate = HandleQuantitiveValueList( input.DemographicEmploymentRate, "DataProfile.DemographicEmploymentRate", "Demographic Employment Rate", ref qdSummary, false );
			output.DataProfileAttributes.EmploymentRate = HandleQuantitiveValueList( input.EmploymentRate, "DataProfile.EmploymentRate", "Employment Rate", ref qdSummary, false );
			output.DataProfileAttributes.HoldersInSet = HandleQuantitiveValueList( input.HoldersInSet, "DataProfile.HoldersInSet", "Holders In Set", ref qdSummary, false );
			//
			output.DataProfileAttributes.IndustryRate = HandleQuantitiveValueList( input.IndustryRate, "DataProfile.IndustryRate", "Industry Rate", ref qdSummary, false );
			output.DataProfileAttributes.InsufficientEmploymentCriteria = HandleQuantitiveValueList( input.InsufficientEmploymentCriteria, "DataProfile.InsufficientEmploymentCriteria", "Insufficient Employment Criteria", ref qdSummary, false );
			output.DataProfileAttributes.MeetEmploymentCriteria = HandleQuantitiveValueList( input.MeetEmploymentCriteria, "DataProfile.MeetEmploymentCriteria", "Meet Employment Criteria", ref qdSummary, false );
			//
			output.DataProfileAttributes.NonCompleters = HandleQuantitiveValueList( input.NonCompleters, "DataProfile.NonCompleters", "Non Completers", ref qdSummary, false );
			output.DataProfileAttributes.NonHoldersInSet = HandleQuantitiveValueList( input.NonHoldersInSet, "DataProfile.NonHoldersInSet", "Non Holders In Set", ref qdSummary, false );
			output.DataProfileAttributes.OccupationRate = HandleQuantitiveValueList( input.OccupationRate, "DataProfile.OccupationRate", "Occupation Rate", ref qdSummary, false );
			//
			output.DataProfileAttributes.RegionalEarningsDistribution = HandleQuantitiveValueList( input.RegionalEarningsDistribution, "DataProfile.RegionalEarningsDistribution", "Regional Earnings Distribution", ref qdSummary, false );
			output.DataProfileAttributes.RegionalEmploymentRate = HandleQuantitiveValueList( input.RegionalEmploymentRate, "DataProfile.RegionalEmploymentRate", "Regional Employment Rate", ref qdSummary, false );
			output.DataProfileAttributes.RelatedEmployment = HandleQuantitiveValueList( input.RelatedEmployment, "DataProfile.RelatedEmployment", "Related Employment", ref qdSummary, false );
			output.DataProfileAttributes.SubjectsInSet = HandleQuantitiveValueList( input.SubjectsInSet, "DataProfile.SubjectsInSet", "Subjects In Set", ref qdSummary, false );
			output.DataProfileAttributes.SufficientEmploymentCriteria = HandleQuantitiveValueList( input.SufficientEmploymentCriteria, "DataProfile.SufficientEmploymentCriteria", "Sufficient Employment Criteria", ref qdSummary, false );
			output.DataProfileAttributes.UnrelatedEmployment = HandleQuantitiveValueList( input.UnrelatedEmployment, "DataProfile.UnrelatedEmployment", "Unrelated Employment", ref qdSummary, false );
			//
			output.DataProfileAttributes.TotalWIOACompleters = HandleQuantitiveValueList( input.TotalWIOACompleters, "DataProfile.TotalWIOACompleters", "Total WIOA Completers", ref qdSummary, false );
			output.DataProfileAttributes.TotalWIOAParticipants = HandleQuantitiveValueList( input.TotalWIOAParticipants, "DataProfile.TotalWIOAParticipants", "Total WIOA Participants", ref qdSummary, false );
			output.DataProfileAttributes.TotalWIOAExiters = HandleQuantitiveValueList( input.TotalWIOAExiters, "DataProfile.TotalWIOAExiters", "Total WIOA Exiters", ref qdSummary, false );
			//
			output.DataProfileAttributeSummary = qdSummary;
			return output;
		}

		//
		public List<MC.MonetaryAmount> FormatMonetaryAmount( List<MJ.MonetaryAmount> profiles, ref SaveStatus status )
		{
			var output = new List<MC.MonetaryAmount>();
			if ( profiles == null || !profiles.Any() )
				return output;

			foreach( var item in profiles )
			{
				//what are the minimum properties
				if ( item == null ||
					( item.Value == 0 && item.MinValue == 0 && item.MaxValue == 0 ))
				{
					continue;
				}
				var ma = new MC.MonetaryAmount()
				{
					Description = HandleLanguageMap( item.Description, "Description" ),
					Value = item.Value,
					MaxValue = item.MaxValue,
					MinValue = item.MinValue,
					Currency = item.Currency,
					UnitText = item.UnitText,
				};
				output.Add( ma );
			}

			return output;
		}
		//
		public List<MC.MonetaryAmountDistribution> FormatMonetaryAmountDistribution( List<MJ.MonetaryAmountDistribution> profiles, ref SaveStatus status )
		{
			var output = new List<MC.MonetaryAmountDistribution>();
			if ( profiles == null || !profiles.Any() )
				return output;

			foreach ( var item in profiles )
			{
				//what are the minimum properties
				if (item == null || 
					(item.Median == 0 && item.Percentile10 == 0  && item.Percentile25 == 0 && item.Percentile75 == 0 && item.Percentile90 == 0 ))
				{
					continue;
				}
				var ma = new MC.MonetaryAmountDistribution()
				{
					Currency = item.Currency,
					Median = item.Median,
					Percentile10 = item.Percentile10,
					Percentile25 = item.Percentile25,
					Percentile75 = item.Percentile75,
					Percentile90 = item.Percentile90
				};
				
				output.Add( ma );
			}

			return output;
		}
		//
		public List<MCQ.SubjectProfile> FormatSubjectProfile( List<MJQ.SubjectProfile> profiles, ref SaveStatus status )
		{
			var output = new List<MCQ.SubjectProfile>();
			if ( profiles == null || !profiles.Any() )
				return output;

			foreach ( var input in profiles )
			{
				if ( input == null )
					continue;
				var sp = new MCQ.SubjectProfile()
				{
					Name = HandleLanguageMap( input.Name, "Name" ),
					Description = HandleLanguageMap( input.Description, "Description" ),
					SubjectValue= HandleQuantitiveValueList( input.SubjectValue, "SubjectProfile.SubjectValue", false )
				};
				sp.SubjectType = MapCAOListToEnumermation( input.SubjectType );

				output.Add( sp );
			}

			return output;
		}
		//
		public List<MC.EmploymentOutcomeProfile> FormatEmploymentOutcomeProfile( List<MJ.EmploymentOutcomeProfile> profiles, OutcomesDTO outcomesDTO, List<BNode> bnodes, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var output = new List<MC.EmploymentOutcomeProfile>();
			foreach ( var input in profiles )
			{
				if ( input == null )
					continue;
				Guid rowId = Guid.NewGuid();
				var profile = new MC.EmploymentOutcomeProfile
				{
					RowId = rowId,
					Name = HandleLanguageMap( input.Name, "Name" ),
					Description = HandleLanguageMap( input.Description, "Description" ),
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					JobsObtained = input.JobsObtained
				};
				profile.Source = input.Source;
				//handle: RelevantDataSet
				if ( input.RelevantDataSet != null && input.RelevantDataSet.Any() )
				{
					//this will be a list of URIs
					foreach ( var item in input.RelevantDataSet )
					{
						//could use URI, but will use ctid
						var ctid = ResolutionServices.ExtractCtid( item );
						if ( string.IsNullOrWhiteSpace( ctid ) )
						{
							status.AddError( string.Format( "Error: Unable to derive a ctid from the EmploymentOutcomeProfile.RelevantDataSet for EmploymentOutcomeProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.Ctid, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.Ctid == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.Ctid ) )
						{
							status.AddError( string.Format( "Error: Unable to find the DataSetProfile for EmploymentOutcomeProfile (HP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.Ctid, ctid ) );
							continue;
						}
						var dspo = FormatDataSetProfile( input.Ctid, dspi, outcomesDTO, ref status );
						//may want to store the ctid, although will likely use relationships, or store the json
						if ( dspo != null )
						{
							profile.RelevantDataSetList.Add( dspo.CTID );
							profile.RelevantDataSet.Add( dspo );
						}
					}
				}

				output.Add( profile );
			}

			return output;
		}

		#endregion
		public string MapDate( string date, string dateName, ref SaveStatus status, bool doingReasonableCheck = true )
		{
			if ( string.IsNullOrWhiteSpace( date ) )
				return null;

			DateTime newDate = new DateTime();

			if ( DateTime.TryParse( date, out newDate ) )
			{
				if ( doingReasonableCheck && newDate < new DateTime( 1800, 1, 1 ) )
					status.AddWarning( string.Format( "Error - {0} is out of range (prior to 1800-01-01 ", dateName ) );
			}
			else
			{
				status.AddWarning( string.Format( "Error - {0} is invalid ", dateName ) );
				return null;
			}
			return newDate.ToString( "yyyy-MM-dd" );

		} //end

		#region  organizations
		/// <summary>
		/// Handle mapping org ref to a single reference
		/// NOTE: need to handle possibility of org references!!
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public Guid MapOrganizationReferencesGuid( string property, List<string> input, ref SaveStatus status )
		{
			//not sure if isResolved is necessary
			bool isResolved = false;
			Guid orgRef = new Guid();
			string registryAtId = "";
			if ( input == null || input.Count < 1 )
				return orgRef;

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;

				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					registryAtId = target;
					return ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					var node = GetBlankNode( target );
					//if type present,can use
					return ResolveOrgBlankNodeToGuid( property, node, ref status, ref isResolved );
				}
			}

			return orgRef;
		}
		public Guid MapOrganizationReferenceGuid( string property, string input, ref SaveStatus status )
		{
			//not sure if isResolved is necessary
			bool isResolved = false;
			Guid orgRef = new Guid();
			string registryAtId = "";
			if ( string.IsNullOrWhiteSpace( input ) )
				return orgRef;

			//determine if just Id, or base
			if ( input.StartsWith( "http" ) )
			{
				registryAtId = input;
				return ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
				//break;
			}
			else if ( input.StartsWith( "_:" ) )
			{
				var node = GetBlankNode( input );
				//if type present,can use
				return ResolveOrgBlankNodeToGuid( property, node, ref status, ref isResolved );
			}


			return orgRef;
		}
		/// <summary>
		/// Map Organization references from a list of strings to a list of Guids.
		/// The input will likely be a registry Url, or a blank node identifier. 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<Guid> MapOrganizationReferenceGuids( List<string> input, ref SaveStatus status )
		{

			return MapOrganizationReferenceGuids( "unknown", input, ref status );
		}

		/// <summary>
		/// Map Organization references from a list of strings to a list of Guids.
		/// The input will likely be a registry Url, or a blank node identifier. 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<Guid> MapOrganizationReferenceGuids( string property, List<string> input, ref SaveStatus status )
		{
			//not sure if isResolved is necessary
			bool isResolved = false;
			Guid orgRef = new Guid();
			List<Guid> orgRefs = new List<Guid>();
			string registryAtId = "";
			//var or = new List<RA.Models.Input.OrganizationReference>();
			if ( input == null || input.Count < 1 )
				return orgRefs;

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;

				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					registryAtId = target;
					if ( registryAtId != registryAtId.ToLower() )
					{
						status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, registryAtId ) );
					}
					orgRef = ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					var node = GetBlankNode( target );
					orgRef = ResolveOrgBlankNodeToGuid( property, node, ref status, ref isResolved );
				} else
				{
					//unexpected
					status.AddError( string.Format( "MapOrganizationReferenceGuids: Unhandled target  format found: {0} for property: {1}.", target, property ) );
				}

				if ( BaseFactory.IsGuidValid( orgRef ) )
					orgRefs.Add( orgRef );
			}

			return orgRefs;
		}
		private Guid ResolveOrgRegistryAtIdToGuid( string registryAtId, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityRef = new Guid();
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				entityRef = ResolutionServices.ResolveOrgByRegistryAtId( registryAtId, ref status, ref isResolved );
			}

			return entityRef;
		}

		/// <summary>
		/// Analyze a organization in a blank node: check if exists, by subject webpage. 
		/// If found return Guid, otherwise create new base
		/// </summary>
		/// <param name="input"></param>
		/// <param name="status"></param>
		/// <param name="isResolved"></param>
		/// <returns></returns>
		private Guid ResolveOrgBlankNodeToGuid( string property, BNode input, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityUid = new Guid();
			if ( input == null )
				return entityUid;
			//20-08-23 update to reuse 'duplicate' method
			int orgId = ResolveBlankNodeAsOrganization( property, input, ref entityUid, ref status );

			return entityUid;
			
		}
		#endregion


		#region  Entities
		public static int GetEntityTypeId( string entityType )
		{
			int entityTypeId = 0;

			switch ( entityType.ToLower() )
			{
				case "credential":
					entityTypeId = 1;
					break;
				case "ceterms:credentialorganization":
				case "ceterms:qacredentialorganization":
				case "ceterms:organization":
				case "organization":
					entityTypeId = 2;
					break;
				case "ceterms:assessmentprofile":
				case "assessmentprofile":
				case "assessment":
					entityTypeId = 3;
					break;
				case "ceterms:conditionprofile":
				case "conditionprofile":
					entityTypeId = 4;
					break;
				case "ceterms:costprofile":
				case "costprofile":
					entityTypeId = 5;
					break;
				case "ceterms:costprofileitem":
				case "costprofileitem":
					entityTypeId = 6;
					break;
				case "ceterms:learningopportunityprofile":
				case "learningopportunityprofile":
				case "learningopportunity":
					entityTypeId = 7;
					break;
				case "ceterms:pathway":
				case "pathway":
					entityTypeId = 8;
					break;
				case "asn:rubric":
				case "rubric":
					entityTypeId = 9;
					break;
				case "ceasn:competencyframework":
				case "competencyframework":
					//ISSUE - still have references to 17 in places for CaSS competencies
					entityTypeId = 10;
					break;
				case "skos:conceptscheme":
				case "conceptscheme":
					entityTypeId = 11;
					break;
				case "ceterms:conditionmanifest":
				case "conditionmanifest":
					entityTypeId = 19;
					break;
				case "ceterms:costmanifest":
				case "costmanifest":
					entityTypeId = 20;
					break;
				case "ceterms:pathwayset":
				case "pathwayset":
					entityTypeId = 23;
					break;
				case "ceterms:transfervalueprofile":
				case "transfervalueprofile":
				case "transfervalue":
					entityTypeId = 26;
					break;
				default:
					//default to credential???
					//20-04-09 mp - no longer can do this with addition of navy data
					entityTypeId = 0;
					break;
			}
			return entityTypeId;
		}

		/// <summary>
		/// Map a List of EntityBase references to a List of Guids
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId">If zero, look up by ctid, or </param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<Guid> MapEntityReferenceGuids( string property, List<string> input, int entityTypeId, ref SaveStatus status )
		{
			Guid entityRef = new Guid();
			List<Guid> entityRefs = new List<Guid>();
			MJ.EntityBase eb = new MJ.EntityBase();
			string registryAtId = "";
			if ( input == null || input.Count < 1 )
				return entityRefs;
			int origEntityTypeId = entityTypeId;
			if ( entityTypeId == 0 )
			{
				//don't always know the type, especially for org accrediting/owning something.
			}

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;

				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					registryAtId = target;
					if ( registryAtId != registryAtId.ToLower() )
					{
						status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, registryAtId ) );
					}
					//TODO - need to ensure existing reference entities can be updated!!!!!!!!
					entityRef = ResolveEntityRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					//should be a blank node
					var node = GetBlankNode( target );
					string name = HandleBNodeLanguageMap( node.Name, "blank node name", true );
					string desc = HandleBNodeLanguageMap( node.Description, "blank node desc", true );
					if ( node == null || string.IsNullOrWhiteSpace( name ) )
					{
						status.AddError( string.Format( "A Blank node was not found for bid of: {0}. ", target ) );
						continue;
						//return entityRefs;
					}
					bool isQAOrgType = false;
					//OR determine a context (ie. property accredited by)
					//NOTE: currently only called for org where is accredits etc.
					if ( origEntityTypeId == 0 || origEntityTypeId == 2 )
						entityTypeId = GetBlankNodeEntityType( node, ref isQAOrgType );
					//if type present,can use
					//DUPLICATE ALARM: may want to take approach of deleting existing
					entityRef = ResolveEntityBaseToGuid( property, node, entityTypeId, isQAOrgType, ref status );
				} else
				{
					//???
					status.AddError( string.Format( "Unexpected value of '{0}' for EntityReference: '{1}'. The expected value is either a registry URI or a blank node identifier.", target, property ) );
					return entityRefs;
				}
				if ( BaseFactory.IsGuidValid( entityRef ) )
					entityRefs.Add( entityRef );
				else
				{

				}
			}

			return entityRefs;
		}
		private Guid ResolveEntityRegistryAtIdToGuid( string property, string registryAtId, int entityTypeId, ref SaveStatus status )
		{
			bool isResolved = false;
			Guid entityUID = new Guid();
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				entityUID = ResolutionServices.ResolveEntityByRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status, ref isResolved );
			}

			return entityUID;
		}

		private Guid ResolveEntityBaseToGuid( string property, BNode input, int entityTypeId, bool isQAOrgType, ref SaveStatus status )
		{
			Guid entityUID = new Guid();
			int entityRefId = 0;
			status.HasSectionErrors = false;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

			if ( string.IsNullOrWhiteSpace( name ) )
				status.AddError( "Invalid EntityBase/BNode, missing name" );

			if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
				status.AddWarning( string.Format("Invalid EntityBase. Type: {0}, name: {1}.  missing SubjectWebpage", entityTypeId, name ));

			if ( status.HasSectionErrors )
				return entityUID;
			//look up by subject webpage
			//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
			//NOTE: need to avoid duplicate swp's; so  should combine
			//if ()

			MC.Entity entity = EntityManager.Entity_Cache_Get( entityTypeId, name, input.SubjectWebpage );
			if ( entity != null && entity.Id > 0 )
			{
				//20-12-16 mp - we don't want to return the UID, need to be able to update the reference
				//					OR ALWAYS DELETE
				//check what happens if called here
				if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
					entityRefId = ResolveBaseEntityAsCredential( input, ref entityUID, ref status );
				else if ( entityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
					entityRefId = ResolveBlankNodeAsOrganization( property, input, ref entityUID, ref status );
				else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
					entityRefId = ResolveBaseEntityAsAssessment( input, ref entityUID, ref status );
				else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
					entityRefId = ResolveBaseEntityAsLopp( input, ref entityUID, ref status );
				else
				{
					//unexpected, should not have entity references for manifests
					status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
				}
				//
				return entity.EntityUid;
			}

			//if not found, then create
			if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
				entityRefId = ResolveBaseEntityAsCredential( input, ref entityUID, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
				entityRefId = ResolveBlankNodeAsOrganization( property, input, ref entityUID, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				entityRefId = ResolveBaseEntityAsAssessment( input, ref entityUID, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
				entityRefId = ResolveBaseEntityAsLopp( input, ref entityUID, ref status );

			//TODO
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
			{
				//entityRefId = ResolveBaseEntityAsOccupation( input, ref entityUID, ref status );
				status.AddError( string.Format( "Error - Occupation blank nodes are not currently handled. entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )

			{
				//entityRefId = ResolveBaseEntityAsOccupation( input, ref entityUID, ref status );
				status.AddError( string.Format( "Error - JOB blank nodes are not currently handled. entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE )

			{
				//entityRefId = ResolveBaseEntityAsOccupation( input, ref entityUID, ref status );
				status.AddError( string.Format( "Error - TASK blank nodes are not currently handled. entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}
			else
			{
				//unexpected, should not have entity references for manifests
				status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}
			return entityUID;
		}

		/// <summary>
		/// Map list of EntityBase items to a list of integer Ids.
		/// These Ids will be used for child records under an Entity
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<int> MapEntityReferences( string property, List<string> input, int entityTypeId, ref SaveStatus status )
		{
			int entityRef = 0;
			List<int> entityRefs = new List<int>();
			string registryAtId = "";
			if ( input == null || input.Count < 1 )
				return entityRefs;

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;
				entityRef = 0;
				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityTypeId: {0}, CtdlId: {1} ", entityTypeId, target ) );
					registryAtId = target;
					entityRef = ResolveEntityRegistryAtId( registryAtId, entityTypeId, ref status );
					if ( entityRef == 0 )
					{
						LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: FAILED TO RESOLVE EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target ) );
					}
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target bnode: {1} ", entityTypeId, target ) );
					var node = GetBlankNode( target );
					//if type present,can use
					entityRef = ResolveEntityBaseToInt( property, node, entityTypeId, ref status );
				}
				if ( entityRef > 0 )
					entityRefs.Add( entityRef );
			}

			return entityRefs;
		}


		/// <summary>
		/// Entities will be string, where cannot be a third party reference
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<int> MapEntityReferences( List<string> input, int entityReferenceTypeId, int parentEntityTypeId, ref SaveStatus status )
		{
			int entityRef = 0;
			List<int> entityRefs = new List<int>();
			string registryAtId = "";
			if ( input == null || input.Count < 1 )
				return entityRefs;

			int cntr = 0;
			foreach ( var target in input )
			{
				cntr++;
				entityRef = 0;
				//only allow a valid id
				if ( !string.IsNullOrWhiteSpace( target ) )
				{
					if ( target.ToLower().StartsWith( "http" ) )
					{
						registryAtId = target;
						entityRef = ResolveEntityRegistryAtId( registryAtId, entityReferenceTypeId, ref status );
					}
					else
					{
						//not valid, log and continue?
						status.AddError( string.Format( "Invalid Entity reference encountered. parentEntityTypeId: {0}; entityReferenceTypeId: {1}; #: {2}; target: {3} ", parentEntityTypeId, entityReferenceTypeId, cntr, target ) );
					}
				}

				if ( entityRef > 0 )
					entityRefs.Add( entityRef );
			}

			return entityRefs;
		}

		private int ResolveEntityRegistryAtId( string registryAtId, int entityTypeId, ref SaveStatus status )
		{
			bool isResolved = false;
			int entityRefId = 0;
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				entityRefId = ResolutionServices.ResolveEntityByRegistryAtId( registryAtId, entityTypeId, ref status, ref isResolved );
			}

			return entityRefId;
		}
		private int ResolveEntityBaseToInt( string property, BNode input, int entityTypeId, ref SaveStatus status )
		{

			int entityRefId = 0;
			Guid entityRef = new Guid();
			status.HasSectionErrors = false;

			if ( input.Name.Count() == 0 )
				status.AddError( "Invalid EntityBase/BNode, missing name" );

			if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
				status.AddWarning( "Invalid EntityBase, missing SubjectWebpage" );

			if ( status.HasSectionErrors )
				return entityRefId;

			//look up by subject webpage
			//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
			//NOTE: need to avoid duplicate swp's; so  should combine
			string name = HandleBNodeLanguageMap( input.Name, "BNode Name for " + input.BNodeId, false );
			MC.Entity entity = EntityManager.Entity_Cache_Get( entityTypeId, name, input.SubjectWebpage );
			if ( entity != null && entity.Id > 0 )
				return entity.EntityBaseId;

			if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
				entityRefId = ResolveBaseEntityAsCredential( input, ref entityRef, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
				entityRefId = ResolveBlankNodeAsOrganization( property, input, ref entityRef, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				entityRefId = ResolveBaseEntityAsAssessment( input, ref entityRef, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
				entityRefId = ResolveBaseEntityAsLopp( input, ref entityRef, ref status );
			else
			{
				//unexpected, should not have entity references for manifests
				status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}


			return entityRefId;
		}
		private int ResolveBaseEntityAsCredential( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			MC.Credential output = CredentialManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			if ( output != null && output.Id > 0 )
			{
				//need additional check for missing credential type id
				if ( output.CredentialTypeId == 0 )
				{
					//need to update!
					output.CredentialTypeSchema = input.Type;
					if ( new CredentialManager().UpdateBaseReferenceCredentialType( output, ref status ) == 0 )
					{
						//any errors should already be in status
					}
				}
				output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
				output.CodedNotation = input.CodedNotation;
				//
				output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
				//
				output.Identifier = MapIdentifierValueList( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				output.OfferedBy = MapOrganizationReferenceGuids( "CredentialReference.OfferedBy", input.OfferedBy, ref status );
				output.OwnedBy = MapOrganizationReferenceGuids( "CredentialReference.OwnedBy", input.OwnedBy, ref status );
				//
				output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				entityUid = output.RowId;
				//do update 
				new CredentialManager().Save( output, ref status );

				return output.Id;
			}
			output = new workIT.Models.Common.Credential()
			{
				Name = name,
				SubjectWebpage = input.SubjectWebpage,
				Description = desc,
				CredentialTypeSchema = input.Type
			};
			//
			output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
			//
			output.CodedNotation = input.CodedNotation;
			//
			output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
			//
			output.Identifier = MapIdentifierValueList( input.Identifier );
			if ( output.Identifier != null && output.Identifier.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			}
			output.OfferedBy = MapOrganizationReferenceGuids( "CredentialReference.OfferedBy", input.OfferedBy, ref status );
			output.OwnedBy = MapOrganizationReferenceGuids( "CredentialReference.OwnedBy", input.OwnedBy, ref status );
			//
			output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			if ( new CredentialManager().AddBaseReference( output, ref status ) > 0 )
			{
				entityUid = output.RowId;
				entityRefId = output.Id;
			}

			return entityRefId;
		}
		private int ResolveBlankNodeAsOrganization( string property, BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			status.HasSectionErrors = false;
			if ( input == null )
				return entityRefId;
			try
			{
				string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
				string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

				//look up by name, subject webpage
				//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
				//NOTE: need to avoid duplicate swp's; so  should combine
				//Be sure this method will return all properties that could be present in a BN
				//20-08-23  - add email, address, and availabilityListing
				//TODO - delete existing references for the org
				var output = OrganizationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
				if ( output != null && output.Id > 0 )
				{
					//20-07-04 - hmmm - are updates not being done??
					//			- the problem with QA references like accredited by, could be variations in description, as well as name and swp!
					//			- may have to check extended properties and first don't override, and perhaps warn/email on any changes	
					entityUid = output.RowId;
					if ( string.IsNullOrWhiteSpace( output.AgentDomainType ) )
						output.AgentDomainType = input.Type;
					//20-08-23 - new additions to org BN
					output.Addresses = FormatAvailableAtAddresses( input.Address, ref status );
					//may not be applicable
					if ( output.Addresses != null && output.Addresses.Any() )
						output.AddressesJson = JsonConvert.SerializeObject( output.Addresses, MappingHelperV3.GetJsonSettings() );
					//
					output.AvailabilityListing = MapListToString( input.AvailabilityListing );
					//future prep
					output.AvailabilityListings = input.AvailabilityListing;
					//
					output.Emails = MapToTextValueProfile( input.Email );
					//do update 
					new OrganizationManager().Save( output, ref status );

					return output.Id;
				}

				//need type of org!!
				output = new MC.Organization()
				{
					AgentDomainType = input.Type,
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					SocialMediaPages = MapToTextValueProfile( input.SocialMedia ),
					Description = desc,
					RowId = Guid.NewGuid()
				};
				//20-08-23 - new additions to org BN
				output.Addresses = FormatAvailableAtAddresses( input.Address, ref status );
				//may not be applicable
				if ( output.Addresses != null && output.Addresses.Any() )
					output.AddressesJson = JsonConvert.SerializeObject( output.Addresses, MappingHelperV3.GetJsonSettings() );
				//
				output.AvailabilityListing = MapListToString( input.AvailabilityListing );
				//future prep
				output.AvailabilityListings = input.AvailabilityListing;
				//
				output.Emails = MapToTextValueProfile( input.Email );


				//20-08-23 - update this to handle new additions
				if ( new OrganizationManager().AddBaseReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, "MappingHelperV3.ResolveBlankNodeAsOrganization()" );
				status.AddError( ex.Message );
				return 0;
			}

			return entityRefId;
		}
		private int ResolveBaseEntityAsAssessment( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			WPM.AssessmentProfile output = AssessmentManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			if ( output != null && output.Id > 0 )
			{
				//20-12-08 mp - combine mapping

				//20-07-04 - hmmm - are updates not being done??
				//entityUid = output.RowId;

				////?? could have updates from multiple sources (not real likely), so don't want to overwrite data
				//output.AssessesCompetencies = MapCAOListToCAOProfileList( input.Assesses );
				//output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				//output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );

				//output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
				////
				//output.CodedNotation = input.CodedNotation;
				//output.DeliveryType = MapCAOListToEnumermation( input.DeliveryType );
				////output.CreditValue = HandleQuantitiveValue( input.CreditValue, "Assessment.CreditValue" );
				//output.CreditValueList = HandleValueProfileListToQVList( input.CreditValue, "Assessment.CreditValue", true );
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				////

				//output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
				//output.DateEffective = input.DateEffective;
				//output.ExpirationDate = input.ExpirationDate;
				////
				//output.Identifier = MapIdentifierValueList( input.Identifier );
				//if ( output.Identifier != null && output.Identifier.Count() > 0 )
				//{
				//	output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				//}
				////
				//output.LearningMethodDescription = HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );
				//output.OfferedBy = MapOrganizationReferenceGuids( "Assessment.OfferedBy", input.OfferedBy, ref status );
				//output.OwnedBy = MapOrganizationReferenceGuids( "Assessment.OwnedBy", input.OwnedBy, ref status );
				//if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				//{
				//	output.OwningAgentUid = output.OwnedBy[ 0 ];
				//}
				////
				//output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				////TBD - arbitrary update or 
				////might be able to just to an update
				////see what happens using regular update.
				//new AssessmentManager().Save( output, ref status );
				//return output.Id;
			}
			else
			{
				output = new workIT.Models.ProfileModels.AssessmentProfile()
				{
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					Description = desc
				};
			}

			//20-12-08 mp - combine mapping

			//20-07-04 need to handle additional properties

			/*
			public List<CredentialAlignmentObject> Assesses { get; set; }
			public LanguageMap AssessmentMethodDescription { get; set; }
			public List<Place> AvailableAt { get; set; }
			public string CodedNotation { get; set; }
			public List<QuantitativeValue> CreditValue { get; set; } = null;
			public List<DurationProfile> EstimatedDuration { get; set; }
			public LanguageMap LearningMethodDescription { get; set; }
			public List<string> OfferedBy { get; set; }
			public List<string> OwnedBy { get; set; }
			public List<CredentialAlignmentObject> Teaches { get; set; }			 
			 */
			output.AssessesCompetencies = MapCAOListToCAOProfileList( input.Assesses );
			output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );

			output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
			output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
			//
			output.CodedNotation = input.CodedNotation;
			output.CreditValueList = HandleValueProfileListToQVList( input.CreditValue, "Assessment.CreditValue", true );
			if ( output.CreditValueList != null && output.CreditValueList.Any() )
				output.CreditValue = output.CreditValueList[ 0 ];
			output.DeliveryType = MapCAOListToEnumermation( input.DeliveryType );

			//
			output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
			output.DateEffective = input.DateEffective;
			output.ExpirationDate = input.ExpirationDate;
			//
			output.Identifier = MapIdentifierValueList( input.Identifier );
			if ( output.Identifier != null && output.Identifier.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			}
			//
			output.LearningMethodDescription = HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );
			output.OfferedBy = MapOrganizationReferenceGuids( "Assessment.OfferedBy", input.OfferedBy, ref status );
			output.OwnedBy = MapOrganizationReferenceGuids( "Assessment.OwnedBy", input.OwnedBy, ref status );
			if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
			{
				output.OwningAgentUid = output.OwnedBy[ 0 ];
			}
			//
			output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			//
			output.Requires = FormatConditionProfile( input.Requires, ref status );
			output.Recommends = FormatConditionProfile( input.Recommends, ref status );
			output.EntryCondition = FormatConditionProfile( input.EntryCondition, ref status );
			output.Corequisite = FormatConditionProfile( input.Corequisite, ref status );
			//will need to ensure mapping includes new properties - and do parts!

			if ( output.Id > 0 )
			{
				new AssessmentManager().Save( output, ref status );
				entityUid = output.RowId;
				return output.Id;
			}
			else
			{
				if ( new AssessmentManager().AddBaseReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}
			return entityRefId;
		}
		private int ResolveBaseEntityAsLopp( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			//20-12-15 mp - now a subject webpage may not be required, so we would need something else OR ALWAYS DELETE
			WPM.LearningOpportunityProfile output = LearningOpportunityManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			if ( output != null && output.Id > 0 )
			{
				//20-12-08 mp - combine mapping

				//20-07-04 - hmmm - are updates not being done??
				//output.TeachesCompetencies = MapCAOListToCAOProfileList( input.Teaches );
				//output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				//output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
				////
				//output.CodedNotation = input.CodedNotation;
				//output.CreditValueList = HandleQuantitiveValueList( input.CreditValue, "LearningOpportunity.CreditValue", true );
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				////
				//output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
				//output.DateEffective = input.DateEffective;
				//output.ExpirationDate = input.ExpirationDate;
				////
				//output.Identifier = MapIdentifierValueList( input.Identifier );
				//if ( output.Identifier != null && output.Identifier.Count() > 0 )
				//{
				//	output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				//}
				////
				//output.LearningMethodDescription = HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );
				////check for changes by owner/offerer
				//output.OfferedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OfferedBy", input.OfferedBy, ref status );
				//output.OwnedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
				//if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				//{
				//	output.OwningAgentUid = output.OwnedBy[ 0 ];
				//}
				////
				//output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
				////see what happens using regular update.
				//new LearningOpportunityManager().Save( output, ref status );
				//entityUid = output.RowId;
				//return output.Id;
			}
			else
			{
				//==================================================
				output = new workIT.Models.ProfileModels.LearningOpportunityProfile()
				{
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					Description = desc
				};
			}
			//20-12-08 mp - combine mapping

			//20-07-04 need to handle additional properties
			output.TeachesCompetencies = MapCAOListToCAOProfileList( input.Teaches );
			output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );
			output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
			output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
			//
			output.CodedNotation = input.CodedNotation;
			output.CreditValueList = HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue", true );
			if ( output.CreditValueList != null && output.CreditValueList.Any() )
				output.CreditValue = output.CreditValueList[ 0 ];
			//
			output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
			output.DateEffective = input.DateEffective;
			
			output.DeliveryType = MapCAOListToEnumermation( input.DeliveryType );
			output.LearningMethodType = MapCAOListToEnumermation( input.LearningMethodType );
			output.LearningMethodDescription = HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );

			output.ExpirationDate = input.ExpirationDate;
			//
			output.Identifier = MapIdentifierValueList( input.Identifier );
			if ( output.Identifier != null && output.Identifier.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			}
			
			output.OfferedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OfferedBy", input.OfferedBy, ref status );
			output.OwnedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
			if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
			{
				output.OwningAgentUid = output.OwnedBy[ 0 ];
			}
			//
			output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			//
			output.Requires = FormatConditionProfile( input.Requires, ref status );
			output.Recommends = FormatConditionProfile( input.Recommends, ref status );
			output.EntryCondition = FormatConditionProfile( input.EntryCondition, ref status );
			output.Corequisite = FormatConditionProfile( input.Corequisite, ref status );
			//
			if ( output.Id > 0 )
			{
				new LearningOpportunityManager().Save( output, ref status );
				entityUid = output.RowId;
				return output.Id;
			}
			else
			{
				if ( new LearningOpportunityManager().AddBaseReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}
			return entityRefId;
		}

		private int ResolveBaseEntityAsOccupation( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			//20-12-15 mp - now a subject webpage may not be required, so we would need something else OR ALWAYS DELETE
			//MC.Occupation output = OccupationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			//if ( output != null && output.Id > 0 )
			//{
			//	//20-12-08 mp - combine mapping

			
			//}
			//else
			//{
			//	//==================================================
			//	output = new MC.Occupation()
			//	{
			//		Name = name,
			//		SubjectWebpage = input.SubjectWebpage,
			//		Description = desc
			//	};
			//}

			////20-07-04 need to handle additional properties

			////
			//output.Identifier = MapIdentifierValueList( input.Identifier );
			//if ( output.Identifier != null && output.Identifier.Count() > 0 )
			//{
			//	output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			//}

			//output.OfferedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OfferedBy", input.OfferedBy, ref status );
			//output.OwnedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
			//if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
			//{
			//	output.OwningAgentUid = output.OwnedBy[ 0 ];
			//}
			////
			//output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
			////

			////
			//if ( output.Id > 0 )
			//{
			//	new OccupationManager().Save( output, ref status );
			//	entityUid = output.RowId;
			//	return output.Id;
			//}
			//else
			//{
			//	if ( new OccupationManager().AddBaseReference( output, ref status ) > 0 )
			//	{
			//		entityUid = output.RowId;
			//		entityRefId = output.Id;
			//	}
			//}
			return entityRefId;
		}

		#endregion


		#region  Condition profiles
		public List<WPM.ConditionProfile> FormatConditionProfile( List<MJ.ConditionProfile> profiles, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var list = new List<WPM.ConditionProfile>();

			foreach ( var input in profiles )
			{

				var output = new WPM.ConditionProfile
				{
					RowId = Guid.NewGuid()
				};
				output.Name = HandleLanguageMap( input.Name, output, "Name", true );
				LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile Name " + ( output.Name ?? "no name" ) );
				output.Description = HandleLanguageMap( input.Description, output, "Description", true );
				output.SubjectWebpage = input.SubjectWebpage;
				output.AudienceLevelType = MapCAOListToEnumermation( input.AudienceLevelType );
				output.AudienceType = MapCAOListToEnumermation( input.AudienceType );
				output.DateEffective = MapDate( input.DateEffective, "DateEffective", ref status );

				output.Condition = MapToTextValueProfile( input.Condition, output, "Condition", true );

				//TEMP expect object and handle accordingly
				//20-05-09 mp - found condition manifest with old format of language maplist???
				//output.SubmissionOf = MapToTextValueProfile( input.SubmissionOf );
				if ( input.SubmissionOf != null )
				{
					if ( input.SubmissionOf is List<string> )
					{
						output.SubmissionOf = MapToTextValueProfile( ( List<string> )input.SubmissionOf );

					}
					//else if ( input.SubmissionOf is MJ.LanguageMapList )
					//{
					//    LoggingHelper.DoTrace( 1, string.Format( "***Found SubmissionOf using LanguageMapList. CurrentEntityTypeId: {0}, CurrentEntityCTID: {1}, CurrentEntityName: {2}", CurrentEntityTypeId, CurrentEntityCTID, CurrentEntityName ));
					//    output.SubmissionOf = MapToTextValueProfile( ( MJ.LanguageMapList )input.SubmissionOf, output, "SubmissionOf", true );
					//}
				}


				output.SubmissionOfDescription = HandleLanguageMap( input.SubmissionOfDescription, output, "SubmissionOfDescription", true );
				//while the input is a list, we are only handling single
				if ( input.AssertedBy != null && input.AssertedBy.Count > 0 )
				{
					output.AssertedByAgent = MapOrganizationReferenceGuids( "ConditionProfile.AssertedBy", input.AssertedBy, ref status );
				}

				output.Experience = input.Experience;
				output.MinimumAge = input.MinimumAge;
				output.YearsOfExperience = input.YearsOfExperience;
				output.Weight = input.Weight;

				//output.CreditValue = HandleQuantitiveValue( input.CreditValue, "ConditionProfile.CreditHourType" );
				output.CreditValueList = HandleValueProfileListToQVList( input.CreditValue, "ConditionProfile.CreditValue", true );
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValueList, MappingHelperV3.GetJsonSettings() );

				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				//
				//if ( !output.CreditValue.HasData() )
				//{
				//	if ( UtilityManager.GetAppKeyValue( "usingQuantitiveValue", false ) )
				//	{
				//		//will not handle ranges
				//		output.CreditValue = new workIT.Models.Common.QuantitativeValue
				//		{
				//			Value = input.CreditHourValue,
				//			CreditUnitType = MapCAOToEnumermation( input.CreditUnitType ),
				//			Description = HandleLanguageMap( input.CreditUnitTypeDescription, output, "CreditUnitTypeDescription" )
				//		};
				//		//what about hours?
				//		//if there is hour data, can't be unit data, so assign
				//		//if ( input.CreditHourValue > 0 )
				//		//{
				//		//	output.CreditValue.Value = input.CreditHourValue;
				//		//	output.CreditValue.Description = HandleLanguageMap( input.CreditHourType, output, "CreditHourType" );
				//		//}
				//	}
				//	else
				//	{
				//		//output.CreditHourType = HandleLanguageMap( input.CreditHourType, output, "CreditHourType" );
				//		//output.CreditHourValue = input.CreditHourValue;

				//		output.CreditUnitType = MapCAOToEnumermation( input.CreditUnitType );
				//		output.CreditUnitValue = input.CreditUnitValue;
				//		output.CreditUnitTypeDescription = HandleLanguageMap( input.CreditUnitTypeDescription, output, "CreditUnitTypeDescription" );
				//	}
				//}
				//
				if ( input.AlternativeCondition != null & input.AlternativeCondition.Count > 0 )
					output.AlternativeCondition = FormatConditionProfile( input.AlternativeCondition, ref status );

				output.EstimatedCosts = FormatCosts( input.EstimatedCost, ref status );

				//jurisdictions
				output.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
				output.ResidentOf = MapToJurisdiction( input.ResidentOf, ref status );

				//targets
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					output.TargetCredentialIds = MapEntityReferences( "ConditionProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
					output.TargetAssessmentIds = MapEntityReferences( "ConditionProfile.TargetAssessment", input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
				if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
				{
					LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile. Has learning opportunities: " + input.TargetLearningOpportunity.Count.ToString() );
					output.TargetLearningOpportunityIds = MapEntityReferences( "ConditionProfile.TargetLearningOpportunity", input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

					LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile. Has learning opportunities. Mapped to list: " + output.TargetLearningOpportunityIds.Count.ToString() );
				}

				output.TargetCompetencies = MapCAOListToCAOProfileList( input.TargetCompetency );
				list.Add( output );
			}

			return list;
		}

		#endregion

		#region  RevocationProfile
		public List<WPM.RevocationProfile> FormatRevocationProfile( List<MJ.RevocationProfile> profiles, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var list = new List<WPM.RevocationProfile>();

			foreach ( var input in profiles )
			{
				var profile = new WPM.RevocationProfile
				{
					RowId = Guid.NewGuid(),
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status ),
					RevocationCriteriaUrl = input.RevocationCriteria,
				};
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
				profile.RevocationCriteriaDescription = HandleLanguageMap( input.RevocationCriteriaDescription, profile, "RevocationCriteriaDescription", true );

				list.Add( profile );
			}

			return list;
		}
		#endregion

		#region  Costs

		public List<WPM.CostProfile> FormatCosts( List<MJ.CostProfile> costs, ref SaveStatus status )
		{
			return FormatCostProfileMerged( costs, ref status );

			//return OFormatCosts( costs, ref status );
		}


		public List<WPM.CostProfile> FormatCostProfileMerged( List<MJ.CostProfile> costs, ref SaveStatus status )
		{
			if ( costs == null || costs.Count == 0 )
				return null;

			var list = new List<WPM.CostProfileMerged>();

			foreach ( MJ.CostProfile item in costs )
			{
				var profile = new WPM.CostProfileMerged
				{
					RowId = Guid.NewGuid(),
					Jurisdiction = MapToJurisdiction( item.Jurisdiction, ref status ),

					StartDate = item.StartDate,
					EndDate = item.EndDate,

					CostDetails = item.CostDetails,
					Currency = item.Currency,
					CurrencySymbol = item.Currency,

					AudienceType = MapCAOListToEnumermation( item.AudienceType ),

					CostType = MapCAOToEnumermation( item.DirectCostType ),

					Price = item.Price,
					ResidencyType = MapCAOListToEnumermation( item.ResidencyType )
				};
				profile.Description = HandleLanguageMap( item.Description, profile, "Description", true );
				profile.Name = HandleLanguageMap( item.Name, profile, "Name", true );
				profile.PaymentPattern = HandleLanguageMap( item.PaymentPattern, profile, "PaymentPattern", true );
				profile.Condition = MapToTextValueProfile( item.Condition, profile, "Condition", true );
				//cp.CostType = MapCAOToEnumermation(item.DirectCostType);

				list.Add( profile );
			}
			return WPM.CostProfileMerged.ExpandCosts( list );
		}

		#endregion

		#region  FinancialAlignmentObject
		//public List<MC.FinancialAlignmentObject> FormatFinancialAssistance( List<MJ.FinancialAlignmentObject> input, ref SaveStatus status )
		//{
		//    if ( input == null || input.Count == 0 )
		//        return null;

		//    var list = new List<MC.FinancialAlignmentObject>();
		//    foreach ( var item in input )
		//    {
		//        var profile = new MC.FinancialAlignmentObject
		//        {
		//            RowId = Guid.NewGuid(),
		//            AlignmentType = item.AlignmentType,
		//            Framework = item.Framework ?? "",
		//            TargetNode = item.TargetNode ?? "",
		//            Weight = item.Weight
		//        };

		//        profile.FrameworkName = HandleLanguageMap( item.FrameworkName, profile, "FrameworkName", true );
		//        profile.TargetNodeDescription = HandleLanguageMap( item.TargetNodeDescription, profile, "TargetNodeDescription", true );
		//        profile.TargetNodeName = HandleLanguageMap( item.TargetNodeName, profile, "TargetNodeName", true );

		//        profile.AlignmentDate = MapDate( item.AlignmentDate, "AlignmentDate", ref status );
		//        profile.CodedNotation = item.CodedNotation;
		//        list.Add( profile );
		//    }

		//    return list;
		//}
		public List<MC.FinancialAssistanceProfile> FormatFinancialAssistance( List<MJ.FinancialAssistanceProfile> input, ref SaveStatus status )
		{
			if ( input == null || input.Count == 0 )
				return null;

			var list = new List<MC.FinancialAssistanceProfile>();
			foreach ( var item in input )
			{
				var profile = new MC.FinancialAssistanceProfile();

				profile.Name = HandleLanguageMap( item.Name, profile, "Name", true );
				profile.Description = HandleLanguageMap( item.Description, profile, "TargetNodeDescription", true );
				profile.SubjectWebpage = item.SubjectWebpage;
				profile.FinancialAssistanceType = MapCAOListToEnumermation( item.FinancialAssistanceType );

				profile.FinancialAssistanceValue = HandleQuantitiveValueList( item.FinancialAssistanceValue, "FinancialAssistanceValue", false );
				if ( profile.FinancialAssistanceValue != null && profile.FinancialAssistanceValue.Any() )
					profile.FinancialAssistanceValueJson = JsonConvert.SerializeObject( profile.FinancialAssistanceValue, MappingHelperV3.GetJsonSettings() );
				list.Add( profile );
			}

			return list;
		}
		#endregion

		#region  DurationProfile
		public List<WPM.DurationProfile> FormatDuration( List<MJ.DurationProfile> input, ref SaveStatus status )
		{
			if ( input == null || input.Count == 0 )
				return null;

			List<WPM.DurationProfile> list = new List<WPM.DurationProfile>();
			var profile = new WPM.DurationProfile();

			foreach ( MJ.DurationProfile item in input )
			{
				if ( item.Description == null &&
					item.ExactDuration == null &&
					item.MaximumDuration == null &&
					item.MinimumDuration == null
					)
					continue;
				else
				{
					profile = new WPM.DurationProfile
					{
						RowId = Guid.NewGuid(),
						ExactDuration = FormatDurationItem( item.ExactDuration ),
						MaximumDuration = FormatDurationItem( item.MaximumDuration ),
						MinimumDuration = FormatDurationItem( item.MinimumDuration )
					};
					profile.Description = HandleLanguageMap( item.Description, profile, "Description", true );
					// only allow an exact duration or a range, not both 
					if ( profile.IsRange && profile.ExactDuration != null )
					{
						status.AddWarning( "Duration Profile error - provide either an exact duration or a minimum and maximum range, but not both. Defaulting to range" );
						profile.ExactDuration = null;
					}
					list.Add( profile );
				}

			}
			if ( list.Count > 0 )
				return list;
			else
				return null;
		}
		public WPM.DurationItem FormatDurationItem( string duration )
		{
			if ( string.IsNullOrEmpty( duration ) )
				return null;

			var match = new System.Text.RegularExpressions.Regex( @"^P(?!$)(\d+Y)?(\d+M)?(\d+W)?(\d+D)?(T(?=\d)(\d+H)?(\d+M)?(\d+S)?)?$" ).Match( duration );

			if ( !match.Success )
				throw new FormatException( "ISO8601 duration format" );

			int years = 0, months = 0, weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;

			if ( match.Groups[ 1 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 1 ].Value, @"\d+" ).Value, out years );
			if ( match.Groups[ 2 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 2 ].Value, @"\d+" ).Value, out months );
			if ( match.Groups[ 3 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 3 ].Value, @"\d+" ).Value, out weeks );
			if ( match.Groups[ 4 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 4 ].Value, @"\d+" ).Value, out days );
			if ( match.Groups[ 6 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 6 ].Value, @"\d+" ).Value, out hours );
			if ( match.Groups[ 7 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 7 ].Value, @"\d+" ).Value, out minutes );
			if ( match.Groups[ 8 ].Success )
				int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 8 ].Value, @"\d+" ).Value, out seconds );

			var output = new WPM.DurationItem
			{
				DurationISO8601 = duration,
				Years = years,
				Months = months,
				Weeks = weeks,
				Days = days,
				Hours = hours,
				Minutes = minutes
			};

			return output;
		}

		#endregion

		#region Verification Profile 
		public List<WPM.VerificationServiceProfile> MapVerificationServiceProfiles( List<MJ.VerificationServiceProfile> verificationServiceProfiles, ref SaveStatus status )
		{
			if ( verificationServiceProfiles == null || verificationServiceProfiles.Count == 0 )
				return null;

			var list = new List<WPM.VerificationServiceProfile>();

			foreach ( var input in verificationServiceProfiles )
			{
				var profile = new WPM.VerificationServiceProfile
				{
					RowId = Guid.NewGuid(),
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					SubjectWebpage = input.SubjectWebpage
				};
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
				profile.VerificationMethodDescription = HandleLanguageMap( input.VerificationMethodDescription, profile, "VerificationMethodDescription", true );

				//VerificationService is hidden in the publisher!
				profile.VerificationServiceUrl = MapListToString( input.VerificationService );
				profile.OfferedByAgentUid = MapOrganizationReferencesGuid( "VerificationServiceProfile.OfferedByAgentUid", input.OfferedBy, ref status );
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					profile.TargetCredentialIds = MapEntityReferences( "VerificationServiceProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				profile.VerificationDirectory = MapListToString( input.VerificationDirectory );
				profile.ClaimType = MapCAOListToEnumermation( input.VerifiedClaimType );
				profile.EstimatedCost = FormatCosts( input.EstimatedCost, ref status );
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );

				list.Add( profile );
			}

			return list;
		}
		#endregion

		public decimal StringtoDecimal( string value )
		{
			decimal output = 0m;
			if ( string.IsNullOrWhiteSpace( value ) )
				return output;

			decimal.TryParse( value, out output );
			return output;
		}
		public static JsonSerializerSettings GetJsonSettings()
		{
			var settings = new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ContractResolver = new EmptyNullResolver(),
				Formatting = Formatting.Indented,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			return settings;
		}
		//Force properties to be serialized in alphanumeric order
		public class AlphaNumericContractResolver : DefaultContractResolver
		{
			protected override System.Collections.Generic.IList<JsonProperty> CreateProperties( System.Type type, MemberSerialization memberSerialization )
			{
				return base.CreateProperties( type, memberSerialization ).OrderBy( m => m.PropertyName ).ToList();
			}
		}

		public class EmptyNullResolver : AlphaNumericContractResolver
		{
			protected override JsonProperty CreateProperty( MemberInfo member, MemberSerialization memberSerialization )
			{
				var property = base.CreateProperty( member, memberSerialization );
				var isDefaultValueIgnored = ( ( property.DefaultValueHandling ?? DefaultValueHandling.Ignore ) & DefaultValueHandling.Ignore ) != 0;

				if ( isDefaultValueIgnored )
					if ( !typeof( string ).IsAssignableFrom( property.PropertyType ) && typeof( IEnumerable ).IsAssignableFrom( property.PropertyType ) )
					{
						Predicate<object> newShouldSerialize = obj =>
						{
							var collection = property.ValueProvider.GetValue( obj ) as ICollection;
							return collection == null || collection.Count != 0;
						};
						Predicate<object> oldShouldSerialize = property.ShouldSerialize;
						property.ShouldSerialize = oldShouldSerialize != null ? o => oldShouldSerialize( oldShouldSerialize ) && newShouldSerialize( oldShouldSerialize ) : newShouldSerialize;
					}
					else if ( typeof( string ).IsAssignableFrom( property.PropertyType ) )
					{
						Predicate<object> newShouldSerialize = obj =>
						{
							var value = property.ValueProvider.GetValue( obj ) as string;
							return !string.IsNullOrEmpty( value );
						};

						Predicate<object> oldShouldSerialize = property.ShouldSerialize;
						property.ShouldSerialize = oldShouldSerialize != null ? o => oldShouldSerialize( oldShouldSerialize ) && newShouldSerialize( oldShouldSerialize ) : newShouldSerialize;
					}
				return property;
			}
		}
	}

	public class OutcomesDTO
	{
		//holder, earnings, employment
		public string OutcomesEntityType { get; set; }
		public string OutcomesEntityCTID { get; set; }

		//add additional properties for tracing, parent credential ctid, holder/earning ctid
		public List<MJ.QData.DataSetProfile> DataSetProfiles { get; set; } = new List<MJ.QData.DataSetProfile>();
		public List<MJ.QData.DataSetTimeFrame> DataSetTimeFrames { get; set; } = new List<MJ.QData.DataSetTimeFrame>();

		public List<MJ.QData.DataProfile> DataProfiles { get; set; } = new List<MJ.QData.DataProfile>();

	}
	//public class EntityReferenceHelper
	//{
	//    public List<IdProperty> IdList { get; set; }
	//}
	//public class IdProperty
	//{
	//    [JsonProperty( "@id" )]
	//    public string CtdlId { get; set; }
	//}
}
