using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using workIT.Factories;
using workIT.Models;
using workIT.Services;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using InputAddress = RA.Models.JsonV2.Place;
using MC = workIT.Models.Common;
using MCQ = workIT.Models.QData;
using MJ = RA.Models.JsonV2;
using MJQ = RA.Models.JsonV2.QData;
using WPM = workIT.Models.ProfileModels;

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
			isQAOrgType = false;
			//replace with this to avoid duplicative+ maintenance
			int entityTypeId=GetEntityTypeId( node.Type );
			//one additional check
			//one additional check
			if ( node.Type.ToLower() == "ceterms:qacredentialorganization" )
				isQAOrgType = true;

			//switch ( node.Type.ToLower() )
			//{
			//	case "ceterms:credentialorganization":
			//		entityTypeId = 2;
			//		break;
			//	case "ceterms:qacredentialorganization":
			//		//what distinctions do we need for QA orgs?
			//		entityTypeId = 2;
			//		isQAOrgType = true;

			//		break;
			//	case "ceterms:organization":
			//		entityTypeId = 2;
			//		break;
			//	case "ceterms:assessmentprofile":
			//		entityTypeId = 3;
			//		break;
			//	case "ceterms:learningopportunityprofile":
			//		entityTypeId = 7;
			//		break;
			//	case "ceterms:conditionmanifest":
			//		entityTypeId = 19;
			//		break;
			//	case "ceterms:costmanifest":
			//		entityTypeId = 20;
			//		break;
			//	case "ceasn:competencyframework":
			//		entityTypeId = 10;
			//		break;
			//	case "skos:conceptscheme":
			//		entityTypeId = 11;
			//		break;
			//	case "ceterms:transfervalueprofile":
			//		entityTypeId = 26;
			//		break;
			//	default:
			//		//default to credential
			//		entityTypeId = 1;
			//		break;
			//}
			return entityTypeId;
		}

		#endregion


		#region  mapping Language maps
		/// <summary>
		/// Set the default language based on the first language in a language map
		/// </summary>
		/// <param name="map"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public string SetDefaultLanguage( MJ.LanguageMap map, string property )
		{
			string output = "";
			var languageCode = DefaultLanguage;
			if ( map == null || map.Count == 0 )
			{
				return DefaultLanguage;
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
					//not useful
					output = languageCode;
				}
				else
				{
					foreach ( var item in map )
					{
						cntr++;
						if ( cntr == 1 )
						{
							//may want to do something with the language?
							output = item.Value;
							break;
						}
					}
				}
			}
			//??
			DefaultLanguage = output;
			return output;
		}

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
				elm.LanguageMapString = JsonConvert.SerializeObject( map, GetJsonSettings() );
				elm.EntityUid = bo.RowId;
				elm.Property = property;
				//don't have to convert if just serializing
				lastLanguageMapString = elm.LanguageMapString;
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

		public List<WPM.TextValueProfile> MapInLanguageToTextValueProfile( List<string> list, string property )
		{
			List<WPM.TextValueProfile> output = new List<WPM.TextValueProfile>();
			lastLanguageMapListString = "";
			if ( list == null || !list.Any() )
			{
				return output;
			}
			foreach ( var lang in list )
			{
				if ( !string.IsNullOrWhiteSpace( lang ) )
				{
					//21-04-05 mp - new try to retain the original language code
					var language = CodesManager.GetLanguage( lang );
					if ( language != null && language.Id > 0 )
					{
						output.Add( new WPM.TextValueProfile()
						{
							CodeId = language.CodeId,
							TextTitle = language.Name,
							TextValue = language.Value
						} );
					} else
					{
						LoggingHelper.DoTrace( 1, string.Format( "Unknown language code encountered: {0} for {1}", lang, property ) );
						//???
						output.Add( new WPM.TextValueProfile()
						{
							CodeId = 0,
							TextTitle = lang,
							TextValue = lang
						} );
					}
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
			output.Value = input.Value != null ? (decimal)input.Value : 0;
			output.MinValue = input.MinValue;
			output.MaxValue = input.MaxValue;
			output.Percentage = input.Percentage != null ? ( decimal )input.Percentage : 0;
			output.Description = HandleLanguageMap( input.Description, property );
			//need to distinguish QV that uses creditvalue
			if ( isCreditValue )
			{
				output.CreditUnitType = MapCAOToEnumermation( input.UnitText );
				if ( output.CreditUnitType!= null && output.CreditUnitType.HasItems() )
				{
					output.UnitText = output.CreditUnitType.GetFirstItem().Name;
				}
			} else
			{
				output.UnitText = (input.UnitText ?? new MJ.CredentialAlignmentObject()).TargetNodeName.ToString();
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
		public List<MC.QuantitativeValue> HandleValueProfileListToQVList( List<MJ.ValueProfile> list, string property)
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

		public List<MC.ValueProfile> HandleQVListToValueProfileList( List<MJ.QuantitativeValue> list, string property )
		{
			var output = new List<MC.ValueProfile>();
			MC.ValueProfile profile = new MC.ValueProfile();
			if ( list == null || list.Count == 0 )
			{
				return output;
			}

			foreach ( var input in list )
			{
				profile = new MC.ValueProfile();
				//
				if ( input.Value == 0 && input.MinValue == 0 && input.MaxValue == 0 && input.Percentage == 0
				&& ( input.Description == null || input.Description.Count() == 0 )
				)
				{
					//should log - although API should not allow this?
					return null;
				}
				profile.Value = input.Value != null && input.Value > 0 ? (decimal) input.Value : 0;
				profile.MinValue = input.MinValue;
				profile.MaxValue = input.MaxValue;
				profile.Description = HandleLanguageMap( input.Description, property );
				//
				profile.CreditUnitType = MapCAOToEnumermation( input.UnitText );
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
			output.CreditLevelType.Name = "Credit Level Type";
			output.Subject = MapCAOListToEnumermation( input.Subject, false );
			//output.SubjectList = MapCAOListToList( input.Subject );
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
					IdentifierTypeName = HandleLanguageMap( item.IdentifierTypeName, "IdentifierTypeName", false )
					//Description = HandleLanguageMap( item.Description, currentBaseObject, "IdentifierValue Description", true )
				};
				list.Add( iv );
			}

			return list;
		}
		//replace latter with 

		public List<MC.IdentifierValue> MapIdentifierValueList2( List<MJ.IdentifierValue> property )
		{
			var list = new List<MC.IdentifierValue>();
			if ( property == null || property.Count == 0 )
				return list;
			var iv = new MC.IdentifierValue();
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
		//
		public MC.Enumeration MapCAOListToEnumermation( List<MJ.CredentialAlignmentObject> input, bool isForConceptScheme = true )
		{
			//
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
					var ei = new workIT.Models.Common.EnumeratedItem()
					{
						SchemaName = item.TargetNode ?? "",
						Name = nodeName ?? "",
						LanguageMapString = lastLanguageMapString
					};
					var codeItem = CodesManager.GetPropertyBySchema( item.TargetNode );
					if (codeItem != null && codeItem.Id > 0)
					{
						ei.Id = codeItem.Id;
						if ( isForConceptScheme )
						{
							output.Id = codeItem.CategoryId;
							output.Name = codeItem.Category;
						}
					}
					output.Items.Add( ei );
					//do a direct look up of the PropertyValueId using TargetNode. ex: creditUnit:DegreeCredit
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
		public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCAOProfileList( List<MJ.CredentialAlignmentObject> input, bool isACompetencyType = false )
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
			var entity = new MC.CredentialAlignmentObjectProfile();

			if ( input == null || input.Count == 0 )
				return output;

			var previousFramework = "";
			bool isFrameworkACollection = false;
			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item == null )
					continue;
				string targetNodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );
				//continue as long as there is a name
				if ( item != null && ( !string.IsNullOrEmpty( targetNodeName ) ) )
				{
					entity = new MC.CredentialAlignmentObjectProfile()
					{
						TargetNode = item.TargetNode ?? "",
						CodedNotation = item.CodedNotation ?? "",
						FrameworkName = HandleLanguageMap( item.FrameworkName, currentBaseObject, "FrameworkName" ),
						FrameworkName_Map= ConvertLanguageMap( item.FrameworkName ),
						Weight = item.Weight
						//Weight = StringtoDecimal( item.Weight )
					};
					entity.TargetNodeCTID = ResolutionServices.ExtractCtid( item.TargetNode );

					//entity.TargetNodeName = HandleLanguageMap( item.ConvertLanguageMap( item.TargetNodeName ), currentBaseObject, "TargetNodeName", false );
					entity.TargetNodeName = targetNodeName;
					entity.TargetNodeName_Map = ConvertLanguageMap( item.TargetNodeName );
					entity.TargetNodeDescription = HandleLanguageMap( item.TargetNodeDescription, currentBaseObject, "TargetNodeDescription", false );
					entity.TargetNodeDescription_Map = ConvertLanguageMap( item.TargetNodeDescription) ;

					//won't know if url or registry uri, but likely non- registry.
					if ( !string.IsNullOrWhiteSpace( item.Framework ) )
					{
						var isRegistryURL = true;
						//TODO - need to handle ce-registry/... so communities as well!!
						if ( item.Framework.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) == -1
							&& item.Framework.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) == -1
							&& item.Framework.ToLower().IndexOf( "credentialengineregistry.org/" ) == -1
							)
						{
							entity.Framework = item.Framework;
							isRegistryURL = false;
							//determine if competency framework or collection
						}
						else
						{
							//doesn't really matter
							entity.Framework = item.Framework;
						}
						if ( previousFramework != item.Framework )
                        {
							isFrameworkACollection = false;
							if ( isACompetencyType )
							{
								if ( isRegistryURL )
								{
									string ctdlType = "";
									string statusMessage = "";
									var resource = RegistryServices.GetResourceByUrl( item.Framework, ref ctdlType, ref statusMessage );
									if ( ctdlType.IndexOf( "Collection" ) > -1 )
									{
										//now what?
										isFrameworkACollection = true;
									}
								}
							}
						}
						previousFramework = item.Framework;
					} else
                    {
						//how to tell if a collection, if no framework? Would have to look up the competency? But in theory can be in a collection or framework. 
						//Check for IsMemberOf
                    }

					entity.FrameworkIsACollection = isFrameworkACollection;
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
							FaxNumber = cpi.FaxNumber,
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
						cp.FaxNumber = cpi.FaxNumber;
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
					njp.GlobalJurisdiction = jp.GlobalJurisdiction ?? false;
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
						gc.IsException = true;
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
				if ( input.Latitude != null && input.Latitude != 0 )
				{
					gc.Latitude = (double)input.Latitude;
					gc.Longitude = ( double ) input.Longitude;
				}


			}
			return gc;
		}
		private MC.Address MapAddress( InputAddress input )
		{

			//20-08-23 NOTE - partial addresses are allowed
			MC.Address output = new MC.Address()
			{
				PostalCode = input.PostalCode,
				PostOfficeBoxNumber=input.PostOfficeBoxNumber,
				Latitude = input.Latitude == null ? 0 : (double) input.Latitude,
				Longitude = input.Longitude == null ? 0 : ( double ) input.Longitude
			};
			output.Name = HandleLanguageMap( input.Name, currentBaseObject, "PlaceName", false );
			output.Description = HandleLanguageMap( input.Description, currentBaseObject, "Place.Description", false );
			output.Name_Map = lastLanguageMapString;

			output.StreetAddress = HandleLanguageMap( input.StreetAddress, currentBaseObject, "StreetAddress", false );
			output.Address1_Map = lastLanguageMapString;

			output.AddressLocality = HandleLanguageMap( input.City, currentBaseObject, "City", false );
			output.City_Map = lastLanguageMapString;

			output.AddressRegion = HandleLanguageMap( input.AddressRegion, currentBaseObject, "AddressRegion", false );

			output.AddressRegion_Map = lastLanguageMapString;
			output.SubRegion = HandleLanguageMap( input.SubRegion, currentBaseObject, "SubRegion", false );

			output.AddressCountry = HandleLanguageMap( input.Country, currentBaseObject, "Country", false );
			output.Country_Map = lastLanguageMapString;
			//only if USA
			if ( output.AddressRegion.Length < 3 )
			{
				output.HasShortRegion = true;
				if ( output.AddressRegion!= null && output.AddressRegion.Length == 2 && !string.IsNullOrWhiteSpace(output.AddressCountry) )
				{
					//should check country? 
					var fullRegion = "";
					int boostLevel = 1;
					if ( CodesManager.Codes_IsState( output.AddressRegion, ref fullRegion, ref boostLevel ) )
					{
						output.AddressRegion = fullRegion;
					}
				}
			}

			output.IdentifierOLD = MapIdentifierValueList( input.Identifier );
			if ( output.IdentifierOLD != null && output.IdentifierOLD.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.IdentifierOLD, MappingHelperV3.GetJsonSettings() );
			}
			//now mignt be a better time to do the geo coding?
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
				profile.DataCollectionMethodType = MapCAOListToEnumermation( input.DataCollectionMethodType );
				profile.ExternalInputType = MapCAOListToEnumermation( input.ExternalInputType );

				profile.ProcessMethod = input.ProcessMethod ?? "";
				profile.ProcessStandards = input.ProcessStandards ?? "";
				profile.ScoringMethodExample = input.ScoringMethodExample ?? "";
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );

				//while the profiles is a list, we are only handling single
				profile.ProcessingAgentUid = MapOrganizationReferencesToGuid( "ProcessProfile.ProcessingAgentUid", input.ProcessingAgent, ref status );

				//targets
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					profile.TargetCredentialIds = MapEntityReferences( "ProcessProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
					profile.TargetAssessmentIds = MapEntityReferences( "ProcessProfile.TargetAssessment", input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
				if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
					profile.TargetLearningOpportunityIds = MapEntityReferences( "ProcessProfile.TargetLearningOpportunity", input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

				if ( input.TargetCompetencyFramework != null && input.TargetCompetencyFramework.Count > 0 )
				{
					profile.TargetCompetencyFrameworkIds = MapEntityReferences( "ProcessProfile.TargetCompetencyFramework", input.TargetCompetencyFramework, CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK, ref status );
				}
				//

				output.Add( profile );
			}

			return output;
		}
		#endregion
		#region  AggregateDataProfile, Earnings, Holders profile
		public List<MC.AggregateDataProfile> FormatAggregateDataProfile( string parentCTID, List<MJ.AggregateDataProfile> profiles, List<BNode> bnodes, ref SaveStatus status )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;

			var output = new List<MC.AggregateDataProfile>();
			foreach ( var input in profiles )
			{
				if ( input == null )
					continue;
				Guid rowId = Guid.NewGuid();
				var profile = new MC.AggregateDataProfile
				{
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					ExpirationDate = MapDate( input.ExpirationDate, "DateEffective", ref status ),
					Currency = input.Currency,
					CurrencySymbol = input.Currency,//???
					HighEarnings = input.HighEarnings,
					LowEarnings = input.LowEarnings,
					MedianEarnings = input.MedianEarnings,
					PostReceiptMonths = input.PostReceiptMonths
				};
				profile.Name = HandleLanguageMap( input.Name, "Name", true );
				profile.Source = input.Source;
				profile.Description = HandleLanguageMap( input.Description, "Description", true );
				profile.DemographicInformation = HandleLanguageMap( input.DemographicInformation, "DemographicInformation", true );
				//
				if ( input.JobsObtained != null && input.JobsObtained.Any() )
				{
					profile.JobsObtained = HandleQuantitiveValueList( input.JobsObtained, "AggregateDataProfile.JobsObtained", false );
				}
                //handle: RelevantDataSet
                profile.RelevantDataSetList = MapEntityReferences( "AggregateData.RelevantDataSet", input.RelevantDataSet, CodesManager.ENTITY_TYPE_DATASET_PROFILE, ref status );
				//22-06-13 - should both of these be done?
				//if ( input.RelevantDataSet != null && input.RelevantDataSet.Any() )
				//{
				//	//this will be a list of URIs
				//	foreach ( var item in input.RelevantDataSet )
				//	{
				//		//could use URI, but will use ctid
				//		var ctid = ResolutionServices.ExtractCtid( item );
				//		if ( string.IsNullOrWhiteSpace( ctid ) )
				//		{
				//			status.AddError( string.Format( "Error: Unable to derive a ctid from the AggregateDataProfile.RelevantDataSet for AggregateDataProfile (parentCTID: '{0}') using RelevantDataSet URI: '{1}'", parentCTID, item ) );
				//			continue;
				//		}
				//		//get dataset profile
				//		//21-10-07 - the DSP will no longer be in the graph. 
				//		var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.CTID == ctid );
				//		if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CTID ) )
				//		{
				//			//status.AddError( string.Format( "Error: Unable to find the DataSetProfile for AggregateDataProfile (parentCTID: '{0}') using dataSetProfile CTID: '{1}'", parentCTID, ctid ) );
				//			//continue;
				//			//create a pending record
							
								
				//		}
				//		else
				//		{
				//			//TBD - now passing parentCTID, rather than profile CTID (as we don't have one). OK??? - should be, just for display
				//			var dspo = FormatDataSetProfile( parentCTID, dspi, ref status );
				//			//may want to store the ctid, although will likely use relationships, or store the json
				//			if ( dspo != null )
				//			{
				//				//this is not current used
				//				//profile.RelevantDataSetList.Add( dspo.CTID );
				//				profile.RelevantDataSet.Add( dspo );
				//			}
				//		}
				//	}
				//}
				output.Add( profile );
			}

			return output;
		}

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
					CTID = input.CTID,
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
							status.AddError( string.Format( "Error: Unable to derive a ctid from the EarningsProfile.RelevantDataSet for EarningsProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.CTID, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.CTID == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CTID ) )
						{
							status.AddError( string.Format( "Error: Unable to find the DataSetProfile for EarningsProfile (EP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.CTID, ctid ) );
							continue;
						}
						var dspo = FormatDataSetProfile( input.CTID, dspi, ref status );
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
				LoggingHelper.DoTrace( 7, string.Format( "FormatHoldersProfile. parentCTID: {0}, CTID: {1}, ", parentCTID, input.CTID ) );

				//holders profile doesn't have a name
				var profile = new MC.HoldersProfile
				{
					RowId = rowId, //?????????????
					CTID = input.CTID,
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
							status.AddError( string.Format( "Error: Unable to derive a ctid from the HoldersProfile.RelevantDataSet for HoldersProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.CTID, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.CTID == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CTID) )
						{
							status.AddError( string.Format("Error: Unable to find the DataSetProfile for HoldersProfile (HP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.CTID, ctid ));
							continue;
						}
						var dspo = FormatDataSetProfile( input.CTID, dspi, ref status );
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
		/// <summary>
		/// Format a DataSetProfile
		/// </summary>
		/// <param name="parentCTID">21-02-25 - this will be the credential CTID for an AggDataProfile. Would be null for a standalone dsp! Apparantly only for documentation!</param>
		/// <param name="input"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public MCQ.DataSetProfile FormatDataSetProfile( string parentCTID, MJ.QData.DataSetProfile input, ref SaveStatus status )
		{
			LoggingHelper.DoTrace( 7, string.Format( "FormatDataSetProfile. parentCTID: {0}, CTID: {1}, ", parentCTID, input.CTID ) );


			var output = new MCQ.DataSetProfile()
			{
				CTID = input.CTID,
				Name = HandleLanguageMap( input.Name, "Name" ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				DataSuppressionPolicy = HandleLanguageMap( input.DataSuppressionPolicy, "DataSuppressionPolicy" ),
				Source = input.Source,
				SubjectIdentification = HandleLanguageMap( input.SubjectIdentification, "SubjectIdentification" ),
			};
			output.DataProviderUID = MapOrganizationReferenceGuid( "DataSetProfile.DataProvider", input.DataProvider, ref status );
			if (output.DataProviderUID == null || output.DataProviderUID == Guid.Empty)
			{
				output.DataProviderUID = CurrentOwningAgentUid;
			}
			//about can be cred, asmt, or lopp. These could be references
			//make sure these are added to entity.cache immediately
			output.AboutUids = MapEntityReferenceGuids( "DataSetProfile.About", input.About, 0, ref status );
			//not sure we need to do RelevantDataSetFor, as should have already been established
			//22-06-14 mp - if About is empty, then RelevantDataSetFor will be important. It should be obsolete, add handling for importing old data.
			//22-09-28 mparsons	- effectively obsolete outside of HoldersProfile, EarningsProfile, EmploymentOutlook and the latter are moving to be obsolete
			//if ( (input.About == null || input.About.Count == 0) && ( input.RelevantDataSetFor!= null && input.RelevantDataSetFor.Count > 0) )
			//         {
			//	output.AboutUids = MapEntityReferenceGuids( "DataSetProfile.About", input.RelevantDataSetFor, 0, ref status );
			//}
			
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

			try
			{

				if ( input.DataSetTimePeriod != null && input.DataSetTimePeriod.Any() )
				{
					//
					//this will now be a list of DataSetTimeFrame
					//and will not have ctdlId
					foreach ( var item in input.DataSetTimePeriod )
					{
						var dspo = FormatDataSetTimeFrame( input.CTID, item, ref status );
						if ( dspo != null )
						{
							output.DataSetTimePeriod.Add( dspo );
						}
					}
				}
				
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "FormatDataSetProfile for parentCTID: " + parentCTID );
			}
			return output;
		}
		//
		public MCQ.DataSetTimeFrame FormatDataSetTimeFrame( string parentCTID, MJ.QData.DataSetTimeFrame input, ref SaveStatus status )
		{
			//|| string.IsNullOrWhiteSpace( input.CtdlId )
			if ( input == null  )
				return null;

			LoggingHelper.DoTrace( 7, string.Format( "FormatDataSetTimeFrame. parentCTID: {0}, Name: {1}, ", parentCTID, input.Name ) );

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

			try
			{
				if ( input.DataAttributes != null && input.DataAttributes.Any() )
				{
					foreach ( var item in input.DataAttributes )
					{
						var dspo = FormatDataProfiles( parentCTID, item, ref status );
						//may want to store the ctid, although will likely use relationships, or store the json
						if ( dspo != null )
						{
							//output.DataAttributesList.Add( dspo.bnID );
							output.DataAttributes.Add( dspo );
						}
					}
				}

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "FormatDataSetTimeFrame for parentCTID: " + parentCTID );
			}
			return output;
		}
		//
		public MCQ.DataProfile FormatDataProfiles( string parentCTID, MJ.QData.DataProfile input, ref SaveStatus status )
		{
			//|| string.IsNullOrWhiteSpace( input.CtdlId )
			if ( input == null  )
				return null;
			MCQ.DataProfileJson qdSummary = new MCQ.DataProfileJson();
			LoggingHelper.DoTrace( 7, "FormatDataProfiles. parentCTID: " + parentCTID );
			var output = new MCQ.DataProfile()
			{
				//bnID = input.CtdlId,
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
			try
			{
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


				//for now map to separate properties.
				output.DataProfileAttributes.DataAvailable = HandleQuantitiveValueList( input.DataAvailable, "DataProfile.DataAvailable", "Data Available", ref qdSummary, false );
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
				output.DataProfileAttributes.PassRate = HandleQuantitiveValueList( input.PassRate, "DataProfile.PassRate", "Pass Rate", ref qdSummary, false );
				//
				output.DataProfileAttributes.RegionalEarningsDistribution = HandleQuantitiveValueList( input.RegionalEarningsDistribution, "DataProfile.RegionalEarningsDistribution", "Regional Earnings Distribution", ref qdSummary, false );
				output.DataProfileAttributes.RegionalEmploymentRate = HandleQuantitiveValueList( input.RegionalEmploymentRate, "DataProfile.RegionalEmploymentRate", "Regional Employment Rate", ref qdSummary, false );

				output.DataProfileAttributes.RelatedEmployment = HandleQuantitiveValueList( input.RelatedEmployment, "DataProfile.RelatedEmployment", "Related Employment", ref qdSummary, false );

				//SubjectExcluded, SubjectIncluded
				//output.DataProfileAttributes.SubjectIncluded = FormatSubjectProfile( input.SubjectIncluded, ref status );
			//	output.DataProfileAttributes.SubjectExcluded = HandleQuantitiveValueList( input.SubjectExcluded, "DataProfile.SubjectExcluded", "SubjectExcluded", ref qdSummary, false );

				output.DataProfileAttributes.SubjectsInSet = HandleQuantitiveValueList( input.SubjectsInSet, "DataProfile.SubjectsInSet", "Subjects In Set", ref qdSummary, false );
				output.DataProfileAttributes.SufficientEmploymentCriteria = HandleQuantitiveValueList( input.SufficientEmploymentCriteria, "DataProfile.SufficientEmploymentCriteria", "Sufficient Employment Criteria", ref qdSummary, false );
				output.DataProfileAttributes.UnrelatedEmployment = HandleQuantitiveValueList( input.UnrelatedEmployment, "DataProfile.UnrelatedEmployment", "Unrelated Employment", ref qdSummary, false );
				//
				output.DataProfileAttributes.TotalWIOACompleters = HandleQuantitiveValueList( input.TotalWIOACompleters, "DataProfile.TotalWIOACompleters", "Total WIOA Completers", ref qdSummary, false );
				output.DataProfileAttributes.TotalWIOAParticipants = HandleQuantitiveValueList( input.TotalWIOAParticipants, "DataProfile.TotalWIOAParticipants", "Total WIOA Participants", ref qdSummary, false );
				output.DataProfileAttributes.TotalWIOAExiters = HandleQuantitiveValueList( input.TotalWIOAExiters, "DataProfile.TotalWIOAExiters", "Total WIOA Exiters", ref qdSummary, false );
				//
				output.DataProfileAttributeSummary = qdSummary;

			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, "FormatDataProfiles for parentCTID: " + parentCTID );
			}
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
				if ( !string.IsNullOrWhiteSpace( ma.Currency ) )
				{
					if ( ma.Currency.ToLower() == "usd" )
						ma.CurrencySymbol = "$";
					else
					{
						var currency = CodesManager.GetCurrencyItem( ma.Currency );
						if ( currency != null && currency.NumericCode > 0 )
						{
							ma.CurrencySymbol = currency.HtmlCodes;
						}
					}
				}
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
				if ( !string.IsNullOrWhiteSpace( ma.Currency ) )
				{
					if ( ma.Currency.ToLower() == "usd" )
						ma.CurrencySymbol = "$";
					else
					{
						var currency = CodesManager.GetCurrencyItem( ma.Currency );
						if ( currency != null && currency.NumericCode > 0 )
						{
							ma.CurrencySymbol = currency.HtmlCodes;
						}
					}
				}
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
				sp.SubjectType = MapCAOListToEnumermation( input.SubjectType, false );

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
					CTID = input.CTID,
					Name = HandleLanguageMap( input.Name, "Name" ),
					Description = HandleLanguageMap( input.Description, "Description" ),
					DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					//JobsObtained = input.JobsObtained
				};
				if ( input.JobsObtained != null && input.JobsObtained.Any())
				{
					profile.JobsObtainedList = HandleQuantitiveValueList( input.JobsObtained, "EmploymentOutcomeProfile.JobsObtained", false );
					if ( profile.JobsObtainedList != null && profile.JobsObtainedList.Any() )
					{
						//if ( profile.JobsObtainedList[ 0 ].Value > 0 )
						//{
						//	profile.JobsObtained = Decimal.ToInt32( profile.JobsObtainedList[ 0 ].Value );
						//}
					}

				}
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
							status.AddError( string.Format( "Error: Unable to derive a ctid from the EmploymentOutcomeProfile.RelevantDataSet for EmploymentOutcomeProfile (HP.CTID: '{0}') using RelevantDataSet URI: '{1}'", input.CTID, item ) );
							continue;
						}
						//get dataset profile
						var dspi = outcomesDTO.DataSetProfiles.FirstOrDefault( s => s.CTID == ctid );
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CTID ) )
						{
							status.AddError( string.Format( "Error: Unable to find the DataSetProfile for EmploymentOutcomeProfile (EOP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.CTID, ctid ) );
							continue;
						}
						var dspo = FormatDataSetProfile( input.CTID, dspi, ref status );
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
				//or could be a partial date. If in registry, should be valid
				//status.AddWarning( string.Format( "Error - {0} is invalid: '{1}' ", dateName, date ) );
				return date.Trim();
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
		public Guid MapOrganizationReferencesToGuid( string property, List<string> input, ref SaveStatus status )
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


		public void MapOrganizationPublishedBy( MC.TopLevelObject output, ref SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
			{
				//unlikely, but return? or check for previous?
				//NOTE: took approach to only handle a publisher if different than owner? Not sure if this is correct?
				return;
			}
			var swp = "";
			var porg = OrganizationManager.GetSummaryByCtid( status.DocumentPublishedBy );
			if ( porg != null && porg.Id > 0 )
			{
				//TODO - store this in a json blob??????????
				if ( status.DocumentPublishedBy != status.DocumentOwnedBy )
					output.PublishedByThirdPartyOrganizationId = porg.Id;
				//
				//this will result in being added to Entity.AgentRelationship
				//TODO - this also should really only apply to a third party publisher
				output.PublishedBy = new List<Guid>() { porg.RowId };
			}
			else
			{
				//if publisher not imported yet, all publishee stuff will be orphaned
				var entityUid = Guid.NewGuid();
				var statusMsg = "";
				if (!string.IsNullOrWhiteSpace( status.ResourceURL ) )
                {
					var resPos = status.ResourceURL.IndexOf( "/resources/" );
					swp = status.ResourceURL.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;
					
				} else
                {
					var registryURL = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );
					status.Community = status.Community ?? UtilityManager.GetAppKeyValue( "defaultCommunity" );
					//we have CTID, so make up a URL
					swp = string.Format(registryURL, status.Community, status.DocumentPublishedBy);
					status.ResourceURL = swp;
				}
				int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref status );
				if ( status.DocumentPublishedBy != status.DocumentOwnedBy )
					output.PublishedByThirdPartyOrganizationId = porg.Id;
				output.PublishedBy = new List<Guid>() { entityUid };
			}
		}

		#endregion


		#region  Entities
		public static int GetEntityTypeId( string entityType )
		{
			int entityTypeId = 0;
			//OR just remove ceterms etc, to reduce number of checks
			switch ( entityType.ToLower() )
			{
				case "credential":
				case "apprenticeshipcertificate":
				case "associatedegree":
				case "bachelordegree":
				case "badge":
				case "certificate":
				case "certificateofcompletion":
				case "participationcertificate":
				case "certification":
				case "degree":
				case "diploma":
				case "digitalbadge":
				case "doctoraldegree":
				case "generaleducationdevelopment":
				case "journeymancertificate":
				case "license":
				case "mastercertificate":
				case "masterdegree":
				case "microcredential":
				case "openbadge":
				case "professionaldoctorate":
				case "qualityassurancecredential":
				case "researchdoctorate":
				case "secondaryschooldiploma":
				case "ceterms:apprenticeshipcertificate":
				case "ceterms:associatedegree":
				case "ceterms:bachelordegree":
				case "ceterms:badge":
				case "ceterms:certificate":
				case "ceterms:certificateofcompletion":
				case "ceterms:participationcertificate":
				case "ceterms:certification":
				case "ceterms:degree":
				case "ceterms:diploma":
				case "ceterms:digitalbadge":
				case "ceterms:doctoraldegree":
				case "ceterms:generaleducationdevelopment":
				case "ceterms:journeymancertificate":
				case "ceterms:license":
				case "ceterms:mastercertificate":
				case "ceterms:masterdegree":
				case "ceterms:microcredential":
				case "ceterms:openbadge":
				case "ceterms:professionaldoctorate":
				case "ceterms:qualityassurancecredential":
				case "ceterms:researchdoctorate":
				case "ceterms:secondaryschooldiploma":
					entityTypeId = 1;
					break;
				case "ceterms:credentialorganization":
				case "credentialorganization":
					entityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
					break;
				case "ceterms:qacredentialorganization":
				case "qacredentialorganization":
					entityTypeId = CodesManager.ENTITY_TYPE_QAORGANIZATION;
					break;
				case "ceterms:organization":
				case "organization":
					entityTypeId = CodesManager.ENTITY_TYPE_PLAIN_ORGANIZATION;
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
				case "ceterms:course":
				case "course":
					entityTypeId = 37;
					break;
				case "ceterms:learningprogram":
				case "learningprogram":
					entityTypeId = 36;
					break;
				case "ceterms:pathway":
				case "pathway":
					entityTypeId = 8;
					break;
				case "ceterms:collection":
				case "collection":
					entityTypeId = 9;
					break;
				
				case "ceasn:competencyframework":
				case "competencyframework":
					//ISSUE - still have references to 17 in places for CaSS competencies
					entityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY_FRAMEWORK;
					break;
				case "skos:conceptscheme":
				case "conceptscheme":
					entityTypeId = CodesManager.ENTITY_TYPE_CONCEPT_SCHEME;
					break;
				case "ceterms:progressionmodel":
				case "progressionmodel":
					entityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
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
				//this will not have an import
				case "ceterms:aggregatedataprofile":
				case "aggregatedataprofile":
					entityTypeId = 27;
					break;
				case "ceterms:transferintermediary":
				case "transferintermediary":
					entityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_INTERMEDIARY;
					break;
				//gap
				case "qdata:datasetprofile":
				case "datasetprofile":
					entityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
					break;
				case "ceterms:job":
				case "job":
					entityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
					break;
				case "ceterms:task":
				case "task":
					entityTypeId = CodesManager.ENTITY_TYPE_TASK_PROFILE;
					break;
				case "ceterms:workrole":
				case "workrole":
					entityTypeId = CodesManager.ENTITY_TYPE_WORKROLE_PROFILE;
					break;
				case "ceterms:occupation":
				case "occupation":
					entityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE;
					break;
				//renumber these for future removal???
				case "ceterms:earningsprofile":
				case "earningsprofile":
					entityTypeId = CodesManager.ENTITY_TYPE_EARNINGS_PROFILE;
					break;
				case "ceterms:holdersprofile":
				case "holdersprofile":
					entityTypeId = CodesManager.ENTITY_TYPE_HOLDERS_PROFILE;
					break;
				case "ceterms:employmentoutcomeprofile":
				case "employmentoutcomeprofile":
					entityTypeId = CodesManager.ENTITY_TYPE_EMPLOYMENT_OUTCOME_PROFILE;
					break;
				case "asn:rubric":
				case "rubric":
					entityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
					break;
				default:
					//default to credential??? - NO
					//20-04-09 mp - no longer can do this with addition of navy data
					entityTypeId = 0;
					break;
			}
			return entityTypeId;
		}

		/// <summary>
		/// Map a List of references to a List of Guids.
		/// For blank nodes:
		/// - get the type
		/// - call method to save the blank node as the appropriate type
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId">If zero, look up by ctid, or </param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<Guid> MapEntityReferenceGuids( string property, List<string> input, int entityTypeId, ref SaveStatus status, string parentCTID = "" )
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
                entityRef = new Guid();
                //determine if just Id, or base
                if ( target.StartsWith( "http" ) )
				{
					registryAtId = target;
					if ( registryAtId != registryAtId.ToLower() )
					{
						status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, registryAtId ) );
					}
					//TODO - need to ensure existing reference entities can be updated!!!!!!!!
					entityRef = ResolveEntityRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status, parentCTID );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					//should be a blank node
					var node = GetBlankNode( target );
					string name = HandleBNodeLanguageMap( node.Name, "blank node name", true );
					if ( node == null || string.IsNullOrWhiteSpace( name ) )
					{
						status.AddError( string.Format( "A Blank node was not found for bNodeId of: {0}. ", target ) );
						continue;
						//return entityRefs;
					}
					
					string desc = HandleBNodeLanguageMap( node.Description, "blank node desc", true );
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
		private Guid ResolveEntityRegistryAtIdToGuid( string property, string registryAtId, int entityTypeId, ref SaveStatus status, string parentCTID = "" )
		{
			bool isResolved = false;
			Guid entityUID = new Guid();
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				entityUID = ResolutionServices.ResolveEntityByRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status, ref isResolved, parentCTID );
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
				else if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
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
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
				entityRefId = ResolveBlankNodeAsOrganization( property, input, ref entityUID, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				entityRefId = ResolveBaseEntityAsAssessment( input, ref entityUID, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
				entityRefId = ResolveBaseEntityAsLopp( input, ref entityUID, ref status );

			//TODO
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
			{
				//entityRefId = ResolveBaseEntityAsOccupation( input, ref entityUID, ref status );
				status.AddError( string.Format( "Error - OCCUPATION blank nodes are not currently handled. entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
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
					//TODO - what to do with non-registry URIs? For example
					// http://dbpedia.com/Stanford_University
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
		public int MapEntityReference ( string property, string target, int entityTypeId, ref SaveStatus status, bool allowingBlankNodes = true )
		{
			int entityRef = 0;
			string registryAtId = "";
			if ( string.IsNullOrWhiteSpace ( target ) )
				return 0;

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
				if ( !allowingBlankNodes )
				{
					//what to do? log and what - don't necessarily want to send an email
				}
				else
				{
					LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target bnode: {1} ", entityTypeId, target ) );
					var node = GetBlankNode( target );
					//if type present,can use
					entityRef = ResolveEntityBaseToInt( property, node, entityTypeId, ref status );
				}
			}			

			return entityRef;
		}

		/// <summary>
		/// Entities will be string, where cannot be a third party reference
		/// That is not blank nodes
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityReferenceTypeId">TODO - try to keep generic for lopp subclasses, etc. </param>
		/// <param name="parentEntityTypeId">Used for messages only</param>
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

		/// <summary>
		/// Resolve a registry entity to a record in the database.
		/// </summary>
		/// <param name="registryAtId"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
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
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
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
			MC.Credential output = new MC.Credential();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			try
			{
				//get full record for updates
				//really should limit this so whole record isn't retrieved
				output = CredentialManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
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
					//output.CodedNotation = input.CodedNotation;
					//
					output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
					//
					output.Identifier = MapIdentifierValueList( input.Identifier );
					if ( output.Identifier != null && output.Identifier.Count() > 0 )
					{
						output.IdentifierJSON = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
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
				//output.CodedNotation = input.CodedNotation;
				//
				output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
				//
				output.Identifier = MapIdentifierValueList( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJSON = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
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
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBaseEntityAsCredential(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}
			return entityRefId;
		}
		private int ResolveBlankNodeAsOrganization( string property, BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			status.HasSectionErrors = false;
			if ( input == null )
				return entityRefId;
			string name = "";
			try
			{
				name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
				string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

				//look up by name, subject webpage
				//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
				//NOTE: need to avoid duplicate swp's; so  should combine
				//Be sure this method will return all properties that could be present in a BN
				//20-08-23  - add email, address, and availabilityListing
				//21-12-07 - the URL checking should use the domain only, and handle with and without www
				//22-09-09 - if we have a bnode and a full org exists, there should NOT be an update!
				//TODO - delete existing references for the org
				if ( !string.IsNullOrWhiteSpace( name ) )
				{
					name = name.Replace( " &amp; ", " and " ).Replace( " & ", " and " );
					var org = OrganizationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
					if ( org != null && org.Id > 0 )
					{
						if (org.EntityStateId == 3)
                        {
							return org.Id;
						}
						//20-07-04 - hmmm - are updates not being done??
						//			- the problem with QA references like accredited by, could be variations in description, as well as name and swp!
						//			- may have to check extended properties and first don't override, and perhaps warn/email on any changes	
						entityUid = org.RowId;
						if ( string.IsNullOrWhiteSpace( org.AgentDomainType ) )
							org.AgentDomainType = input.Type;
						if ( string.IsNullOrWhiteSpace( org.Description ) )
							org.Description = desc;
						else if ( !string.IsNullOrWhiteSpace( desc) )
						{
							//what now? Take the longer one?
                        }
						//20-08-23 - new additions to org BN
						org.Addresses = FormatAvailableAtAddresses( input.Address, ref status );
						//may not be applicable
						if ( org.Addresses != null && org.Addresses.Any() )
							org.AddressesJson = JsonConvert.SerializeObject( org.Addresses, MappingHelperV3.GetJsonSettings() );
						//
						org.AvailabilityListing = MapListToString( input.AvailabilityListing );
						//future prep
						org.AvailabilityListings = input.AvailabilityListing;
						//
						org.Emails = MapToTextValueProfile( input.Email );
						//do update 
						new OrganizationManager().Save( org, ref status );

						return org.Id;
					}
				}
				//need type of org!!
				var output = new MC.Organization()
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
				//output.AvailabilityListings = input.AvailabilityListing;
				//
				output.Emails = MapToTextValueProfile( input.Email );


				//20-08-23 - update this to handle new additions
				if ( new OrganizationManager().AddReferenceOrganization( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			} catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBlankNodeAsOrganization(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID,( name ?? "missing")) );
				status.AddError( ex.Message );
				return 0;
			}

			return entityRefId;
		}
		private int ResolveBaseEntityAsAssessment( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			WPM.AssessmentProfile output = new WPM.AssessmentProfile();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			try
			{
				output = AssessmentManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
				if ( output != null && output.Id > 0 )
				{
					//20-12-08 mp - combine mapping

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
				output.AssessesCompetencies = MapCAOListToCAOProfileList( input.Assesses, true );
				output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );

				output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
				//
				output.CodedNotation = input.CodedNotation;
				//output.QVCreditValueList = HandleValueProfileListToQVList( input.CreditValue, "Assessment.CreditValue" );
				output.CreditValue = HandleValueProfileList( input.CreditValue, "Assessment.CreditValue" );
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );

				//get rid of this:
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				output.DeliveryType = MapCAOListToEnumermation( input.DeliveryType );

				//
				output.EstimatedDuration = FormatDuration( input.EstimatedDuration, ref status );
				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;
				//
				output.Identifier = MapIdentifierValueList( input.Identifier );
				if ( output.Identifier != null && output.Identifier.Count() > 0 )
				{
					output.IdentifierJSON = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
				}
				//
				output.LearningMethodDescription = HandleLanguageMap( input.LearningMethodDescription, output, "LearningMethodDescription" );
				output.OfferedBy = MapOrganizationReferenceGuids( "Assessment.OfferedBy", input.OfferedBy, ref status );
				output.OwnedBy = MapOrganizationReferenceGuids( "Assessment.OwnedBy", input.OwnedBy, ref status );
				if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
				{
					output.OwningAgentUid = output.OwnedBy[0];
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
					if ( new AssessmentManager().AddReferenceAssessment( output, ref status ) > 0 )
					{
						entityUid = output.RowId;
						entityRefId = output.Id;
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBaseEntityAsAssessment(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}
			return entityRefId;
		}
		private int ResolveBaseEntityAsLopp( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			WPM.LearningOpportunityProfile output = new WPM.LearningOpportunityProfile();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			//20-12-15 mp - now a subject webpage may not be required, so we would need something else OR ALWAYS DELETE
			
			try
			{
				output = LearningOpportunityManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
				if ( output != null && output.Id > 0 )
				{
					//20-12-08 mp - combine mapping

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
				output.TeachesCompetencies = MapCAOListToCAOProfileList( input.Teaches, true );
				output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );
				output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
				output.Addresses = FormatAvailableAtAddresses( input.AvailableAt, ref status );
				//
				output.CodedNotation = input.CodedNotation;
				//new
				//output.QVCreditValueList = HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue" );
				output.CreditValue = HandleValueProfileList( input.CreditValue, "LearningOpportunity.CreditValue" );
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );
				//output.CreditValueList = HandleValueProfileList( input.CreditValue, "LearningOpportunity.CreditValue" );
				//get rid of this:
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
				//old

				//output.CreditValueList = HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue", true );
				//if ( output.CreditValueList != null && output.CreditValueList.Any() )
				//	output.CreditValue = output.CreditValueList[ 0 ];
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
					output.IdentifierJSON = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
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
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBaseEntityAsLopp(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
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
					if ( output.AssertedByAgent != null && output.AssertedByAgent.Count > 0 )
					{
						//just handling one
						output.AssertedByAgentUid = output.AssertedByAgent[0];
						output.AssertedByAgent = null;
					}
				}

				try
				{
					//temp handle where older resources had experience published as a string and not a languageMap.
					if ( input.Experience != null )
					{
						if ( input.Experience.GetType() == typeof( string ) )
						{
							output.Experience = input.Experience.ToString();
						}
						else
						{
							//language map
							var request = JsonConvert.DeserializeObject<MJ.LanguageMap>( input.Experience.ToString() );
							output.Experience = HandleLanguageMap( request, output, "Experience", true );
						}
					}
				} catch ( Exception ex )
                {
					//ignore for now. using try so can continue
					LoggingHelper.DoTrace( 5, string.Format( "MappingHelperV3.FormatConditionProfile. Experience exception CTID: {0}, ex: {1}", status.Ctid, ex.Message ) );
                }
				output.MinimumAge = input.MinimumAge;
				output.YearsOfExperience = input.YearsOfExperience;
				output.Weight = input.Weight;

				//output.CreditValue = HandleQuantitiveValue( input.CreditValue, "ConditionProfile.CreditHourType" );
				//output.CreditValueList2 = HandleValueProfileListToQVList( input.CreditValue, "ConditionProfile.CreditValue" );
				//TODO - chg to output to ValueProfile
				output.CreditValueList = HandleValueProfileList( input.CreditValue, "ConditionProfile.CreditValue" );
				//21-03-23 starting to use CreditValueJson 
				if ( output.CreditValueList != null && output.CreditValueList.Count > 0 )
					output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValueList, MappingHelperV3.GetJsonSettings( false ) );
				else
					output.CreditValueJson = "";
				output.CreditUnitTypeDescription = HandleLanguageMap( input.CreditUnitTypeDescription, output, "CreditUnitTypeDescription" );

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
				//
				output.CostManifestIds = MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, ref status );
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

				//22-08-22 - using true, but the competency can be from a collection, is the latter significant?
				output.TargetCompetencies = MapCAOListToCAOProfileList( input.TargetCompetency, true );
				list.Add( output );
			}

			return list;
		}

		public List<WPM.ConditionProfile> FormatConditionProfile( List<MJ.ConditionProfileOLD> profiles, ref SaveStatus status )
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
                        output.SubmissionOf = MapToTextValueProfile( ( List<string> ) input.SubmissionOf );

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
                    if ( output.AssertedByAgent != null && output.AssertedByAgent.Count > 0 )
                    {
                        //just handling one
                        output.AssertedByAgentUid = output.AssertedByAgent[0];
                        output.AssertedByAgent = null;
                    }
                }

                try
                {
                    //temp handle where older resources had experience published as a string and not a languageMap.
                    if ( input.Experience != null )
                    {
                        if ( input.Experience.GetType() == typeof( string ) )
                        {
                            output.Experience = input.Experience.ToString();
                        }
                        else
                        {
                            //language map
                            var request = JsonConvert.DeserializeObject<MJ.LanguageMap>( input.Experience.ToString() );
                            output.Experience = HandleLanguageMap( request, output, "Experience", true );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    //ignore for now. using try so can continue
                    LoggingHelper.DoTrace( 5, string.Format( "MappingHelperV3.FormatConditionProfile. Experience exception CTID: {0}, ex: {1}", status.Ctid, ex.Message ) );
                }
                output.MinimumAge = input.MinimumAge;
                output.YearsOfExperience = input.YearsOfExperience;
                output.Weight = input.Weight;

                //output.CreditValue = HandleQuantitiveValue( input.CreditValue, "ConditionProfile.CreditHourType" );
                
                //TODO - chg to output to ValueProfile
                output.CreditValueList = HandleQVListToValueProfileList( input.CreditValue, "ConditionProfile.CreditValue" );
				//21-03-23 starting to use CreditValueJson 
				output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValueList, MappingHelperV3.GetJsonSettings(false) );
				output.CreditUnitTypeDescription = HandleLanguageMap( input.CreditUnitTypeDescription, output, "CreditUnitTypeDescription" );

                //
                if ( input.AlternativeCondition != null & input.AlternativeCondition.Count > 0 )
                    output.AlternativeCondition = FormatConditionProfile( input.AlternativeCondition, ref status );

                output.EstimatedCosts = FormatCosts( input.EstimatedCost, ref status );
                //
                output.CostManifestIds = MapEntityReferences( input.CommonCosts, CodesManager.ENTITY_TYPE_COST_MANIFEST, CodesManager.ENTITY_TYPE_CONDITION_PROFILE, ref status );
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

                //22-08-22 - using true, but the competency can be from a collection, is the latter significant?
                output.TargetCompetencies = MapCAOListToCAOProfileList( input.TargetCompetency, true );

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
			//21-03-27 mp - moved expandCosts out of CostProfile to separate and prepare for storing as JSON
			//return WPM.CostProfileMerged.ExpandCosts( list );
			//
			return ExpandCosts( list );
		}
		public static List<WPM.CostProfile> ExpandCosts( List<WPM.CostProfileMerged> input )
		{
			var result = new List<WPM.CostProfile>();

			//First expand each into its own CostProfile with one CostItem
			var holder = new List<WPM.CostProfile>();
			foreach ( var merged in input )
			{
				//Create cost profile
				var cost = new WPM.CostProfile()
				{
					ProfileName = merged.Name,
					Description = merged.Description,
					Jurisdiction = merged.Jurisdiction,
					//StartTime = merged.StartTime,	//why did we have start and end time?
					//EndTime = merged.EndTime,
					StartDate = merged.StartDate,
					EndDate = merged.EndDate,
					CostDetails = merged.CostDetails,
					Currency = merged.Currency,
					CurrencySymbol = merged.CurrencySymbol,
					Condition = merged.Condition,
					Items = new List<WPM.CostProfileItem>()
				};
				//If there's any data for a cost item, create one
				if ( merged.Price > 0 ||
					!string.IsNullOrWhiteSpace( merged.PaymentPattern ) ||
					merged.AudienceType.Items.Count() > 0 ||
					merged.CostType.Items.Count() > 0 ||
					merged.ResidencyType.Items.Count() > 0
					)
				{
					cost.Items.Add( new WPM.CostProfileItem()
					{
						AudienceType = merged.AudienceType,
						DirectCostType = merged.CostType,
						PaymentPattern = merged.PaymentPattern,
						Price = merged.Price,
						ResidencyType = merged.ResidencyType
					} );
				}
				holder.Add( cost );
			}

			//Remove duplicates and hope that pass-by-reference issues don't cause trouble
			while ( holder.Count() > 0 )
			{
				//Take the first item in holder and set it aside
				var currentItem = holder.FirstOrDefault();
				//Remove it from the holder list so it doesn't get included in the LINQ query results on the next line
				holder.Remove( currentItem );
				//Find any other items in the holder list that match the item we just took out
				var matches = holder.Where( m =>
					m.ProfileName == currentItem.ProfileName &&
					m.Description == currentItem.Description &&
					m.CostDetails == currentItem.CostDetails &&
					m.Currency == currentItem.Currency &&
					m.CurrencySymbol == currentItem.CurrencySymbol
				).ToList();
				//For each matching item...
				foreach ( var item in matches )
				{
					//Take its cost profile items (if it has any) and add them to the cost profile we set aside
					currentItem.Items = currentItem.Items.Concat( item.Items ).ToList();
					//Remove the item from the holder so it doesn't get detected again, and so that we eventually get out of this "while" loop
					holder.Remove( item );
				}
				//Now that currentItem has all of the cost profile items from all of its matches, add it to the result
				result.Add( currentItem );
			}

			return result;
		}
		//
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
					SubjectWebpage = input.SubjectWebpage,
					HolderMustAuthorize = input.HolderMustAuthorize
				};
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
				profile.VerificationMethodDescription = HandleLanguageMap( input.VerificationMethodDescription, profile, "VerificationMethodDescription", true );

				//VerificationService is hidden in the publisher!
				profile.VerificationService = MapListToString( input.VerificationService );
				profile.OfferedByAgentUid = MapOrganizationReferencesToGuid( "VerificationServiceProfile.OfferedByAgentUid", input.OfferedBy, ref status );
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					profile.TargetCredentialIds = MapEntityReferences( "VerificationServiceProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				profile.VerificationDirectory = MapListToString( input.VerificationDirectory );
				profile.ClaimType = MapCAOListToEnumermation( input.VerifiedClaimType );
				profile.EstimatedCost = FormatCosts( input.EstimatedCost, ref status );
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
				profile.OfferedIn = MapToJurisdiction( input.OfferedIn, ref status );

				list.Add( profile );
			}

			return list;
		}
		#endregion


		public string FormatFinderResourcesURL( string url )
		{
			var ctid = ResolutionServices.ExtractCtid( url.Trim() );

			var finderUrl = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" ) + "resources/" + ctid;

			return finderUrl;
		}
		public decimal StringtoDecimal( string value )
		{
			decimal output = 0m;
			if ( string.IsNullOrWhiteSpace( value ) )
				return output;

			decimal.TryParse( value, out output );
			return output;
		}
		//public static JsonSerializerSettings GetJsonSettings()
		//{
		//	var settings = new JsonSerializerSettings()
		//	{
		//		NullValueHandling = NullValueHandling.Ignore,
		//		DefaultValueHandling = DefaultValueHandling.Ignore,
		//		ContractResolver = new EmptyNullResolver(),
		//		Formatting = Formatting.Indented,
		//		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		//	};

		//	return settings;
		//}
		public static JsonSerializerSettings GetJsonSettings( bool doingFormating=false)
		{
			var settings = new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ContractResolver = new EmptyNullResolver(),
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				Formatting = doingFormating ? Formatting.Indented : Formatting.None
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
