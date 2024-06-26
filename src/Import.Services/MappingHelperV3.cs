using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nest;

//using Nest;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using workIT.Factories;
using workIT.Models;
using workIT.Models.Common;
using workIT.Services;
using workIT.Utilities;

using BNode = RA.Models.JsonV2.BlankNode;
using InputAddress = RA.Models.JsonV2.Place;
using MC = workIT.Models.Common;
using MCQ = workIT.Models.QData;
using MJ = RA.Models.JsonV2;
using MJQ = RA.Models.JsonV2.QData;
using WPM = workIT.Models.ProfileModels;
using Import.Services.RegistryModels;
using System.Text.RegularExpressions;
//using System.Data.Entity.Validation;
//using System.Diagnostics;

namespace Import.Services
{
	public class MappingHelperV3
	{
		#region Properties
		static readonly string thisClassName = "Import.Services.MappingHelperV3";
		public List<BNode> entityBlankNodes = new List<BNode>();
		public List<BNodeWrapper> ReferenceObjects = new List<BNodeWrapper>();
		public string DefaultLanguage = "en";
		public List<MC.EntityLanguageMap> LanguageMaps = new List<MC.EntityLanguageMap>();
		public MC.EntityLanguageMap LastEntityLanguageMap = new MC.EntityLanguageMap();
		public string lastLanguageMapString = "";
		public string lastLanguageMapListString = "";
		public MC.BaseObject currentBaseObject = new MC.BaseObject();
		//TODO  should change this to CurrentPrimary to make more generic
		public Guid CurrentOwningAgentUid;
		public Guid CurrentPublishedByAgentUid;
		public int CurrentEntityTypeId = 0;
		public string CurrentEntityCTID = "";
		public string CurrentEntityName = "";
		public static string credentialRegistryUrl = UtilityManager.GetAppKeyValue( "credentialRegistryUrl" );

		//TODO
		public List<TopLevelObject> ResourcesToIndex = new List<TopLevelObject>();
		#endregion
		public MappingHelperV3()
		{
		}
		public MappingHelperV3( int entityTypeId )
		{
			CurrentEntityTypeId = entityTypeId;
			entityBlankNodes = new List<BNode>();
		}

		#region handle blank nodes
		public BNodeWrapper FormatBlankNode( string blankNode )
		{
			if ( string.IsNullOrWhiteSpace( blankNode ) )
				return null;
			var resourceOutline = RegistryServices.GetGraphMainResource( blankNode );
			var bnWrapper = new BNodeWrapper()
			{
				BNodeId = resourceOutline.CtdlId,
				Type = resourceOutline.Type,
				Resource = blankNode		//??just the string then deserialize when needed?
			};
			bnWrapper.EntityTypeId = GetEntityTypeId( bnWrapper.Type );
			//or just add to ReferenceObjects
			if ( ReferenceObjects == null )
				ReferenceObjects = new List<BNodeWrapper>();

			ReferenceObjects.Add(bnWrapper );

			return bnWrapper;
		}
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
			int entityTypeId = GetEntityTypeId( node.Type );
			//TODO - for lopps, should we use just 7?
			//	it needs to be saved as course though, so not here
			//if ( entityTypeId == 37 || entityTypeId == 36 )
			//	entityTypeId = 7;
			//
			//one additional check
			if ( node.Type.ToLower() == "ceterms:qacredentialorganization" )
				isQAOrgType = true;
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
		/// <summary>
		/// handle a property defined as an object but should be a string or language map list
		/// </summary>
		/// <param name="input"></param>
		/// <param name="bo"></param>
		/// <param name="property"></param>
		/// <param name="savingLastEntityMap"></param>
		/// <param name="languageCode"></param>
		/// <returns></returns>
		public List<WPM.TextValueProfile> MapToTextValueProfile( object input, MC.BaseObject bo, string property, bool savingLastEntityMap = true, string languageCode = "en" )
		{
			List<WPM.TextValueProfile> output = new List<WPM.TextValueProfile>();
			lastLanguageMapListString = "";
			if ( input == null )
			{
				return output;
			}

			try
			{
				if ( input.GetType() == typeof( string ) )
				{
					output.Add( new WPM.TextValueProfile()
					{
						TextValue = input.ToString(),
					} );
				}
				else
				{
					//language map
					var lmapList = JsonConvert.DeserializeObject<MJ.LanguageMapList>( input.ToString() );
					return MapToTextValueProfile( lmapList, bo, property, savingLastEntityMap, languageCode );
				}
			} catch (Exception ex )
			{
				LoggingHelper.DoTrace( 1, $"{thisClassName}.MapToTextValue (object input). Property: {property}. Parent: {bo.CTID} " );
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
			output.Value = input.Value != null ? ( decimal ) input.Value : 0;
			output.MinValue = input.MinValue != null ? ( decimal ) input.MinValue : 0;
			output.MaxValue = input.MaxValue != null ? ( decimal ) input.MaxValue : 0;
			output.Percentage = input.Percentage != null ? ( decimal ) input.Percentage : 0;
			output.Description = HandleLanguageMap( input.Description, property );
			//need to distinguish QV that uses creditvalue
			if ( isCreditValue )
			{
				output.CreditUnitType = MapCAOToEnumermation( input.UnitText );
				if ( output.CreditUnitType != null && output.CreditUnitType.HasItems() )
				{
					output.UnitText = output.CreditUnitType.GetFirstItem().Name;
				}
			} else
			{
				output.UnitText = ( input.UnitText ?? new MJ.CredentialAlignmentObject() ).TargetNodeName.ToString();
			}

			return output;
		}

		#endregion
		#region ValueProfile

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
				profile.Value = input.Value != null && input.Value > 0 ? ( decimal ) input.Value : 0;
				profile.MinValue = input.MinValue != null ? ( decimal ) input.MinValue : 0;
				profile.MaxValue = input.MaxValue != null ? ( decimal ) input.MaxValue : 0;
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
			output.Value = input.Value != null ? ( decimal ) input.Value : 0;
			output.MinValue = input.MinValue != null ? ( decimal ) input.MinValue : 0;
			output.MaxValue = input.MaxValue != null ? ( decimal ) input.MaxValue : 0;
			output.Percentage = input.Percentage != null ? ( decimal ) input.Percentage : 0;
			output.Description = HandleLanguageMap( input.Description, property );
			//
			output.CreditUnitType = MapCAOListToEnumermation( input.CreditUnitTypeOLD );
			output.CreditLevelType = MapCAOListToEnumermation( input.CreditLevelTypeOLD );

			//TODO
			//output.CreditUnitType = MapConceptURIListToList( input.CreditUnitType );
			//output.CreditLevelType = MapConceptURIListToList( input.CreditLevelType );

			//why???
			if ( output.CreditLevelType != null && output.CreditLevelType.HasItems() )
				output.CreditLevelType.Name = "Credit Level Type";
			else
				output.CreditLevelType = null;

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
			if ( property.Count > 1 )
			{

			}

			var input = property[ 0 ];

			if ( input.IdentifierTypeName != null )
				return HandleLanguageMap( input.IdentifierTypeName, "IdentifierTypeName", false );
			//else if ( !string.IsNullOrWhiteSpace( property[ 0 ].IdentifierTypeName ) )
			//	return property[ 0 ].IdentifierTypeName;
			else if ( !string.IsNullOrWhiteSpace( input.IdentifierValueCode ) )
				return input.IdentifierValueCode;
			else
				return "";
		}

		public List<WPM.Entity_IdentifierValue> MapIdentifierValueList( List<MJ.IdentifierValue> property )
		{
			var list = new List<WPM.Entity_IdentifierValue>();
			if ( property == null || property.Count == 0 )
				return list;
			var iv = new WPM.Entity_IdentifierValue();
			var startingDateTime = DateTime.Now;
			foreach ( var item in property )
			{
				iv = new WPM.Entity_IdentifierValue()
				{
					IdentifierType = item.IdentifierType ?? "",
					IdentifierValueCode = item.IdentifierValueCode,
					IdentifierTypeName = HandleLanguageMap( item.IdentifierTypeName, "IdentifierTypeName", false ),
					//add the date now as a means of retaining order from registry. Hmm in a lot of cases, the datetime can be the same
					Created = startingDateTime.AddMilliseconds(1),
				};
				list.Add( iv );
			}
			//just in case
			//the display (like for CO WIOA) could do a group by, but shouldn't need anything else here?
			list = list.OrderBy( s => s.IdentifierType ).ThenBy( s => s.Created ).ToList();

			return list;
		}
		/// <summary>
		/// Use this method when storing the Identifiers as JSON on a base record. 
		/// Note: this should only be done for fairly simple uses, where only one or a few identifiers
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>

		public List<MC.IdentifierValue> MapIdentifierValueListInternal( List<MJ.IdentifierValue> property )
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

		public MC.Enumeration MapStringListToEnumeration( List<String> input, bool isForConceptScheme = true )
		{
			//
			//TBD = do we need anything for emumeration, or just items?
			MC.Enumeration output = new workIT.Models.Common.Enumeration();
			if ( input == null || input.Count == 0 )
				return output;

			foreach ( var item in input )
			{
				if ( item == null )
					continue;
				//temp workaround, these would have to be handled very differently (i.e. retrieving from DB)
				//could add to the Enumeration

				if ( item != null && ( item != null || !string.IsNullOrEmpty( item ) ) )
				{
					var ei = new workIT.Models.Common.EnumeratedItem();
					//do a direct look up of the PropertyValueId using TargetNode. ex: creditUnit:DegreeCredit
					var codeItem = CodesManager.GetPropertyBySchema( item );
					if ( codeItem != null && codeItem.Id > 0 )
					{
						ei.Id = codeItem.Id;
						ei.Name = codeItem.Name;
						ei.Description = codeItem.Description;
						ei.SchemaName = codeItem.SchemaName;
						output.Name = codeItem.Category;
						output.Id = codeItem.CategoryId;
					}
					output.Items.Add( ei );

				}

			}
			return output;
		}

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
					//do a direct look up of the PropertyValueId using TargetNode. ex: creditUnit:DegreeCredit
					var codeItem = CodesManager.GetPropertyBySchema( item.TargetNode );
					if ( codeItem != null && codeItem.Id > 0 )
					{
						ei.Id = codeItem.Id;
						if ( isForConceptScheme )
						{
							output.Id = codeItem.CategoryId;
							output.Name = codeItem.Category;
						}
					}
					output.Items.Add( ei );
					
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
				return null;

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
				//NOTE: targetNodeName is not required! 24-03-29 mp - yes the name is required for occupations, etc. 
				if ( item != null && ( !string.IsNullOrEmpty( targetNodeName ) ) )
				{
					entity = new MC.CredentialAlignmentObjectProfile()
					{
						TargetNode = item.TargetNode ?? "",
						CodedNotation = item.CodedNotation ?? "",
						FrameworkName = HandleLanguageMap( item.FrameworkName, currentBaseObject, "FrameworkName" ),
						FrameworkName_Map = ConvertLanguageMap( item.FrameworkName ),
						Weight = item.Weight != null ? ( decimal ) item.Weight : 0
						//Weight = StringtoDecimal( item.Weight )
					};
					entity.TargetNodeCTID = ResolutionServices.ExtractCtid( item.TargetNode );

					//entity.TargetNodeName = HandleLanguageMap( item.ConvertLanguageMap( item.TargetNodeName ), currentBaseObject, "TargetNodeName", false );
					entity.TargetNodeName = targetNodeName;
					entity.TargetNodeName_Map = ConvertLanguageMap( item.TargetNodeName );
					entity.TargetNodeDescription = HandleLanguageMap( item.TargetNodeDescription, currentBaseObject, "TargetNodeDescription", false );
					entity.TargetNodeDescription_Map = ConvertLanguageMap( item.TargetNodeDescription );

					//won't know if url or registry uri, but likely non- registry.
					if ( !string.IsNullOrWhiteSpace( item.Framework ) )
					{
						var isRegistryURL = true;
						//TODO - need to handle ce-registry/... so communities as well!!
						if ( !IsCredentialRegistryURL( item.Framework ) )
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

		#region  Concepts (typically short URI)
		//
		public List<string> MapConceptURIListToList( object input, string property )
		{
			var output = new List<string>();
			if ( input == null )
				return null;

			if ( input.GetType() == typeof( string ) )
			{
				//??
				var text = input.ToString();
			}
			else if ( input.GetType() == typeof( List<string> ) )
			{
				//
				var list = input as List<string>;
				if ( list != null && list.Count() > 0 )
				{
					foreach ( var item in list )
					{
					}
				}

			}
			else if ( input.GetType() == typeof( Newtonsoft.Json.Linq.JArray ) )
			{
				var stringArray = ( Newtonsoft.Json.Linq.JArray ) input;
				var list = stringArray.ToObject<List<string>>();
				//
				if ( list != null && list.Count() > 0 )
				{
					foreach ( var item in list )
					{
					}
				}
			}
			else if ( input.GetType() == typeof( List<MJ.CredentialAlignmentObject> ) ) //possible? prob not
			{
				//
				var list = input as List<MJ.CredentialAlignmentObject>;
				if ( list != null && list.Count() > 0 )
				{
					foreach ( var item in list )
					{
					}
				}
			}
			else
			{
				//what would be the default
			}
			//foreach ( var item in input )
			//{
			//	if ( item == null )
			//		continue;
			//	//look up 
			//	string nodeName = "";
			//		//HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );

			//	if ( !string.IsNullOrEmpty( nodeName ) )
			//	{
			//		output.Add( nodeName );
			//	}
			//}
			return output;
		}
		/// <summary>
		/// Handling properties that point to concepts of an unknown type/concept scheme
		/// TODO - handle blank nodes
		/// ALSO - what/how to store. Just the short uri?
		/// </summary>
		/// <param name="input"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public List<string> MapGenericConceptURIListToList( List<string> input, string property, ref SaveStatus status )
		{
			var output = new List<string>();
			if ( input == null || input.Count == 0 )
				return output;
			//not sure if isResolved is necessary
			bool isResolved = false;
			foreach ( var item in input )
			{
				if ( item == null )
					continue;
				if ( item.StartsWith( "_:" ) )
				{
					var node = GetBlankNode( item );
					//if type present,can use
					//return ResolveOrgBlankNodeToGuid( property, node, ref status, ref isResolved );
				}
				//look up 
				string nodeName = "";
				var concept = CodesManager.GetPropertyBySchema( item );

				if ( !string.IsNullOrEmpty( nodeName ) )
				{
					output.Add( nodeName );
				}
			}
			return output;
		}
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
			if ( addresses == null || addresses.Count == 0 )
				return list;

			MC.ContactPoint cp = new MC.ContactPoint();

			foreach ( var item in addresses )
			{
				Address output = MapAddress( item );

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
					gc.RelatedEntityId = 0;
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
							gc.RelatedEntityId = 0;
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
					gc.Latitude = ( double ) input.Latitude;
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
				PostOfficeBoxNumber = input.PostOfficeBoxNumber,
				Latitude = input.Latitude == null ? 0 : ( double ) input.Latitude,
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
				if ( output.AddressRegion != null && output.AddressRegion.Length == 2 && !string.IsNullOrWhiteSpace( output.AddressCountry ) )
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
			if ( input.Identifier != null && input.Identifier.Count() > 0 )
			{
				output.IdentifierOLD = MapIdentifierValueList( input.Identifier );
				output.IdentifierJson = JsonConvert.SerializeObject( output.IdentifierOLD, MappingHelperV3.GetJsonSettings() );

				output.Identifier = MapIdentifierValueListInternal( input.Identifier );

			}

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
		public List<MC.AggregateDataProfile> FormatAggregateDataProfile( string parentCTID, List<MJ.AggregateDataProfile> profiles, ref SaveStatus status, ref List<string> ctidList )
		{
			if ( profiles == null || profiles.Count == 0 )
				return null;
			//ref bool hasDataSetProfiles,
			//hasDataSetProfiles = false;
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
				profile.FacultyToStudentRatio = input.FacultyToStudentRatio;
				profile.Source = input.Source;

				profile.Description = HandleLanguageMap( input.Description, "Description", true );
				profile.DemographicInformation = HandleLanguageMap( input.DemographicInformation, "DemographicInformation", true );
				//
				if ( input.JobsObtained != null && input.JobsObtained.Any() )
				{
					profile.JobsObtained = HandleQuantitiveValueList( input.JobsObtained, "AggregateDataProfile.JobsObtained", false );
				}
				//handle: RelevantDataSet
				//TODO - handle previous dsps not in this import - no this is partly a separate one time fix.
				//23-02-12 mp - need to save the CTID, not the Id. so probably don't have to do the ResolveEntityRegistryAtId
				//			- sigh. Sill have need to do the add pending later then
				//			- next: updated trigger to just do virtual delete
				profile.RelevantDataSetList = MapEntityReferences( "AggregateData.RelevantDataSet", input.RelevantDataSet, CodesManager.ENTITY_TYPE_DATASET_PROFILE, ref status, ref ctidList );
				//if ( profile.RelevantDataSetList != null && profile.RelevantDataSetList.Any() )
				//	hasDataSetProfiles = true;

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
						if ( dspi == null || string.IsNullOrWhiteSpace( dspi.CTID ) )
						{
							status.AddError( string.Format( "Error: Unable to find the DataSetProfile for HoldersProfile (HP.CTID: '{0}') using dataSetProfile CTID: '{1}'", input.CTID, ctid ) );
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
			if ( output.DataProviderUID == null || output.DataProviderUID == Guid.Empty )
			{
				output.DataProviderUID = CurrentOwningAgentUid;
			}
			//about can be cred, asmt, or lopp. These could be references
			//make sure these are added to entity.cache immediately
			output.AboutUids = MapEntityReferenceGuids( "DataSetProfile.About", input.About, 0, ref status );
			//not sure we need to do RelevantDataSetFor, as should have already been established
			//22-06-14 mp - if About is empty, then RelevantDataSetFor will be important. It should be obsolete, add handling for importing old data.
			//22-09-28 mparsons	- effectively obsolete outside of HoldersProfile, EarningsProfile, EmploymentOutlook and the latter are moving to be obsolete
			//23-02-14 mp - however, for old data, it may still be present
			if ( ( input.About == null || input.About.Count == 0 ) && ( input.RelevantDataSetFor != null && input.RelevantDataSetFor.Count > 0 ) )
			{
				output.AboutUids = MapEntityReferenceGuids( "DataSetProfile.About", input.RelevantDataSetFor, 0, ref status );
			}

			output.AdministrationProcess = FormatProcessProfile( input.AdministrationProcess, ref status );
			if ( input.DistributionFile != null && input.DistributionFile.Any() )
			{
				foreach ( var item in input.DistributionFile )
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

					output.DataSetTimePeriodJson = JsonConvert.SerializeObject( output.DataSetTimePeriod, MappingHelperV3.GetJsonSettings() );

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
			if ( input == null )
				return null;

			LoggingHelper.DoTrace( 7, string.Format( "FormatDataSetTimeFrame. parentCTID: {0}, Name: {1}, ", parentCTID, input.Name ) );

			var output = new MCQ.DataSetTimeFrame()
			{
				Name = HandleLanguageMap( input.Name, "Name" ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				StartDate = input.StartDate ?? "",
				EndDate = input.EndDate ?? "",

			};
			//to avoid more entity types, and/or codes.PropertyCategories, just store list of names (think this should be single
			//output.DataSourceCoverageType = MapCAOListToEnumermation( input.DataSourceCoverageType );
			//output.DataSourceCoverageTypeList = MapCAOListToList( input.DataSourceCoverageTypeOLD );
			//TODO handle change from CAO
			output.DataSourceCoverageTypeList = MapConceptURIListToList( input.DataSourceCoverageType, "DataSourceCoverageType" );

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
			if ( input == null )
				return null;
			MCQ.DataProfileJson qdSummary = new MCQ.DataProfileJson();
			LoggingHelper.DoTrace( 7, "FormatDataProfiles. parentCTID: " + parentCTID );
			var output = new MCQ.DataProfile()
			{
				//bnID = input.CtdlId,
				AdministrativeRecordType = MapCAOToEnumermation( input.AdministrativeRecordType ),
				Description = HandleLanguageMap( input.Description, "Description" ),
				IncomeDeterminationType = MapCAOToEnumermation( input.IncomeDeterminationType ),
			};
			//??why doing twice and not storing it?
			//output.AdministrativeRecordTypeList = MapCAOToList( input.AdministrativeRecordType );
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
                //
                output.DataProfileAttributes.EmploymentOutlook = HandleQuantitiveValueList( input.EmploymentOutlook, "DataProfile.EmploymentOutlook", "EmploymentOutlook", ref qdSummary, false );
                //
                output.DataProfileAttributes.FacultyToStudentRatio = input.FacultyToStudentRatio;


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
				output.DataProfileAttributes.SubjectExcluded = HandleQuantitiveValueList( input.SubjectExcluded, "DataProfile.SubjectExcluded", "SubjectExcluded", ref qdSummary, false );

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

			foreach ( var input in profiles )
			{
				//what are the minimum properties
				if ( input == null ||
					( input.Value == 0 && input.MinValue == 0 && input.MaxValue == 0 ) )
				{
					continue;
				}
				var ma = new MC.MonetaryAmount()
				{
					Description = HandleLanguageMap( input.Description, "Description" ),
					Value = input.Value != null ? ( decimal ) input.Value : 0,
					MaxValue = input.MaxValue != null ? ( decimal ) input.MaxValue : 0,
					MinValue = input.MinValue != null ? ( decimal ) input.MinValue : 0,
					Currency = input.Currency,
					UnitText = input.UnitText,
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
				if ( item == null ||
					( item.Median == 0 && item.Percentile10 == 0 && item.Percentile25 == 0 && item.Percentile75 == 0 && item.Percentile90 == 0 ) )
				{
					continue;
				}
				var ma = new MC.MonetaryAmountDistribution()
				{
					Currency = item.Currency,
					Median = item.Median != null ? ( decimal ) item.Median : 0,
					Percentile10 = item.Percentile10 != null ? ( decimal ) item.Percentile10 : 0,
					Percentile25 = item.Percentile25 != null ? ( decimal ) item.Percentile10 : 0,
					Percentile75 = item.Percentile75 != null ? ( decimal ) item.Percentile75 : 0,
					Percentile90 = item.Percentile90 != null ? ( decimal ) item.Percentile90 : 0
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
					SubjectValue = HandleQuantitiveValueList( input.SubjectValue, "SubjectProfile.SubjectValue", false )
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
				if ( input.JobsObtained != null && input.JobsObtained.Any() )
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

			DateTime newDate = DateTime.Now;

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
			string inputAtId = "";
			//var or = new List<RA.Models.Input.OrganizationReference>();
			if ( input == null || input.Count < 1 )
				return orgRefs;

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;

				orgRef = MapOrganizationReferenceGuids( property, target, ref status );

				if ( BaseFactory.IsGuidValid( orgRef ) )
					orgRefs.Add( orgRef );
			}

			return orgRefs;
		}

		public Guid MapOrganizationReferenceGuids( string property, string target, ref SaveStatus status )
		{
			//not sure if isResolved is necessary
			bool isResolved = false;
			Guid orgRef = new Guid();
			List<Guid> orgRefs = new List<Guid>();
			string inputAtId = "";

				if ( string.IsNullOrWhiteSpace( target ) )
					return orgRef;

				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					inputAtId = target;

					if ( !IsCredentialRegistryURL( target ) )
					{

					}
					else
					{
						if ( inputAtId != inputAtId.ToLower() )
						{
							status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, inputAtId ) );
						}
					}

					orgRef = ResolveOrgRegistryAtIdToGuid( inputAtId, ref status, ref isResolved );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					var node = GetBlankNode( target );
					orgRef = ResolveOrgBlankNodeToGuid( property, node, ref status, ref isResolved );
				}
				else
				{
					//unexpected
					status.AddError( string.Format( "MapOrganizationReferenceGuids: Unhandled target  format found: {0} for property: {1}.", target, property ) );
				}

				//if ( BaseFactory.IsGuidValid( orgRef ) )
				//	orgRefs.Add( orgRef );
			

			return orgRef;
		}

		/// <summary>
		/// Check if the org related uri has already been resolved.
		/// </summary>
		/// <param name="inputAtId"></param>
		/// <param name="status"></param>
		/// <param name="isResolved"></param>
		/// <returns></returns>
		private Guid ResolveOrgRegistryAtIdToGuid( string inputAtId, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityRef = new Guid();
			if ( !string.IsNullOrWhiteSpace( inputAtId ) )
			{
				entityRef = ResolutionServices.ResolveOrgByRegistryAtId( inputAtId, ref status, ref isResolved );
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
			int orgId = ResolveBlankNodeAsOrganization( property, input, 2, ref entityUid, ref status );

			return entityUid;

		}

		//mapping to an int
		public List<int> MapOrganizationReferenceToInteger( string property, List<string> input, ref SaveStatus status )
		{
			//not sure if isResolved is necessary
			bool isResolved = false;
			var orgRef = 0;
			var orgRefs = new List<int>();
			string inputAtId = "";
			//
			if ( input == null || input.Count < 1 )
				return null;

			//just take first one
			foreach ( var target in input )
			{
				if ( string.IsNullOrWhiteSpace( target ) )
					continue;

				//determine if just Id, or base
				if ( target.StartsWith( "http" ) )
				{
					inputAtId = target;
					if ( !IsCredentialRegistryURL( target ) )
					{

					}
					else
					{
						if ( inputAtId != inputAtId.ToLower() )
						{
							status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, inputAtId ) );
						}
					}

					orgRef = ResolveOrgRegistryAtId(property, inputAtId, ref status, ref isResolved );
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					var node = GetBlankNode( target );
					orgRef = ResolveOrgBlankNode( property, node, ref status, ref isResolved );
				}
				else
				{
					//unexpected
					status.AddError( string.Format( "MapOrganizationReference: Unhandled target  format found: {0} for property: {1}.", target, property ) );
				}

				if ( orgRef > 0 )
					orgRefs.Add( orgRef );
			}

			return orgRefs;
		}

		/// <summary>
		/// Check if the org related uri has already been resolved.
		/// </summary>
		/// <param name="inputAtId"></param>
		/// <param name="status"></param>
		/// <param name="isResolved"></param>
		/// <returns></returns>
		private int ResolveOrgRegistryAtId( string property, string inputAtId, ref SaveStatus status, ref bool isResolved )
		{
			var entityRef = 0;
			if ( !string.IsNullOrWhiteSpace( inputAtId ) )
			{
				entityRef = ResolutionServices.ResolveEntityByRegistryAtId( property, inputAtId, 2, ref status, ref isResolved );
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
		private int ResolveOrgBlankNode( string property, BNode input, ref SaveStatus status, ref bool isResolved )
		{
			Guid entityUid = new Guid();

			if ( input == null )
				return 0;
			//20-08-23 update to reuse 'duplicate' method
			int orgId = ResolveBlankNodeAsOrganization( property, input, 2, ref entityUid, ref status );

			return orgId;

		}

		#endregion
		public void MapOrganizationPublishedBy( MC.TopLevelObject output, ref SaveStatus status )
		{
			if ( string.IsNullOrWhiteSpace( status.DocumentPublishedBy ) )
			{
				//unlikely, but return? or check for previous?
				//if ( output.Id > 0 )
				//            {
				//                if ( output.OrganizationRole != null && output.OrganizationRole.Any() )
				//                {
				//                    var publishedByList = output.OrganizationRole.Where( s => s.RoleTypeId == 30 ).ToList();
				//                    if ( publishedByList != null && publishedByList.Any() )
				//                    {
				//                        var pby = publishedByList[0].ActingAgentUid;
				//                        output.PublishedBy = new List<Guid>() { publishedByList[0].ActingAgentUid };
				//                    }
				//                }
				//            }
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
				//if publisher not imported yet (because a TPP may not normally publish itself), all publishee stuff will be orphaned
				var entityUid = Guid.NewGuid();
				var statusMsg = "";
				if ( !string.IsNullOrWhiteSpace( status.ResourceURL ) )
				{
					var resPos = status.ResourceURL.IndexOf( "/resources/" );
					swp = status.ResourceURL.Substring( 0, ( resPos + "/resources/".Length ) ) + status.DocumentPublishedBy;

				} else
				{
					var registryURL = UtilityManager.GetAppKeyValue( "credentialRegistryResource" );
					status.Community = status.Community ?? UtilityManager.GetAppKeyValue( "defaultCommunity" );
					//we have CTID, so make up a URL
					swp = string.Format( registryURL, status.Community, status.DocumentPublishedBy );
					status.ResourceURL = swp;
				}
				int orgId = new OrganizationManager().AddPendingRecord( entityUid, status.DocumentPublishedBy, swp, ref status );
				if ( status.DocumentPublishedBy != status.DocumentOwnedBy )
					output.PublishedByThirdPartyOrganizationId = porg.Id;
				output.PublishedBy = new List<Guid>() { entityUid };
			}
		}


		#region  Entities
		/// <summary>
		/// Get the EntityTypeId for the provided type.
		/// TODO - this is not good to have to update this method, should use the code table.
		/// Confirm if this is typically just the top level classes (i.e. not conditionProfile, etc.)
		/// </summary>
		/// <param name="entityType"></param>
		/// <returns></returns>
		public static int GetEntityTypeId( string entityType )
		{
			int entityTypeId = 0;
			if ( string.IsNullOrWhiteSpace( entityType ) )
				return entityTypeId;
			//or maybe just check credential. The action types will not be in the code.entity or codes.propertyValue tables
			//OK added a lookup for GetCredentialingActionType
			entityTypeId = CodesManager.Codes_GetEntityTypeId( entityType );
			if ( entityTypeId  > 0)
				return entityTypeId;	

			//OR just remove ceterms etc, to reduce number of checks
			entityType = entityType.Replace( "ceterms:", "" );

			switch ( entityType.ToLower() )
			{

				//NOTE: don't have to maintain credentials here, now that using the above look up
				case "credential":          //leave just in case
				case "academiccertificate":
				case "apprenticeshipcertificate":
				case "associatedegree":
				case "associateofappliedartsdegree":
				case "associateofappliedsciencedegree":
				case "associateofartsdegree":
				case "associateofsciencedegree":
				case "bachelordegree":
				case "bachelorofartsdegree":
				case "bachelorofsciencedegree":
				case "badge":
				case "basictechnicalcertificate":
				case "certificate":
				case "certificateofcompletion":
				case "certificateofparticipation":
				case "certification":
				case "digitalbadge":
				case "diploma":
				case "doctoraldegree":
				case "generaleducationdevelopment":
				case "generaleducationlevel1certificate":
				case "generaleducationlevel2certificate":
				case "highereducationlevel1certificate":
				case "highereducationlevel2certificate":
				case "journeymancertificate":
				case "license":
				case "mastercertificate":
				case "masterdegree":
				case "masterofartsdegree":
				case "masterofsciencedegree":
				case "microcredential":
				case "openbadge":
				case "postbaccalaureatecertificate":
				case "postmastercertificate":
				case "preapprenticeshipcertificate":
				case "professionalcertificate":
				case "professionaldoctorate":
				case "proficiencycertificate":
				case "qualityassurancecredential":
				case "researchdoctorate":
				case "secondaryeducationcertificate":
				case "secondaryschooldiploma":
				case "specialistdegree":
				case "technicallevel1certificate":
				case "technicallevel2certificate":
				case "technicallevel3certificate":
				case "workbasedlearningcertificate":
					entityTypeId = 1;
					break;
					//23-12-19 mp - not sure we should use separate entityTypeId for orgs here? But am doing so for lopps?
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
				case "asn:progressionmodel":
				case "progressionmodel":
					entityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
					break;
				case "ceterms:scheduledoffering":
				case "scheduledoffering":
					entityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
					break;
				case "ceasn:competency":
				case "competency":
					entityTypeId = CodesManager.ENTITY_TYPE_COMPETENCY;
					break;
				case "ceterms:conditionmanifest":
				case "conditionmanifest":
					entityTypeId = 19;
					break;
				case "ceterms:costmanifest":
				case "costmanifest":
					entityTypeId = 20;
					break;
					//NOTE: need to add these to the codes table
				case "accreditaction":
				case "advancedstandingaction":
				case "approveaction":
				case "credentialingaction":
				case "offeraction":
				case "recognizeaction":
				case "regulateaction":
				case "renewaction":
				case "revokeaction":
				case "rightsaction":
					entityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
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
				case "ceterms:supportservice":
				case "supportservice":
					entityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
					break;
				case "ceterms:verificationserviceprofile":
				case "verificationserviceprofile":
					entityTypeId = CodesManager.ENTITY_TYPE_VERIFICATION_PROFILE;
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
				case "ceasn:rubric":
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
		/// <param name="property"></param>
		/// <param name="input"></param>
		/// <param name="entityTypeId">If zero, look up by ctid, or </param>
		/// <param name="status"></param>
		/// <param name="parentCTID"></param>
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

				entityRef= MapEntityReferenceGuid( property, target, entityTypeId, ref status, parentCTID );
			
				////
				if ( BaseFactory.IsGuidValid( entityRef ) )
					entityRefs.Add( entityRef );
				else
				{

				}
			}

			return entityRefs;
		}

		public Guid MapEntityReferenceGuid( string property, string input, int entityTypeId, ref SaveStatus status, string parentCTID = "" )
		{
			Guid entityRef = new Guid();
			MJ.EntityBase eb = new MJ.EntityBase();
			string registryAtId = "";
			if ( string.IsNullOrWhiteSpace( input ))
				return entityRef;
			int origEntityTypeId = entityTypeId;
			if ( entityTypeId == 0 )
			{
				//don't always know the type, especially for org accrediting/owning something.
			}

			//determine if just Id, or base
			if ( input.StartsWith( "http" ) )
			{
				registryAtId = input;
				if ( registryAtId != registryAtId.ToLower() )
				{
					status.AddWarning( string.Format( "Property: {0} Contains Upper case Reference URI: {1} ", property, registryAtId ) );
				}
				//TODO - need to ensure existing reference entities can be updated!!!!!!!!
				//	also these may need to be reindexed!
				entityRef = ResolveEntityRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status, parentCTID );
				//break;
			}
			else if ( input.StartsWith( "_:" ) )
			{
				//should be a blank node
				var node = GetBlankNode( input );
				//temp fix
				//if ( node.Type == "ceterms:Occupation" && ( System.DateTime.Now.ToString( "yyyy-MM-dd HH" ) == "2023-09-27 17" || System.DateTime.Now.ToString( "yyyy-MM-dd HH" ) == "2023-09-27 18" ) )
				//{
				//	node.Type = "ceterms:AssessmentProfile";
				//}
				string name = HandleBNodeLanguageMap( node.Name, "blank node name", true );
				if ( node == null || string.IsNullOrWhiteSpace( name ) )
				{
					status.AddError( string.Format( "A Blank node was not found for bNodeId of: {0}. ", input ) );
					return entityRef;
					//return entityRefs;
				}

				string desc = HandleBNodeLanguageMap( node.Description, "blank node desc", true );
				bool isQAOrgType = false;
				//OR determine a context (ie. property accredited by)
				//NOTE: currently only called for org where is accredits etc.
				if ( origEntityTypeId == 0 || origEntityTypeId == 2 )
					entityTypeId = GetBlankNodeEntityType( node, ref isQAOrgType );
				//again no, needs to be saved as a course
				if ( entityTypeId == 37 || entityTypeId == 36 )
				{
					//entityTypeId = 7;
				}
				//if type present,can use
				//DUPLICATE ALARM: may want to take approach of deleting existing
				entityRef = ResolveBlankNodeToGuid( property, node, entityTypeId, isQAOrgType, ref status );
			}
			else
			{
				//???
				status.AddError( string.Format( "Unexpected value of '{0}' for EntityReference: '{1}'. The expected value is either a registry URI or a blank node identifier.", input, property ) );
				return entityRef;
			}
			return entityRef;
		}
		private Guid ResolveEntityRegistryAtIdToGuid( string property, string registryAtId, int entityTypeId, ref SaveStatus status, string parentCTID = "" )
		{
			bool isResolved = false;
			Guid entityUID = Guid.Empty;
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				entityUID = ResolutionServices.ResolveEntityByRegistryAtIdToGuid( property, registryAtId, entityTypeId, ref status, ref isResolved, parentCTID );
			}

			return entityUID;
		}

		private Guid ResolveBlankNodeToGuid( string property, BNode input, int entityTypeId, bool isQAOrgType, ref SaveStatus status )
		{
			Guid entityUID = new Guid();
			int entityRefId = 0;
			status.HasSectionErrors = false;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

			if ( string.IsNullOrWhiteSpace( name ) )
				status.AddError( "Invalid EntityBase/BNode, missing name" );
			//not always required
			if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
			{
				//no longer required
				//status.AddWarning( string.Format( "Invalid bnode. Type: {0}, name: {1}.  missing SubjectWebpage", entityTypeId, name ) );
			}
			if ( status.HasSectionErrors )
				return entityUID;
			//look up by subject webpage
			//		==> SWP may not be present
			//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
			//NOTE: need to avoid duplicate swp's; so  should combine
			//if ()
			if ( entityTypeId > 0 )
			{
				//this could be risky
				var entity = EntityManager.EntityCache_GetReference( entityTypeId, name, input.SubjectWebpage, desc );
				if ( entity != null && entity.Id > 0 )
				{
					//20-12-16 mp - we don't want to return the UID, need to be able to update the reference
					//					OR ALWAYS DELETE
					//check what happens if called here
					//23-09-27 mp - this process seems the same as the process for not found.
					//				- TODO - sync with process for ResolveEntityBaseToInt
					if ( string.IsNullOrWhiteSpace( entity.ResourceDetail ) )
					{
						//should just let it fall thru?
						//but will it be useful to reindex?
						TopLevelObject resource = new TopLevelObject()
						{
							Id = entity.Id,
							Name = entity.Name,
							EntityTypeId = entityTypeId,//AND?
							RowId = entity.EntityUid,
							OrganizationId = entity.OwningOrgId,
						};
						ResourcesToIndex.Add( resource );
					}
					return entity.EntityUid;
					/*
					switch ( entityTypeId )
					{
						case 1:
							//if we know found, then should pass the Id, rather than searching again!
							entityRefId = ResolveBlankNodeAsCredential( input, ref entityUID, ref status );
							break;
						case 2:
						case 13:
						case 14:
							entityRefId = ResolveBlankNodeAsOrganization( property, input, entityTypeId, ref entityUID, ref status );
							break;
						case 3:
							entityRefId = ResolveBlankNodeAsAssessment( input, ref entityUID, ref status );
							break;
						case 7:
						case 36:
						case 37:
							entityRefId = ResolveBlankNodeAsLopp( input, entityTypeId, ref entityUID, ref status );
							break;
						case 17:
							status.AddError( string.Format( "Error - Competency blank nodes are not currently handled. entityTypeId: {0}, Name: {1}", entityTypeId, input.Name ) );
							break;
						case 32:
							entityRefId = ResolveBlankNodeAsJob( input, ref entityUID, ref status );
							break;
						case 33:
							entityRefId = ResolveBlankNodeAsTask( input, ref entityUID, ref status );
							break;
						case 34:
							entityRefId = ResolveBlankNodeAsWorkRole( input, ref entityUID, ref status );
							break;
						case 35:
							entityRefId = ResolveBlankNodeAsOccupation( input, ref entityUID, ref status );
							break;
						default:
							//unexpected, should not have entity references for manifests
							status.AddError( string.Format( "Error - unhandled entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
							return new Guid();
					}
					*/
					//
					//if ( entityRefId > 0 )
					//	return entity.EntityUid;
				}
			}
			//if not found, then create
			//24-03-04 mp - NOTE:entityRefId is assigned but not used, as entityUID is returned from the ref.
			switch ( entityTypeId )
			{
				case 1:
					//if we know found, then should pass the Id, rather than searching again!
					entityRefId = ResolveBlankNodeAsCredential( input, ref entityUID, ref status );
					break;
				case 2:
				case 13:
				case 14:
					entityRefId = ResolveBlankNodeAsOrganization( property, input, entityTypeId, ref entityUID, ref status );
					break;
				case 3:
					entityRefId = ResolveBlankNodeAsAssessment( input, ref entityUID, ref status );
					break;
				case 7:
				case 36:
				case 37:
					entityRefId = ResolveBlankNodeAsLopp( input, entityTypeId, ref entityUID, ref status );
					break;
				case 17:
					status.AddError( string.Format( "Error - Competency blank nodes are not currently handled. entityTypeId: {0}, Name: {1}", entityTypeId, input.Name ) );
					break;
				case 32:
					entityRefId = ResolveBlankNodeAsJob( input, ref entityUID, ref status );
					break;
				case 33:
					entityRefId = ResolveBlankNodeAsTask( input, ref entityUID, ref status );
					break;
				case 34:
					entityRefId = ResolveBlankNodeAsWorkRole( input, ref entityUID, ref status );
					break;
				case 35:
					entityRefId = ResolveBlankNodeAsOccupation( input, ref entityUID, ref status );
					break;
				default:
					//unexpected, should not have entity references for manifests
					status.AddError( string.Format( "Error - unhandled entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
					return new Guid();
			}
		
			return entityUID;
		}

		/// <summary>
		/// Map list of EntityBase items to a list of integer Ids.
		/// These Ids will be used for child records under an Entity,
		/// ONLY USED WHERE THE EntityTypeId is known
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<int> MapEntityReferences( string property, List<string> input, int entityTypeId, ref SaveStatus status )
		{
			List<string> ctidList = new List<string>();
			return MapEntityReferences( property, input, entityTypeId, ref status, ref ctidList );
		}

		/// <summary>
		/// Map list of EntityBase items to a list of integer Ids.
		/// These Ids will be used for child records under an Entity
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<int> MapEntityReferences( string property, List<string> input, int entityTypeId, ref SaveStatus status, ref List<string> ctidList )
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
					var ctid = ResolutionServices.ExtractCtid( target.Trim() );
					if ( BaseFactory.IsValidCtid( ctid ) )
						ctidList.Add( ctid );
					//TODO - what to do with non-registry URIs? For example
					// http://dbpedia.com/Stanford_University
					entityRef = ResolveEntityRegistryAtId( property, registryAtId, entityTypeId, ref status );
					if ( entityRef == 0 )
					{
						LoggingHelper.DoTrace( 5, string.Format( "MappingHelper.MapEntityReferences: FAILED TO RESOLVE EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target ) );
					} else
					{

					}
					//break;
				}
				else if ( target.StartsWith( "_:" ) )
				{
					LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target bnode: {1} ", entityTypeId, target ) );
					var node = GetBlankNode( target );
					//if type present,can use
					entityRef = ResolveBlankNodeToInt( property, node, entityTypeId, ref status );
				}
				if ( entityRef > 0 )
					entityRefs.Add( entityRef );
			}

			return entityRefs;
		}

		///// <summary>
		///// Only use this method where the entity type is known!
		///// </summary>
		///// <param name="property"></param>
		///// <param name="target"></param>
		///// <param name="entityTypeId"></param>
		///// <param name="status"></param>
		///// <param name="allowingBlankNodes"></param>
		///// <returns></returns>
		//public int MapEntityReference( string property, string target, int entityTypeId, ref SaveStatus status, bool allowingBlankNodes = true )
		//{
		//	int entityRef = 0;
		//	string registryAtId = "";
		//	if ( string.IsNullOrWhiteSpace( target ) )
		//		return 0;

		//	entityRef = 0;
		//	//determine if just Id, or base
		//	if ( target.StartsWith( "http" ) )
		//	{
		//		LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityTypeId: {0}, CtdlId: {1} ", entityTypeId, target ) );
		//		registryAtId = target;
		//		entityRef = ResolveEntityRegistryAtId(property, registryAtId, entityTypeId, ref status );
		//		if ( entityRef == 0 )
		//		{
		//			LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: FAILED TO RESOLVE EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target ) );
		//		}
		//		//break;
		//	}
		//	else if ( target.StartsWith( "_:" ) )
		//	{
		//		if ( !allowingBlankNodes )
		//		{
		//			//what to do? log and what - don't necessarily want to send an email
		//		}
		//		else
		//		{
		//			LoggingHelper.DoTrace( 7, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target bnode: {1} ", entityTypeId, target ) );
		//			var node = GetBlankNode( target );
		//			//if type present,can use
		//			entityRef = ResolveEntityBaseToInt( property, node, entityTypeId, ref status );
		//		}
		//	}

		//	return entityRef;
		//}

		/// <summary>
		/// Entities will be string, where cannot be a third party reference
		/// That is not blank nodes
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId">TODO - try to keep generic for lopp subclasses, etc. </param>
		/// <param name="parentEntityTypeId">Used for messages only</param>
		/// <param name="status"></param>
		/// <returns></returns>
		public List<int> MapEntityReferences( List<string> input, int entityTypeId, int parentEntityTypeId, ref SaveStatus status )
		{
			string property= "TODO: need to add from caller";
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
						var ctid = ResolutionServices.ExtractCtid( registryAtId.Trim() );

						entityRef = ResolveEntityRegistryAtId( property, registryAtId, entityTypeId, ref status );
					}
					else
					{
						//not valid, log and continue?
						status.AddError( string.Format( "Invalid Entity reference encountered. parentEntityTypeId: {0}; entityReferenceTypeId: {1}; #: {2}; target: {3} ", parentEntityTypeId, entityTypeId, cntr, target ) );
					}
				}

				if ( entityRef > 0 )
					entityRefs.Add( entityRef );
			}

			return entityRefs;
		}

		/// <summary>
		/// Resolve a registry entity to a record in the database.
		/// Confirm the process handles ignoring virtual deletes
		/// </summary>
		/// <param name="registryAtId"></param>
		/// <param name="entityTypeId">May be zero</param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int ResolveEntityRegistryAtId( string property, string registryAtId, int entityTypeId, ref SaveStatus status )
		{
			bool isResolved = false;
			int entityRefId = 0;
			if ( !string.IsNullOrWhiteSpace( registryAtId ) )
			{
				//?? just returning an id is not useful if the entity type is not known
				entityRefId = ResolutionServices.ResolveEntityByRegistryAtId(property, registryAtId, entityTypeId, ref status, ref isResolved );
			}

			return entityRefId;
		}
		//TODO - need the parent in order to do safer assignments
		//we have CurrentEntityCTID, but the direct parent could be a condition or process profile, etc. 
		private int ResolveBlankNodeToInt( string property, BNode input, int entityTypeId, ref SaveStatus status )
		{

			int entityRefId = 0;
			Guid entityUID = new Guid();
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
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", false );
			//this can be flawed. If an owner is included, should always be used
			var ownedBy = MapOrganizationReferenceGuids( "AssessmentReference.OwnedBy", input.OwnedBy, ref status );
			Guid primaryAgentUID = new Guid();
			if ( ownedBy != null && ownedBy.Any() )
			{
				primaryAgentUID = ownedBy[0];
			}

			//WARNING: not all reference resources need to have a SWP now.
			//	As well need to watch for hits on top level resources
			//		So maybe only search for references here???
			//	May want to include description as is being done in the resolve methods!!
			//	Could bnodeId be used as the rowId? Confirm how the assistant API creates the bnodeId
			var entity = EntityManager.EntityCache_GetReference( entityTypeId, name, input.SubjectWebpage, desc );
			if ( entity != null && entity.Id > 0 )
			{
				if ( string.IsNullOrWhiteSpace(entity.ResourceDetail))
				{
					//should just let it fall thru?
					//but will it be useful to reindex?
					TopLevelObject resource = new TopLevelObject()
					{
						Id = entity.Id,
						Name = entity.Name,
						EntityTypeId = entityTypeId,//AND?
						RowId = entity.EntityUid,
						OrganizationId = entity.OwningOrgId,
					};
					ResourcesToIndex.Add( resource );
				}
				return entity.BaseId;
			}

			switch ( entityTypeId )
			{
				case 1:
					entityRefId = ResolveBlankNodeAsCredential( input, ref entityUID, ref status );
					break;
				case 2:
				case 13:
				case 14:
					entityRefId = ResolveBlankNodeAsOrganization( property, input, entityTypeId, ref entityUID, ref status );
					break;
				case 3:
					entityRefId = ResolveBlankNodeAsAssessment( input, ref entityUID, ref status );
					break;
				case 7:
				case 36:
				case 37:
					entityRefId = ResolveBlankNodeAsLopp( input, entityTypeId, ref entityUID, ref status );
					break;
				case 17:
					status.AddError( string.Format( "Error - Competency blank nodes are not currently handled. entityTypeId: {0}, Name: {1}", entityTypeId, input.Name ) );
					break;
				case 32:
					entityRefId = ResolveBlankNodeAsJob( input, ref entityUID, ref status );
					break;
				case 33:
					entityRefId = ResolveBlankNodeAsTask( input, ref entityUID, ref status );
					break;
				case 34:
					entityRefId = ResolveBlankNodeAsWorkRole( input, ref entityUID, ref status );
					break;
				case 35:
					entityRefId = ResolveBlankNodeAsOccupation( input, ref entityUID, ref status );
					break;
				default:
					//unexpected, should not have entity references for manifests
					status.AddError( string.Format( "Error - unhandled blank node entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
					break;
			}

			/*
			if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
				entityRefId = ResolveBlankNodeAsCredential( input, ref entityRef, ref status );

			else if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION )
				entityRefId = ResolveBlankNodeAsOrganization( property, input, entityTypeId, ref entityRef, ref status );

			else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
				entityRefId = ResolveBlankNodeAsAssessment( input, ref entityRef, ref status );
			else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
				entityRefId = ResolveBlankNodeAsLopp( input, entityTypeId, ref entityRef, ref status );

            else if ( entityTypeId == CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE )
                entityRefId = ResolveBlankNodeAsOccupation( input, ref entityRef, ref status );

            else if ( entityTypeId == CodesManager.ENTITY_TYPE_JOB_PROFILE )
                entityRefId = ResolveBlankNodeAsJob( input, ref entityRef, ref status );

            else if ( entityTypeId == CodesManager.ENTITY_TYPE_TASK_PROFILE )
                entityRefId = ResolveBlankNodeAsTask( input, ref entityRef, ref status );

            else if ( entityTypeId == CodesManager.ENTITY_TYPE_WORKROLE_PROFILE )
                entityRefId = ResolveBlankNodeAsWorkRole( input, ref entityRef, ref status );
            else
			{
				//unexpected, should not have entity references for manifests
				status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
			}
			*/

			return entityRefId;
		}
		private int ResolveBlankNodeAsCredential( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			MC.Credential output = new MC.Credential();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

			try
			{
				var ownedBy = MapOrganizationReferenceGuids( "CredentialReference.OwnedBy", input.OwnedBy, ref status );
				Guid primaryAgentUID = new Guid();
				if ( ownedBy != null && ownedBy.Any())
				{
					primaryAgentUID = ownedBy[0];
				}
				//get full record for updates
				//really should limit this so whole record isn't retrieved
				if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
					output = CredentialManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
				if ( output != null && output.Id > 0 )
				{
					//20-12-08 mp - combine mapping

				}
				else
				{
					output = CredentialManager.GetByName_CodedNotation_PrimaryAgentUId( name, input.CodedNotation, primaryAgentUID );
					if ( output != null && output.Id > 0 )
					{

					}
					else
					{
						//==================================================
						output = new Credential()
						{
							Name = name,
							SubjectWebpage = input.SubjectWebpage,
							Description = desc,
							CredentialTypeSchema = input.Type
						};
					}
				}
				//WARNING - if the returned resource is a full resource, need to be careful about overwriting, in fact shouldn't continue?
				//ALSO: if a different owner
				if ( ServiceHelper.IsValidCtid( output.CTID ) )
				{
					//will need to reindex
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
				else
				{
					//need additional check for missing credential type id
					//???
					if ( output.Id > 0 && output.CredentialTypeId == 0 )
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
					output.EstimatedDuration = FormatDuration( $"Credential.EstimatedDuration", input.EstimatedDuration, ref status );
					//
					output.Identifier = MapIdentifierValueList( input.Identifier );
					//if ( output.Identifier != null && output.Identifier.Count() > 0 )
					//{
					//	output.IdentifierJSON = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
					//}
					output.OfferedBy = MapOrganizationReferenceGuids( "CredentialReference.OfferedBy", input.OfferedBy, ref status );
					output.OwnedBy = MapOrganizationReferenceGuids( "CredentialReference.OwnedBy", input.OwnedBy, ref status );
					//
					output.Subject = MapCAOListToTextValueProfile( input.Subject, CodesManager.PROPERTY_CATEGORY_SUBJECT );
					entityUid = output.RowId;
					//
					if ( output.Id > 0 )
					{
						new CredentialManager().Save( output, ref status );
						entityUid = output.RowId;
						entityRefId = output.Id;
					}
					else
					{
						if ( new CredentialManager().AddBaseReference( output, ref status ) > 0 )
						{
							entityUid = output.RowId;
							entityRefId = output.Id;
						}
					}
				}
					
				

				//TBD
				var eManager = new EntityManager();
				string statusMsg = "";
				//wrong
				//will be handled for TVP, what about others? Could add to a queue and process later?
				//need to do have the parent has been saved and related relationships have been saved

				
				//if ( eManager.EntityCacheUpdateAgentRelationshipsForCredential( output.RowId.ToString(), ref statusMsg ) == false )
				//{
				//	status.AddError( statusMsg );
				//}

				//24-03-25 mp - add to a queue for generic handling later
				output.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL;
				ResourcesToIndex.Add( output );
				//need to add to elastic - may need to do this later. TVP is OK, now others
				//new CredentialServices().UpdatePendingReindex( output, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBlankNodeAsCredential(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}
			return entityRefId;
		}
		private int ResolveBlankNodeAsOrganization( string property, BNode input, int entityTypeId, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			status.HasSectionErrors = false;
			if ( input == null )
				return entityRefId;
			string name = string.Empty;
			try
			{
				name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
				string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
				//no, should only be for compares
				//name = name.Replace( " &amp; ", " and " ).Replace( " & ", " and " );

				//look up by name, subject webpage
				//to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
				//NOTE: need to avoid duplicate swp's; so  should combine
				//Be sure this method will return all properties that could be present in a BN
				//20-08-23  - add email, address, and availabilityListing
				//21-12-07 - the URL checking should use the domain only, and handle with and without www
				//		
				//22-09-09 - if we have a bnode and a full org exists, there should NOT be an update!
				//TODO - delete existing references for the org
				//23-12-04 - with the ACE example, need to consider the full URL now
				if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )	
				{
					
					//maybe include entityTypeId??
					//now swp is not required. So if not present, just name, and possible desc?
					var org = OrganizationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
					if ( org != null && org.Id > 0 )
					{
						if ( org.EntityStateId == 3 )
						{
							org.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
							entityUid = org.RowId;
							ResourcesToIndex.Add( org );
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
						else if ( !string.IsNullOrWhiteSpace( desc ) )
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
						org.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
						ResourcesToIndex.Add( org );
						return org.Id;
					}
				}
				//need type of org!!
				var output = new MC.Organization()
				{
					AgentDomainType = input.Type,
					Name = name,
					EntityTypeId = entityTypeId,
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

					output.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIAL_ORGANIZATION;
					ResourcesToIndex.Add( output );
				}
			} catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBlankNodeAsOrganization(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}

			return entityRefId;
		}
		private int ResolveBlankNodeAsAssessment( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			WPM.AssessmentProfile output = new WPM.AssessmentProfile();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", false );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", false );
			try
			{
				//this can be flawed. If an owner is included, should always be used
				var ownedBy = MapOrganizationReferenceGuids( "AssessmentReference.OwnedBy", input.OwnedBy, ref status );
				Guid primaryAgentUID = new Guid();
				if ( ownedBy != null && ownedBy.Any() )
				{
					primaryAgentUID = ownedBy[0];
				}
				if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
					output = AssessmentManager.GetByName_SubjectWebpage( name, input.SubjectWebpage, input.CodedNotation, primaryAgentUID );
				if ( output != null && output.Id > 0 )
				{
					//this is still weak, if just name and SWP could get wrong one
					//if not changing, don't want to take the hit of resource detail plus indexing!
				}
				else
				{
					
					output = AssessmentManager.FindReferenceResource( name, desc, input.CodedNotation, primaryAgentUID );
					if ( output != null && output.Id > 0 )
					{

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
				}


				//20-07-04 need to handle additional properties
				//24-03-24 mp - but do NOT allow updates to a resource with a CTID!!!!
				//	except the context. What is referring to this
				if ( ServiceHelper.IsValidCtid( output.CTID ) )
				{
					//will need to reindex
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
				else
				{
					//20-12-08 mp - combine mapping

					output.AssessesCompetencies = MapCAOListToCAOProfileList( input.Assesses, true );
					output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );

					output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
					output.AvailableAt = FormatAvailableAtAddresses( input.AvailableAt, ref status );
					//
					output.CodedNotation = input.CodedNotation;
					output.CreditValue = HandleValueProfileList( input.CreditValue, "Assessment.CreditValue" );
					output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );

					//get rid of this:
					//if ( output.CreditValueList != null && output.CreditValueList.Any() )
					//	output.CreditValue = output.CreditValueList[ 0 ];
					output.DeliveryType = MapCAOListToEnumermation( input.DeliveryType );

					//
					output.EstimatedDuration = FormatDuration( $"Asmt.EstimatedDuration", input.EstimatedDuration, ref status );
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
						output.PrimaryAgentUID = output.OwnedBy[0];
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
						entityRefId = output.Id;
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
				//
				output.EntityTypeId = CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE;
				ResourcesToIndex.Add( output );

				//var eManager = new EntityManager();
				//string statusMsg = "";

				//need to add to elastic - may need to do this later. TVP is OK, now others
				//new AssessmentServices().UpdatePendingReindex( output, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBlankNodeAsAssessment(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}
			return entityRefId;
		}

		/// <summary>
		/// TODO: need to include more data for existance checks, including:
		/// - parent (at least type)
		/// - publishedBy (used where no owner)
		/// We might be able to create and use a module variable?
		/// </summary>
		/// <param name="input"></param>
		/// <param name="entityTypeId"></param>
		/// <param name="entityUid"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		private int ResolveBlankNodeAsLopp( BNode input, int entityTypeId, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			WPM.LearningOpportunityProfile output = new WPM.LearningOpportunityProfile();
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			//20-12-15 mp - now a subject webpage may not be required, so we would need something else OR ALWAYS DELETE

			try
			{
				//this can be flawed. If an owner is included, should always be used
				var ownedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
				Guid primaryAgentUID = new Guid();
				if ( ownedBy != null && ownedBy.Any())
				{
					primaryAgentUID = ownedBy[0];
				}
				//probably want one method to handle the variations
				//sigh could have the same swp and different codedNotation
				if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
					output = LearningOpportunityManager.GetByName_SubjectWebpage( name, input.SubjectWebpage, input.CodedNotation, primaryAgentUID );
				if ( output != null && output.Id > 0 )
				{
					//20-12-08 mp - combine mapping

				}
				else
				{
					//may want to include a description for a tie breaker
					output = LearningOpportunityManager.FindReferenceResource( name, input.CodedNotation, primaryAgentUID );
					if ( output != null && output.Id > 0 )
					{

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
				}


				//WARNING - if the returned resource is a full resource, need to be careful about overwriting, in fact shouldn't continue?
				//ALSO: if a different owner
				if ( ServiceHelper.IsValidCtid( output.CTID ) )
				{
					//will need to reindex
					entityRefId = output.Id;
				}
				else
				{
					output.OfferedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OfferedBy", input.OfferedBy, ref status );
					output.OwnedBy = ownedBy;   // MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
					if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
					{
						output.PrimaryAgentUID = output.OwnedBy[0];
					}
					else if ( output.OfferedBy != null && output.OfferedBy.Count > 0 )
					{
						output.PrimaryAgentUID = output.OfferedBy[0];
					}

					//20-12-08 mp - combine mapping
					output.LearningEntityTypeId = output.EntityTypeId = entityTypeId;
					//20-07-04 need to handle additional properties
					output.TeachesCompetencies = MapCAOListToCAOProfileList( input.Teaches, true );
					output.AssessesCompetencies = MapCAOListToCAOProfileList( input.Assesses, true );
					output.AssessmentMethodType = MapCAOListToEnumermation( input.AssessmentMethodType );
					output.AssessmentMethodDescription = HandleLanguageMap( input.AssessmentMethodDescription, output, "AssessmentMethodDescription" );
					output.AvailableAt = FormatAvailableAtAddresses( input.AvailableAt, ref status );
					//
					output.CodedNotation = input.CodedNotation;
					//new
					//output.QVCreditValueList = HandleValueProfileListToQVList( input.CreditValue, "LearningOpportunity.CreditValue" );
					output.CreditValue = HandleValueProfileList( input.CreditValue, "LearningOpportunity.CreditValue" );
					output.CreditValueJson = JsonConvert.SerializeObject( output.CreditValue, MappingHelperV3.GetJsonSettings() );

					output.EstimatedDuration = FormatDuration( $"Lopp.EstimatedDuration", input.EstimatedDuration, ref status );
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
						entityRefId = output.Id;
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
				/*
				 * 24-03-04 mp - what? this wrong!!
				var eManager = new EntityManager();
				string statusMsg = "";
				var resourceDetail = JsonConvert.SerializeObject( output, JsonHelper.GetJsonSettings( false ) );
				if ( eManager.EntityCacheUpdateResourceDetail( output.RowId, resourceDetail, ref statusMsg ) == 0 )
				{
					status.AddError( statusMsg );
				}
				if ( eManager.EntityCacheUpdateAgentRelationshipsForLopp( output.RowId.ToString(), ref statusMsg ) == false )
				{
					status.AddError( statusMsg );
				}
				*/

				//need to add to elastic - may need to do this later
				//also need to generate the resourceDetail data for use in elastic/export
				//	although at this point, a TVP may not be available
				//24-03-01 - for TVPs, the lopps/asmts will be handled after the TVP save.
				//		Now what about others? Need to generate the resource detail and agent relationships
				//24-03-25 mp - add to a queue for generic handling later
				output.EntityTypeId = CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE;
				ResourcesToIndex.Add( output );

				//new LearningOpportunityServices().UpdatePendingReindex( output, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelperV3.ResolveBlankNodeAsLopp(). CurrentEntityCTID: '{0}', Name: {1}", CurrentEntityCTID, ( name ?? "missing" ) ) );
				status.AddError( ex.Message );
				return 0;
			}
			return entityRefId;
		}


		private int ResolveBlankNodeAsOccupation( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//don't have a lot to go on for occupation
			//if name changes, we will get dups
			//and we may just have a name
			var output = new MC.OccupationProfile();
			if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
			{
				output = OccupationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			}
			else if ( !string.IsNullOrWhiteSpace( desc ) )
			{
				output = OccupationManager.GetByNameAndDescription( name, desc );
			} else
				output = OccupationManager.GetByName( name );

			if ( output != null && output.Id > 0 )
			{
				//


			}
			else
			{
				//==================================================
				output = new MC.OccupationProfile()
				{
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					Description = desc
				};
			}


			//output.OfferedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OfferedBy", input.OfferedBy, ref status );
			//output.OwnedBy = MapOrganizationReferenceGuids( "LearningOpportunityReference.OwnedBy", input.OwnedBy, ref status );
			//if ( output.OwnedBy != null && output.OwnedBy.Count > 0 )
			//{
			//	output.OwningAgentUid = output.OwnedBy[ 0 ];
			//}


			//
			if ( output.Id > 0 )
			{
				if ( output.EntityStateId < 3 )
					new OccupationManager().Save( output, ref status );
				entityUid = output.RowId;
				entityRefId = output.Id;
			}
			else
			{
				if ( new OccupationManager().AddReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}
			//
			output.EntityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE;
			ResourcesToIndex.Add( output );
			return entityRefId;
		}

		private int ResolveBlankNodeAsJob( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			var output = new MC.Job();
			if ( !string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
			{
				output = JobManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
			}
			else if ( !string.IsNullOrWhiteSpace( desc ) )
			{
				output = JobManager.GetByNameAndDescription( name, desc );
			}
			else
			{
				output = JobManager.GetByName( name );
			}
			if ( output != null && output.Id > 0 )
			{


			}
			else
			{
				//==================================================
				output = new MC.Job()
				{
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					Description = desc
				};
			}

			//
			if ( output.Id > 0 )
			{
				if ( output.EntityStateId < 3)
					new JobManager().Save( output, ref status );
				entityUid = output.RowId;
				entityRefId = output.Id;
			}
			else
			{
				if ( new JobManager().AddReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}
			//
			output.EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
			ResourcesToIndex.Add( output );

			return entityRefId;
		}
		private int ResolveBlankNodeAsTask( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//if name changes, we will get dups
			var output = new MC.Task();
			if ( !string.IsNullOrWhiteSpace( desc ) )
			{
				output = TaskManager.GetByNameAndDescription( name, desc );
			}
			else
			{
				output = TaskManager.GetByName( name );
			}
			if ( output != null && output.Id > 0 )
			{


			}
			else
			{
				//==================================================
				output = new MC.Task()
				{
					Name = name,
					SubjectWebpage = input.SubjectWebpage,
					Description = desc
				};
			}

			//
			if ( output.Id > 0 )
			{
				if (output.EntityStateId < 3)
					new TaskManager().Save( output, ref status );
				entityUid = output.RowId;
				entityRefId = output.Id;
			}
			else
			{
				if ( new TaskManager().AddReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}
			//
			output.EntityTypeId = CodesManager.ENTITY_TYPE_TASK_PROFILE;
			ResourcesToIndex.Add( output );
			return entityRefId;
		}

		private int ResolveBlankNodeAsWorkRole( BNode input, ref Guid entityUid, ref SaveStatus status )
		{
			int entityRefId = 0;
			string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
			string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
			//this will be tough with just a name. actually consider decription, especially for successive updates
			var output = new MC.WorkRole();
			if ( !string.IsNullOrWhiteSpace( desc ) )
			{
				output = WorkRoleManager.GetByNameAndDescription( name, desc );
			} else
			{
				output = WorkRoleManager.GetByName( name );
			}
			if ( output != null && output.Id > 0 )
			{


			}
			else
			{
				//==================================================
				output = new MC.WorkRole()
				{
					Name = name,
					Description = desc
				};
			}

			//not likely to provide much else?

			//
			output.Identifier = MapIdentifierValueListInternal( input.Identifier );
			if ( output.Identifier != null && output.Identifier.Count() > 0 )
			{
				output.IdentifierJson = JsonConvert.SerializeObject( output.Identifier, MappingHelperV3.GetJsonSettings() );
			}


			if ( output.Id > 0 )
			{
				if (output.EntityStateId < 3)
					new WorkRoleManager().Save( output, ref status );
				entityUid = output.RowId;
				entityRefId = output.Id;
			}
			else
			{
				if ( new WorkRoleManager().AddReference( output, ref status ) > 0 )
				{
					entityUid = output.RowId;
					entityRefId = output.Id;
				}
			}

			//
			output.EntityTypeId = CodesManager.ENTITY_TYPE_WORKROLE_PROFILE;
			ResourcesToIndex.Add( output );
			return entityRefId;
		}

		#endregion

		#region Map Entity Guids to Resource summary 
		public List<ResourceSummary> MapEntityUIDsToResourceSummary( List<Guid> input )
		{
			var output = new List<ResourceSummary>();
			if ( input == null || !input.Any() )
				return null;

			
			foreach ( var item in input )
			{
				//look up entityType
				var cacheItem = EntityManager.EntityCacheGetByGuid( item );
				if ( cacheItem != null && cacheItem.Id > 0 )
				{
					var resSummary = new ResourceSummary()
					{
						Id = cacheItem.Id,
						Name = cacheItem.Name,
						CTID = cacheItem.CTID,
						EntityTypeId = cacheItem.EntityTypeId,
					};
					output.Add( resSummary );
				}
				else
				{
					//error?
					//any reason not in cache yet?
				}
			}
		

			return output;
		}

		/// <summary>
		/// assumes concepts or competency
		/// </summary>
		/// <param name="input"></param>
		/// <param name="EntityTypeId"></param>
		/// <returns></returns>
		public List<ResourceSummary> MapEntityCTIDsToResourceSummary( List<string> input, int EntityTypeId )
		{
			var output = new List<ResourceSummary>();
			List<string> inputlist = new List<string>();
			if ( input == null || !input.Any() )
				return null;
			try
			{
				foreach ( string url in input )
				{
					if ( Regex.IsMatch( url, @"^(https://)" ) )
					{
						// Use a regular expression to extract the desired part
						Match match = Regex.Match( url, @"[^/]+$" );
						if ( match.Success )
						{
							inputlist.Add( match.Value );
						}
					}
				}
				input = inputlist.Count > 0 ? inputlist : input;
				foreach ( var item in input )
				{
					//look up entityType
					var cacheItem = EntityManager.EntityCacheGetByCTIDWithOrganization( item );
					if ( cacheItem != null && cacheItem.Id > 0 )
					{
						var resSummary = new ResourceSummary()
						{
							Id = cacheItem.BaseId,
							Name = cacheItem.Name,
							CTID = cacheItem.CTID,
							EntityTypeId = cacheItem.EntityTypeId,
						};
						output.Add( resSummary );
					}
					else
					{
						//error?
						//any reason not in cache yet?
					}
				}

			} catch (Exception ex )
			{
				LoggingHelper.LogError( ex, $"MappingHelperV3.MapEntityCTIDsToResourceSummary. EntityTypeId: {CurrentEntityTypeId}, CTID: {CurrentEntityCTID}" );
			}
			return output;
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
				output.YearsOfExperience = input.YearsOfExperience != null ? ( decimal ) input.YearsOfExperience : 0;
				output.Weight = input.Weight != null ? ( decimal ) input.Weight : 0;

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
                if ( input.TargetOccupation != null && input.TargetOccupation.Count > 0 )
                {
                    LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile. Has TargetOccupation: " + input.TargetOccupation.Count.ToString() );
                    output.TargetOccupationIds = MapEntityReferences( "ConditionProfile.TargetOccupation", input.TargetOccupation, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );

                }
				if ( input.TargetJob != null && input.TargetJob.Count > 0 )
				{
					LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile. Has TargetJob: " + input.TargetJob.Count.ToString() );
					output.TargetJobIds = MapEntityReferences( "ConditionProfile.TargetJob", input.TargetJob, CodesManager.ENTITY_TYPE_JOB_PROFILE, ref status );
				}

				//22-08-22 - using true, but the competency can be from a collection, is the latter significant?
				output.TargetCompetency = MapCAOListToCAOProfileList( input.TargetCompetency, true );
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
					//TODO - this doesn't work unless defined as an object
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
                    LoggingHelper.DoTrace( 5, string.Format( "MappingHelperV3.FormatConditionProfile (OLD). Experience exception CTID: {0}, ex: {1}", status.Ctid, ex.Message ) );
                }
                output.MinimumAge = input.MinimumAge;
				output.YearsOfExperience = input.YearsOfExperience != null ? ( decimal ) input.YearsOfExperience : 0;
				output.Weight = input.Weight != null ? ( decimal ) input.Weight : 0;
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
                if ( input.TargetOccupation != null && input.TargetOccupation.Count > 0 )
                {
                    LoggingHelper.DoTrace( 7, "MappingHelper.FormatConditionProfile. Has TargetOccupation: " + input.TargetOccupation.Count.ToString() );
                    output.TargetOccupationIds = MapEntityReferences( "ConditionProfile.TargetOccupation", input.TargetOccupation, CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE, ref status );

                }
                //22-08-22 - using true, but the competency can be from a collection, is the latter significant?
                output.TargetCompetency = MapCAOListToCAOProfileList( input.TargetCompetency, true );

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
		public List<WPM.CostProfile> ExpandCosts( List<WPM.CostProfileMerged> input )
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
		public List<WPM.DurationProfile> FormatDuration( string property, List<MJ.DurationProfile> input, ref SaveStatus status )
		{
			if ( input == null || input.Count == 0 )
				return null;

			List<WPM.DurationProfile> list = new List<WPM.DurationProfile>();
			var profile = new WPM.DurationProfile();

			foreach ( MJ.DurationProfile item in input )
			{
				if ( item.Description == null &&
					item.ExactDuration == null &&
					item.TimeRequired == null &&
					item.MaximumDuration == null &&
					item.MinimumDuration == null
					)
					continue;
				else
				{
					profile = new WPM.DurationProfile
					{
						RowId = Guid.NewGuid(),
						ExactDuration = FormatDurationItem( property, item.ExactDuration, ref status ),
						TimeRequiredImport = FormatDurationItem( property, item.TimeRequired, ref status ),
						MaximumDuration = FormatDurationItem( property, item.MaximumDuration, ref status ),
						MinimumDuration = FormatDurationItem( property, item.MinimumDuration, ref status )
					};
					profile.Description = HandleLanguageMap( item.Description, profile, "Description", true );
					// only allow an exact duration or a range, not both 
					if ( profile.IsRange && profile.ExactDuration != null )
					{
						status.AddWarning( $"Duration Profile error for {property}. Provide either an exact duration or a minimum and maximum range, but not both. Defaulting to range" );
						profile.ExactDuration = null;
					}
					if (profile.HasData)
						list.Add( profile );
				}

			}
			if ( list.Count > 0 )
				return list;
			else
				return null;
		}
		public WPM.DurationItem FormatDurationItem( string property, string duration, ref SaveStatus status )
		{
			if ( string.IsNullOrEmpty( duration ) )
				return null;
			//if (duration.IndexOf(".") > 0)
			//{
			//	LoggingHelper.DoTrace( 1, $"{thisClassName}.FormatDurationItem. Property: {property} ENCOUNTERED A DECIMAL DURATION. Using workaround!" );
			//	try
			//	{
			//		//workaround until decimals are handled
			//		WPM.DurationItemDecimal did = FormatDurationItemAsDecimal( property, duration, ref status );
			//		var output2 = new WPM.DurationItem
			//		{
			//			DurationISO8601 = did.DurationISO8601,
			//			Years = Convert.ToInt32( did.Years ),
			//			Months = Convert.ToInt32( did.Months ),
			//			Weeks = Convert.ToInt32( did.Weeks ),
			//			Days = Convert.ToInt32( did.Days ),
			//			Hours = Convert.ToInt32( did.Hours ),
			//			Minutes = Convert.ToInt32( did.Minutes )
			//		};
			//		return output2;
			//	}
			//	catch ( Exception ex )
			//	{
			//		var msg = BaseFactory.FormatExceptions( ex );
			//		LoggingHelper.LogError( ex, $"{thisClassName}.FormatDurationItem. Error exception encountered for Property: {property}, duration: {duration} after FormatDurationItemAsDecimal." );
			//		status.AddError( $"{thisClassName}.FormatDurationItem. Error exception encountered for Property: {property}, duration: {duration} after FormatDurationItemAsDecimal. " + msg );
			//		return null;

			//	}
			//}
			decimal years = 0, months = 0, weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;
			var originalDuration = duration;
            try
            {
				duration = originalDuration = duration.Replace( ".00", "" ).Replace( ".0", "" );

				string patternWithDecimals = "^P(?!$)((\\d+Y)|(\\d+\\.\\d+Y$))?((\\d+M)|(\\d+\\.\\d+M$))?((\\d+W)|(\\d+\\.\\d+W$))?((\\d+D)|(\\d+\\.\\d+D$))?(T(?=\\d)((\\d+H)|(\\d+\\.\\d+H$))?((\\d+M)|(\\d+\\.\\d+M$))?(\\d+(\\.\\d+)?S)?)??$";
				Regex Iso8601DurationRegex2 = new Regex( patternWithDecimals, RegexOptions.Compiled );
				var match = Iso8601DurationRegex2.Match( duration );
				//if ( !match.Success )
				//{
				//	throw new ArgumentOutOfRangeException( nameof( duration ) );
				//}
				//int
				//var matchI = new System.Text.RegularExpressions.Regex( @"^P(?!$)(\d+Y)?(\d+M)?(\d+W)?(\d+D)?(T(?=\d)(\d+H)?(\d+M)?(\d+S)?)?$" ).Match( duration );


				if (!match.Success)
				{
					//23-08-09 - kind of bad to blow up the whole import for this error!
					//throw new FormatException( "ISO8601 duration format" );
					status.AddError( $"Property: {property} had a 'bad duration ISO8601 duration format of {duration}" );
					//perhaps should still save with string
					//return null;
					var output2 = new WPM.DurationItem
					{
						DurationISO8601 = originalDuration,
						Years = years,
						Months = months,
						Weeks = weeks,
						Days = days,
						Hours = hours,
						Minutes = minutes
					};

					return output2;
				}

                if ( duration.ToLower().StartsWith( "p" ) )
                    duration = duration.Substring( 1 );

                string remainder = "";
				
				//if any time, skip the non-time
				var timePos = duration.ToLower().IndexOf( "t" );
				if ( timePos > -1 )
                {
					//can't have time with other data  (or years, but with awareness of an example with years and hours
					//just in case
					remainder = duration.Substring( timePos + 1 );

					hours = SplitDuration( remainder, "H", ref remainder );
                    minutes = SplitDuration( remainder, "M", ref remainder );
                    //if anything left, error
                    if ( !string.IsNullOrEmpty( remainder ) )
                    {
                        //di.sec = SplitDuration( remainder, "H", ref remainder );
                    }
                }
                else
                {
					years = SplitDuration( duration, "Y", ref remainder );				
					months = SplitDuration( remainder, "M", ref remainder );
                    weeks = SplitDuration( remainder, "W", ref remainder );
                    days = SplitDuration( remainder, "D", ref remainder );
                }
                /*
				//y - 1, 2
				if (match.Groups[1].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[1].Value, @"\d+" ).Value, out years );
				//m-4 0r 5
				int mthGoup = 4;
				if (match.Groups[ mthGoup ].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ mthGoup ].Value, @"\d+" ).Value, out months );
				//7,8
				int weekGroup = 8;
				if (match.Groups[ weekGroup ].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ weekGroup ].Value, @"\d+" ).Value, out weeks );

				//10,11
				int dayGroup = 10;
				if (match.Groups[ dayGroup ].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ dayGroup ].Value, @"\d+" ).Value, out days );
				//14,16
				int hourGroup = 14;
				if ( match.Groups[ hourGroup ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ hourGroup ].Value, @"\d+" ).Value, out hours );

				//17,18?
				int minGroup = 19;
				if (match.Groups[ minGroup ].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ minGroup ].Value, @"\d+" ).Value, out minutes );

				//20
				int secGroup = 19;
				if (match.Groups[ secGroup ].Success)
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ secGroup ].Value, @"\d+" ).Value, out seconds );

				*/

			}
			catch (Exception ex)
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.LogError( ex, $"{thisClassName}.FormatDurationItem. Error exception encountered for Property: {property}, duration: {duration}." );
                status.AddError( $"{thisClassName}.FormatDurationItem. Error exception encountered for Property: {property}, duration: {duration}. " + msg );
                return null;


            }
            var output = new WPM.DurationItem
			{
				DurationISO8601 = originalDuration,
				Years = years,
				Months = months,
				Weeks = weeks,
				Days = days,
				Hours = hours,
				Minutes = minutes
			};

			return output;
		}
		/// <summary>
		/// not clear how to do correctly with regex, so old style
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="letter"></param>
		/// <param name="remainder"></param>
		/// <returns></returns>
        public static decimal SplitDuration( string duration, string letter, ref string remainder )
        {
            var value = 0M;
            if ( string.IsNullOrEmpty( duration ) )
                return value;
            if ( duration.ToLower().StartsWith( "p" ) )
                duration = duration.Substring( 1 );
			//caller needs to do this?
            //if ( duration.ToLower().StartsWith( "t" ) )
            //    duration = duration.Substring( 1 );

            if ( duration.ToLower().IndexOf( letter.ToLower() ) == -1 )
            {
                //handle M before or after T
                //could be OK where caller handles how called
                remainder = duration;
				return value;
            }

            try
            {
                var result = duration.Split( letter.ToCharArray() );
                //not sure
                if ( result.Length > 0 )
                {
                    //have to consider months with minutes though unlikely.
                    //also have to consider just time
                    if ( result.Length > 1 )
                    {
                        remainder = result[1];
                        //remainder=string.Join( "", result.ToArray() );
                    }

                    if ( decimal.TryParse( result[0], out value ) )
                    {
                        return value;
                    }
                }
                else
                {

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, thisClassName + ".SplitDuration" );
            }

            return value;
        }

        public WPM.DurationItemDecimal FormatDurationItemAsDecimal( string property, string duration, ref SaveStatus status )
        {
            if (string.IsNullOrEmpty( duration ))
                return null;
			decimal years = 0, months = 0, weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;

			try
			{
				//decimal
				duration = duration.Replace( ".00", "" ).Replace( ".0", "" );
				var match = new System.Text.RegularExpressions.Regex( @"^P(?!$)(\d+Y)?(\d+M)?(\d+W)?(\d+D)?(T(?=\d)(\d+H)?(\d+M)?(\d+S)?)?$" ).Match( duration );

				if ( !match.Success )
				{
					//throw new FormatException( "ISO8601 duration format" );
					status.AddError( $"Property: {property} had a 'bad duration ISO8601 duration format of {duration}" );
					return null;
				}


				if ( match.Groups[ 1 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 1 ].Value, @"\d+" ).Value, out years );
				if ( match.Groups[ 2 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 2 ].Value, @"\d+" ).Value, out months );
				if ( match.Groups[ 3 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 3 ].Value, @"\d+" ).Value, out weeks );
				if ( match.Groups[ 4 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 4 ].Value, @"\d+" ).Value, out days );
				if ( match.Groups[ 6 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 6 ].Value, @"\d+" ).Value, out hours );
				if ( match.Groups[ 7 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 7 ].Value, @"\d+" ).Value, out minutes );
				if ( match.Groups[ 8 ].Success )
					decimal.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[ 8 ].Value, @"\d+" ).Value, out seconds );

			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.LogError( ex, $"{thisClassName}.FormatDurationItemAsDecimal. Error exception encountered for Property: {property}, duration: {duration}." );
				status.AddError( $"{thisClassName}.FormatDurationItem. Error exception encountered for Property: {property}, duration: {duration}. " + msg );
				return null;


			}

			var output = new WPM.DurationItemDecimal
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
        public List<int> MapVerificationServiceReferences( List<string> input, string property, string parentCTID, ref SaveStatus status )
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
                        var ctid = ResolutionServices.ExtractCtid( registryAtId.Trim() );
                        if ( string.IsNullOrWhiteSpace( ctid ) )
                        {
                            status.AddError( $"Invalid VerificationServiceProfile URI encountered: does not contain a CTID. Property: {property}, Parent CTID: {parentCTID}, VSP# {cntr}; URI: '{target}' " );
                            continue;
                        }
                        var entityUid = Guid.NewGuid();
                        var vsp = VerificationServiceProfileManager.GetByCtid( ctid );
						if ( vsp == null || vsp.Id == 0 )
						{
							//should not happen, but may be best to create a pending record?
							//Can happen as org needs to be published before the vsp
							var newId = new VerificationServiceProfileManager().AddPendingRecord( entityUid, ctid, registryAtId, ref status );
							if ( newId > 0)
								entityRefs.Add( newId );
                            //status.AddError( $"Error: A VerificationServiceProfile doesn't exist for the CTID: '{ctid}'. Property: {property}, Parent CTID: {parentCTID}, VSP# {cntr}. " );
                            //continue;
                        }
						else
						{
							entityRefs.Add( vsp.Id );
						}
                    }
                    else
                    {
                        //not valid, log and continue?
                        status.AddError( string.Format( "Invalid VerificationServiceProfile URI encountered: does not contain a CTID. parentCTID: '{0}', VSP# {1}, URI: '{2}' ", parentCTID, cntr, target ) );
                    }
                }

                if ( entityRef > 0 )
                    entityRefs.Add( entityRef );
            }

            return entityRefs;
        }


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
                    CTID = input.CTID,
                    DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
					SubjectWebpage = input.SubjectWebpage,
					HolderMustAuthorize = input.HolderMustAuthorize
				};
				profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
				profile.VerificationMethodDescription = HandleLanguageMap( input.VerificationMethodDescription, profile, "VerificationMethodDescription", true );

				//VerificationService is hidden in the publisher!
				profile.VerificationService = input.VerificationService;
				profile.OfferedByAgentUid = MapOrganizationReferencesToGuid( "VerificationServiceProfile.OfferedByAgentUid", input.OfferedBy, ref status );
				if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
					profile.TargetCredentialIds = MapEntityReferences( "VerificationServiceProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
				profile.VerificationDirectory = input.VerificationDirectory;
				profile.VerifiedClaimType = MapCAOListToEnumermation( input.VerifiedClaimType );
				profile.EstimatedCost = FormatCosts( input.EstimatedCost, ref status );
				profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
				profile.OfferedIn = MapToJurisdiction( input.OfferedIn, ref status );

				list.Add( profile );
			}

			return list;
		}
        public WPM.VerificationServiceProfile MapVerificationServiceProfile( MJ.VerificationServiceProfile input, ref SaveStatus status )
        {
            if ( input == null || input.Description == null )
                return null;

            var output = new WPM.VerificationServiceProfile
            {
                RowId = Guid.NewGuid(),
				CTID = input.CTID,
                DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
                SubjectWebpage = input.SubjectWebpage,
                HolderMustAuthorize = input.HolderMustAuthorize
            };
            output.Description = HandleLanguageMap( input.Description, output, "Description", true );
            output.EstimatedCost = FormatCosts( input.EstimatedCost, ref status );

			//????????????????????????????????????????????
            //output.OfferedByAgentUid = MapOrganizationReferencesToGuid( "VerificationServiceProfile.OfferedByAgentUid", input.OfferedBy, ref status );
            //BYs 
            output.OfferedByList = MapOrganizationReferenceGuids( "VerificationServiceProfile.OfferedBy", input.OfferedBy, ref status );
            //add warning?
            if ( output.OfferedByList == null || output.OfferedByList.Count == 0 )
            {
                status.AddWarning( "document doesn't have an offering organization." );
            }
            else
            {
                output.PrimaryAgentUID = output.OfferedByList[0];
                CurrentOwningAgentUid = output.OfferedByList[0];
            }

            output.OfferedIn = MapToJurisdiction( input.OfferedIn, ref status );
			//change to handle a list
            //output.VerificationDirectoryOLD = MapListToString( input.VerificationDirectory );
            output.VerificationDirectory = input.VerificationDirectory;
            output.VerificationMethodDescription = HandleLanguageMap( input.VerificationMethodDescription, output, "VerificationMethodDescription", true );

            //change to handle a list
            //output.VerificationServiceOLD = MapListToString( input.VerificationService );
            output.VerificationService = input.VerificationService;

            output.VerifiedClaimType = MapCAOListToEnumermation( input.VerifiedClaimType );

            output.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
            //this will be obsolete
            if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                output.TargetCredentialIds = MapEntityReferences( "VerificationServiceProfile.TargetCredential", input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );


            return output;
        }
		#endregion

		public static bool IsCredentialRegistryURL( string url )
		{
			if ( url.IndexOf( credentialRegistryUrl ) > -1 )
			{
				return true;
			}
			else
				return false;
		}
		public string FormatFinderResourcesURL( string url )
		{
			var finderUrl = "";
			var ctid = ResolutionServices.ExtractCtid( url.Trim() );
			if ( !string.IsNullOrWhiteSpace( ctid))
				finderUrl = UtilityManager.GetAppKeyValue( "credentialFinderMainSite" ) + "resources/" + ctid;

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
