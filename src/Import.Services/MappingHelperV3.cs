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
using BNode = RA.Models.JsonV2.BlankNode;
using InputAddress = RA.Models.JsonV2.Place;
using workIT.Models;
using MC = workIT.Models.Common;
using WPM = workIT.Models.ProfileModels;
using workIT.Factories;
using workIT.Utilities;
using workIT.Services;


namespace Import.Services
{
    public class MappingHelperV3
    {
        public List<BNode> entityBlankNodes = new List<BNode>();
        public string DefaultLanguage = "en";
        public List<MC.EntityLanguageMap> LanguageMaps = new List<MC.EntityLanguageMap>();
        public MC.EntityLanguageMap LastEntityLanguageMap = new MC.EntityLanguageMap();
        string lastLanguageMapString = "";
        string lastLanguageMapListString = "";
        public MC.BaseObject currentBaseObject = new MC.BaseObject();
        public MappingHelperV3()
        {
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
        public int GetBlankNodeEntityType( BNode node )
        {
            int entityTypeId = 0;
            switch ( node.Type.ToLower() )
            {
                case "ceterms:credentialorganization":
                    entityTypeId = 2;
                    break;
                case "ceterms:qacredentialorganization":
                    //what distinctions do we need for QA orgs?
                    entityTypeId = 2;
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
        public string HandleLanguageMap( MJ.LanguageMap map, string property, bool savingLastEntityMap = true, string languageCode = "en" )
        {
            return HandleLanguageMap( map, currentBaseObject, property, savingLastEntityMap, languageCode );
        }
        public string HandleLanguageMap( MJ.LanguageMap map, MC.BaseObject bo, string property, bool savingLastEntityMap = true,  string languageCode = "en" )
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
        public string HandleLanguageMap( MJ.LanguageMap map, MC.BaseObject bo, string property, ref string langMapString, bool savingLastEntityMap = true  )
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
                    if (cntr == 1)
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
            //assuming these types will only have one entry
            if ( !string.IsNullOrWhiteSpace( property[ 0 ].Name ) )
                return property[ 0 ].Name;
            else if ( !string.IsNullOrWhiteSpace( property[ 0 ].IdentifierValueCode ) )
                return property[ 0 ].IdentifierValueCode;
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
                    Name = item.Name,
                    Description = HandleLanguageMap( item.Description, currentBaseObject, "IdentifierValue Description", true )
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
        public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCAOProfileList( List<MJ.CredentialAlignmentObject> input )
        {
            List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
            MC.CredentialAlignmentObjectProfile entity = new MC.CredentialAlignmentObjectProfile();

            if ( input == null || input.Count == 0 )
                return output;

            foreach ( MJ.CredentialAlignmentObject item in input )
            {
				string targetNodeName = HandleLanguageMap( item.TargetNodeName, currentBaseObject, "TargetNodeName", ref lastLanguageMapString, false );
				//18-12-06 mp - not sure if we should skip if targetNode is missing? We don't do anything with it directly.
				//item.TargetNode != null &&
				if ( item != null && (  !string.IsNullOrEmpty( targetNodeName ) ) )
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
		}	//

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
                    njp.AssertedByList = MapOrganizationReferenceGuids( jp.AssertedBy, ref status );
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
                        status.AddWarning( "Warning - invalid/incomplete jurisdiction. " );
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
                profile.ProcessingAgentUid = MapOrganizationReferencesGuid( input.ProcessingAgent, ref status );

                //targets
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    profile.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
                    profile.TargetAssessmentIds = MapEntityReferences( input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
                if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
                    profile.TargetLearningOpportunityIds = MapEntityReferences( input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

                //if ( input.TargetCompetencyFramework != null && input.TargetCompetencyFramework.Count > 0 )
                //

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
        public Guid MapOrganizationReferencesGuid( List<string> input, ref SaveStatus status )
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
                    return ResolveOrgBlankNodeToGuid( node, ref status, ref isResolved );
                }
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
                    orgRef = ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
                    //break;
                }
                else if ( target.StartsWith( "_:" ) )
                {
                    var node = GetBlankNode( target );
                    orgRef = ResolveOrgBlankNodeToGuid( node, ref status, ref isResolved );
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
        private Guid ResolveOrgBlankNodeToGuid( BNode input, ref SaveStatus status, ref bool isResolved )
        {
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
            string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
            if ( string.IsNullOrWhiteSpace( name ) )
                status.AddError( "Invalid OrganizationBase, missing name" );
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid OrganizationBase, missing SubjectWebpage" );
            //any messages return
            if ( start < status.Messages.Count )
                return entityRef;

            //look up by name, subject webpage

            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            MC.Organization org = OrganizationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
            if ( org != null && org.Id > 0 )
                return org.RowId;

            //if not found, need to create!!!
            //TODO _ also handle type!!!!
            org = new workIT.Models.Common.Organization()
            {
                Name = name,
                Description = desc,
                SubjectWebpage = input.SubjectWebpage,
                SocialMediaPages = MapToTextValueProfile( input.SocialMedia ),
                RowId = Guid.NewGuid()
            };
            if ( input.Type.ToLower() == "ceterms:qacredentialorganization" )
            {
                org.ISQAOrganization = true;
            }
            if ( new OrganizationManager().AddBaseReference( org, ref status ) > 0 )
                entityRef = org.RowId;

            return entityRef;
        }
        #endregion


        #region  Entities

        /// <summary>
        /// Map a List of EntityBases to a List of Guids
        /// </summary>
        /// <param name="input"></param>
        /// <param name="entityTypeId">If zero, look up by ctid, or </param>
        /// <param name="status"></param>
        /// <returns></returns>
        public List<Guid> MapEntityReferenceGuids( List<string> input, int entityTypeId, ref SaveStatus status )
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
                //don't always know the type, especially for org accrediting something.
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
                    entityRef = ResolveEntityRegistryAtIdToGuid( registryAtId, entityTypeId, ref status );
                    //break;
                }
                else if ( target.StartsWith( "_:" ) )
                {
                    //should be a blank node
                    var node = GetBlankNode( target );
                    string name = HandleBNodeLanguageMap( node.Name, "blank node name", true );
                    string desc = HandleBNodeLanguageMap( node.Description, "blank node desc", true );
                    if (node == null || string.IsNullOrWhiteSpace(name) ) 
                    {
                        status.AddError( string.Format( "A Blank node was not found for bid of: {0}. ", target ));
                        continue;
                        //return entityRefs;
                    }
                    if ( origEntityTypeId == 0 )
                        entityTypeId = GetBlankNodeEntityType( node );
                    //if type present,can use
                    entityRef = ResolveEntityBaseToGuid( node, entityTypeId, ref status );
                }
                if ( BaseFactory.IsGuidValid( entityRef ) )
                    entityRefs.Add( entityRef );
            }

            return entityRefs;
        }
        private Guid ResolveEntityRegistryAtIdToGuid( string registryAtId, int entityTypeId, ref SaveStatus status )
        {
            bool isResolved = false;
            Guid entityRef = new Guid();
            if ( !string.IsNullOrWhiteSpace( registryAtId ) )
            {
                entityRef = ResolutionServices.ResolveEntityByRegistryAtIdToGuid( registryAtId, entityTypeId, ref status, ref isResolved );
            }

            return entityRef;
        }

        private Guid ResolveEntityBaseToGuid( BNode input, int entityTypeId, ref SaveStatus status )
        {
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
            string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );

            if ( string.IsNullOrWhiteSpace( name ) )
                status.AddError( "Invalid EntityBase/BNode, missing name" );

            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid EntityBase, missing SubjectWebpage" );

            if ( start < status.Messages.Count )
                return entityRef;
            //look up by subject webpage
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            //if ()
  
            MC.Entity entity = EntityManager.Entity_Cache_Get( entityTypeId, name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
                return entity.EntityUid;

            int entityRefId = 0;
            //if not found, then create
            if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                entityRefId = ResolveBaseEntityAsCredential( input, ref entityRef, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_ORGANIZATION )
                entityRefId = ResolveBlankNodeAsOrganization( input, ref entityRef, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
                entityRefId = ResolveBaseEntityAsAssessment( input, ref entityRef, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
                entityRefId = ResolveBaseEntityAsLopp( input, ref entityRef, ref status );
            else
            {
                //unexpected, should not have entity references for manifests
                status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, input.SubjectWebpage ) );
            }
            return entityRef;
        }

        /// <summary>
        /// Map list of EntityBase items to a list of integer Ids.
        /// These Ids will be used for child records under an Entity
        /// </summary>
        /// <param name="input"></param>
        /// <param name="entityTypeId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public List<int> MapEntityReferences( List<string> input, int entityTypeId, ref SaveStatus status )
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
                    LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: EntityTypeId: {0}, CtdlId: {1} ", entityTypeId, target ) );
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
                    LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target bnode: {1} ", entityTypeId, target ) );
                    var node = GetBlankNode( target );
                    //if type present,can use
                    entityRef = ResolveEntityBaseToInt( node, entityTypeId, ref status );
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
            int entityRef = 0;
            if ( !string.IsNullOrWhiteSpace( registryAtId ) )
            {
                entityRef = ResolutionServices.ResolveEntityByRegistryAtId( registryAtId, entityTypeId, ref status, ref isResolved );
            }

            return entityRef;
        }
        private int ResolveEntityBaseToInt( BNode input, int entityTypeId, ref SaveStatus status )
        {

            int entityRefId = 0;
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            if ( input.Name.Count() == 0 )
                status.AddError( "Invalid EntityBase/BNode, missing name" );
           
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid EntityBase, missing SubjectWebpage" );

            if ( start < status.Messages.Count )
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
                entityRefId = ResolveBlankNodeAsOrganization( input, ref entityRef, ref status );
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
            MC.Credential entity = CredentialManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                //need additional check for missing credential type id
                if (entity.CredentialTypeId == 0)
                {
                    //need to update!
                    entity.CredentialTypeSchema = input.Type;
                    if ( new CredentialManager().UpdateBaseReferenceCredentialType( entity, ref status ) == 0 )
                    {
                        //any errors should already be in status
                    }
                }
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.Common.Credential()
            {
                Name = name,
                SubjectWebpage = input.SubjectWebpage,
                Description = desc,
                CredentialTypeSchema = input.Type
            };
            if ( new CredentialManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

            return entityRefId;
        }
        private int ResolveBlankNodeAsOrganization( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;
            string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
            string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
            var entity = OrganizationManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new MC.Organization()
            {
                Name = name,
                SubjectWebpage = input.SubjectWebpage,
                SocialMediaPages = MapToTextValueProfile( input.SocialMedia ),
                Description = desc
            };
            if ( new OrganizationManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

            return entityRefId;
        }
        private int ResolveBaseEntityAsAssessment( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;
            string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
            string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
            WPM.AssessmentProfile entity = AssessmentManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.ProfileModels.AssessmentProfile()
            {
                Name = name,
                SubjectWebpage = input.SubjectWebpage,
                Description = desc
            };
            if ( new AssessmentManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

            return entityRefId;
        }
        private int ResolveBaseEntityAsLopp( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;
            string name = HandleBNodeLanguageMap( input.Name, "blank node name", true );
            string desc = HandleBNodeLanguageMap( input.Description, "blank node desc", true );
            WPM.LearningOpportunityProfile entity = LearningOpportunityManager.GetByName_SubjectWebpage( name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.ProfileModels.LearningOpportunityProfile()
            {
                Name = name,
                SubjectWebpage = input.SubjectWebpage,
                Description = desc
            };
            if ( new LearningOpportunityManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

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

                var profile = new WPM.ConditionProfile
                {
                    RowId = Guid.NewGuid()
                };
                profile.Name = HandleLanguageMap( input.Name, profile, "Name", true );
                LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile Name " + ( profile.Name ?? "no name" ) );
                profile.Description = HandleLanguageMap( input.Description, profile, "Description", true );
                profile.SubjectWebpage = input.SubjectWebpage;
                profile.AudienceLevelType = MapCAOListToEnumermation( input.AudienceLevelType );
                profile.AudienceType = MapCAOListToEnumermation( input.AudienceType );
                profile.DateEffective = MapDate( input.DateEffective, "DateEffective", ref status );

                profile.Condition = MapToTextValueProfile( input.Condition, profile, "Condition", true );
                profile.SubmissionOf = MapToTextValueProfile( input.SubmissionOf, profile, "SubmissionOf", true );
        
                //while the input is a list, we are only handling single
                if ( input.AssertedBy != null && input.AssertedBy.Count > 0 )
                {
                    profile.AssertedByAgent = MapOrganizationReferenceGuids( input.AssertedBy, ref status );
                }

                profile.Experience = input.Experience;
                profile.MinimumAge = input.MinimumAge;
                profile.YearsOfExperience = input.YearsOfExperience;
                profile.Weight = input.Weight;

                profile.CreditUnitTypeDescription = HandleLanguageMap( input.CreditUnitTypeDescription, profile, "CreditUnitTypeDescription", true );
                profile.CreditUnitType = MapCAOToEnumermation( input.CreditUnitType );
                profile.CreditUnitValue = input.CreditUnitValue;
                profile.CreditHourType = HandleLanguageMap( input.CreditHourType, profile, "CreditHourType", true );
                profile.CreditHourValue = input.CreditHourValue;

                if ( input.AlternativeCondition != null & input.AlternativeCondition.Count > 0 )
                    profile.AlternativeCondition = FormatConditionProfile( input.AlternativeCondition, ref status );

                profile.EstimatedCosts = FormatCosts( input.EstimatedCost, ref status );

                //jurisdictions
                profile.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
                profile.ResidentOf = MapToJurisdiction( input.ResidentOf, ref status );

                //targets
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    profile.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
                    profile.TargetAssessmentIds = MapEntityReferences( input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
                if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
                {
                    LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile. Has learning opportunities: " + input.TargetLearningOpportunity.Count.ToString() );
                    profile.TargetLearningOpportunityIds = MapEntityReferences( input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

                    LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile. Has learning opportunities. Mapped to list: " + profile.TargetLearningOpportunityIds.Count.ToString() );
                }

                profile.TargetCompetencies = MapCAOListToCAOProfileList( input.TargetCompetency );
                list.Add( profile );
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
        public List<MC.FinancialAlignmentObject> FormatFinancialAssistance( List<MJ.FinancialAlignmentObject> input, ref SaveStatus status )
        {
            if ( input == null || input.Count == 0 )
                return null;

            var list = new List<MC.FinancialAlignmentObject>();
            foreach ( var item in input )
            {
                var profile = new MC.FinancialAlignmentObject
                {
                    RowId = Guid.NewGuid(),
                    AlignmentType = item.AlignmentType,
                    Framework = item.Framework ?? "",
                    TargetNode = item.TargetNode ?? "",
                    Weight = item.Weight
                };

                profile.FrameworkName = HandleLanguageMap( item.FrameworkName, profile, "FrameworkName", true );
                profile.TargetNodeDescription = HandleLanguageMap( item.TargetNodeDescription, profile, "TargetNodeDescription", true );
                profile.TargetNodeName = HandleLanguageMap( item.TargetNodeName, profile, "TargetNodeName", true );

                profile.AlignmentDate = MapDate( item.AlignmentDate, "AlignmentDate", ref status );
                profile.CodedNotation = item.CodedNotation;
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
                profile.OfferedByAgentUid = MapOrganizationReferencesGuid( input.OfferedBy, ref status );
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    profile.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
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
