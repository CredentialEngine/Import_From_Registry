using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
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
    public class MappingHelperV2
    {
        public List<BNode> entityBlankNodes = new List<BNode>();
        public MappingHelperV2 ()
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
        public int GetBlankNodeEntityType( BNode node)
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
                    Description = item.Description
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
                if ( item != null
                    && ( item.TargetNode != null || !string.IsNullOrEmpty( item.TargetNodeName ) )
                    )
                {
                    output.Items.Add( new workIT.Models.Common.EnumeratedItem()
                    {
                        SchemaName = item.TargetNode ?? "",
                        Name = item.TargetNodeName ?? ""
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
                && ( input.TargetNode != null || !string.IsNullOrEmpty( input.TargetNodeName ) )
                )
            {
                output.Items.Add( new workIT.Models.Common.EnumeratedItem()
                {
                    SchemaName = input.TargetNode ?? "",
                    Name = input.TargetNodeName ?? ""
                } );
            }

            return output;
        }
        public List<MC.CredentialAlignmentObjectProfile> MapCAOListToFramework( List<MJ.CredentialAlignmentObject> input )
        {
            List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
            MC.CredentialAlignmentObjectProfile entity = new MC.CredentialAlignmentObjectProfile();

            if ( input == null || input.Count == 0 )
                return output;

            foreach ( MJ.CredentialAlignmentObject item in input )
            {
                if ( item != null && !string.IsNullOrEmpty( item.TargetNodeName ) )
                {
                    entity = new MC.CredentialAlignmentObjectProfile()
                    {
                        TargetNode = item.TargetNode,
                        CodedNotation = item.CodedNotation,

                        TargetNodeName = item.TargetNodeName,
                        TargetNodeDescription = item.TargetNodeDescription,

                        FrameworkName = item.FrameworkName,
                        //won't know if url or registry uri
                        //SourceUrl = item.Framework,
                        Weight = item.Weight
                        //Weight = StringtoDecimal( item.Weight )
                    };
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

        public List<MC.CredentialAlignmentObjectProfile> MapCAOListToCompetencies( List<MJ.CredentialAlignmentObject> input )
        {
            List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
            MC.CredentialAlignmentObjectProfile cao = new MC.CredentialAlignmentObjectProfile();

            if ( input == null || input.Count == 0 )
                return output;

            foreach ( MJ.CredentialAlignmentObject item in input )
            {
                if ( item != null && !string.IsNullOrEmpty( item.TargetNodeName ) )
                {
                    cao = new MC.CredentialAlignmentObjectProfile()
                    {
                        TargetNodeName = item.TargetNodeName,
                        TargetNodeDescription = item.TargetNodeDescription,
                        TargetNode = item.TargetNode,
                        CodedNotation = item.CodedNotation,
                        FrameworkName = item.FrameworkName,
                        //FrameworkUrl = item.Framework,
                        Weight = item.Weight
                        //Weight = StringtoDecimal(item.Weight)
                    };
                    //Framework willl likely be a registry url, so should be saved as FrameworkUri. The SourceUrl will be added from a download of the actual framework
                    if ( !string.IsNullOrWhiteSpace( item.Framework ) )
                    {
						if ( item.Framework.ToLower().IndexOf( "credentialengineregistry.org/resources/ce-" ) == -1
							&& item.Framework.ToLower().IndexOf( "credentialengineregistry.org/graph/ce-" ) == -1 )
						{
                            cao.SourceUrl = item.Framework;
                        }
                        else
                        {
                            cao.FrameworkUri = item.Framework;
                        }
                    }
                    output.Add( cao );
                }

            }
            return output;
        }
        public List<WPM.TextValueProfile> MapCAOListToTextValueProfile( List<MJ.CredentialAlignmentObject> input, int categoryId )
        {
            List<WPM.TextValueProfile> list = new List<WPM.TextValueProfile>();
            if ( input == null || input.Count == 0 )
                return list;

            foreach ( MJ.CredentialAlignmentObject item in input )
            {
                if ( item != null && !string.IsNullOrEmpty( item.TargetNodeName ) )
                {
                    list.Add( new WPM.TextValueProfile()
                    {
                        CategoryId = categoryId,
                        //TextTitle = item.TargetNodeName,
                        TextValue = item.TargetNodeName
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

            //MC.Address jaddress = new InputAddress();
            MC.ContactPoint cp = new MC.ContactPoint();

            foreach ( var item in addresses )
            {
                output = new MC.Address();
                //output.Name = item.Name;
                //address is defined as an array. Realistically, should be one
                //foreach ( var a in item )
                //{
                output = MapAddress( item );

                if ( item.ContactPoint != null && item.ContactPoint.Count > 0 )
                {
                    foreach ( var cpi in item.ContactPoint )
                    {
                        cp = new MC.ContactPoint()
                        {
                            Name = cpi.Name,
                            ContactType = cpi.ContactType
                        };
                        //cp.ContactOption = MapListToString( cpi.ContactOption );
                        cp.PhoneNumbers = cpi.PhoneNumbers;
                        cp.Emails = cpi.Emails;
                        cp.SocialMediaPages = cpi.SocialMediaPages;

                        output.ContactPoint.Add( cp );
                    }
                }
                list.Add( output );
                //}
            }

            return list;
        } //

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
                            Name = cpi.Name,
                            ContactType = cpi.ContactType
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
                njp.Description = jp.Description;
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
                    if ( string.IsNullOrWhiteSpace( jp.Description ) )
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
            gc.Name = input.Name;
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
                Name = input.Name,
                Address1 = input.StreetAddress,
                //Address2 = input.Address2,
                City = input.City,
                Country = input.Country,
                AddressRegion = input.AddressRegion,
                PostalCode = input.PostalCode,
                Latitude = input.Latitude,
                Longitude = input.Longitude
            };

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
                var cp = new WPM.ProcessProfile
                {
                    DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
                    Description = input.Description,
                    ProcessFrequency = input.ProcessFrequency,
                    ProcessMethodDescription = input.ProcessMethodDescription,
                    ProcessStandardsDescription = input.ProcessStandardsDescription,
                    ScoringMethodDescription = input.ScoringMethodDescription,
                    ScoringMethodExampleDescription = input.ScoringMethodExampleDescription,
                    VerificationMethodDescription = input.VerificationMethodDescription
                };

                cp.SubjectWebpage = input.SubjectWebpage;
                cp.ExternalInputType = MapCAOListToEnumermation( input.ExternalInputType );
                cp.ProcessMethod = input.ProcessMethod ?? "";
                cp.ProcessStandards = input.ProcessStandards ?? "";
                cp.ScoringMethodExample = input.ScoringMethodExample ?? "";
                cp.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );

                //while the profiles is a list, we are only handling single
                cp.ProcessingAgentUid = MapOrganizationReferencesGuid( input.ProcessingAgent, ref status );

                //targets
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    cp.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
                    cp.TargetAssessmentIds = MapEntityReferences( input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
                if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
                    cp.TargetLearningOpportunityIds = MapEntityReferences( input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

                //if ( input.TargetCompetencyFramework != null && input.TargetCompetencyFramework.Count > 0 )
                //

                output.Add( cp );
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
                    return ResolveOrgBaseToGuid( node, ref status, ref isResolved );
                }
            }

            return orgRef;
        }

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
                    orgRef = ResolveOrgBaseToGuid( node, ref status, ref isResolved );
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
        /// Analyze a base organization: check if exists, by subject webpage. 
        /// If found return Guid, otherwise create new base
        /// </summary>
        /// <param name="input"></param>
        /// <param name="status"></param>
        /// <param name="isResolved"></param>
        /// <returns></returns>
        private Guid ResolveOrgBaseToGuid( BNode input, ref SaveStatus status, ref bool isResolved )
        {
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            if ( string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid OrganizationBase, missing name" );
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid OrganizationBase, missing SubjectWebpage" );
            //any messages return
            if ( start < status.Messages.Count )
                return entityRef;

            //look up by name, subject webpage
            
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            MC.Organization org = OrganizationManager.GetByName_SubjectWebpage( input.Name, input.SubjectWebpage);
            if ( org != null && org.Id > 0 )
                return org.RowId;

            //if not found, need to create!!!
            //TODO _ also handle type!!!!
            org = new workIT.Models.Common.Organization()
            {
                Name = input.Name,
                Description = input.Description,
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
                if ( target.StartsWith("http" ))
                {
                    registryAtId = target;
                    entityRef = ResolveEntityRegistryAtIdToGuid( registryAtId, entityTypeId, ref status );
                    //break;
                }
                else if ( target.StartsWith( "_:" ))
                {
                    //should be a blank node
                    var node = GetBlankNode( target );
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
            if ( string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid EntityBase, missing name" );
            
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid EntityBase, missing SubjectWebpage" );

            if ( start < status.Messages.Count )
                return entityRef;
            string url = input.SubjectWebpage;
            //look up by subject webpage
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            //if ()
            MC.Entity entity = EntityManager.Entity_Cache_Get( entityTypeId, input.Name, url );
            if ( entity != null && entity.Id > 0 )
                return entity.EntityUid;

            int entityRefId = 0;
            //if not found, then create
            if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                entityRefId = ResolveBaseEntityAsCredential( input, ref entityRef, ref status );
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
            if ( string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid EntityBase, missing name" );
            //if ( string.IsNullOrWhiteSpace( input.Description ) )
            //    status.AddError( "Invalid EntityBase, missing Description" );
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage ) )
                status.AddError( "Invalid EntityBase, missing SubjectWebpage" );

            if ( start < status.Messages.Count )
                return entityRefId;

            string swp = input.SubjectWebpage;
            //look up by subject webpage
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                entityRefId = ResolveBaseEntityAsCredential( input, ref entityRef, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
                entityRefId = ResolveBaseEntityAsAssessment( input, ref entityRef, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
                entityRefId = ResolveBaseEntityAsLopp( input, ref entityRef, ref status );
            else
            {
                //unexpected, should not have entity references for manifests
                status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, swp ) );
            }


            return entityRefId;
        }
        private static int ResolveBaseEntityAsCredential( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;

            MC.Credential entity = CredentialManager.GetByName_SubjectWebpage( input.Name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.Common.Credential()
            {
                Name = input.Name,
                SubjectWebpage = input.SubjectWebpage,
                Description = input.Description,
                CredentialTypeSchema = input.Type
            };
            if ( new CredentialManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

            return entityRefId;
        }
        private static int ResolveBaseEntityAsAssessment( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;

            WPM.AssessmentProfile entity = AssessmentManager.GetByName_SubjectWebpage( input.Name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.ProfileModels.AssessmentProfile()
            {
                Name = input.Name,
                SubjectWebpage = input.SubjectWebpage,
                Description = input.Description
            };
            if ( new AssessmentManager().AddBaseReference( entity, ref status ) > 0 )
            {
                entityUid = entity.RowId;
                entityRefId = entity.Id;
            }

            return entityRefId;
        }
        private static int ResolveBaseEntityAsLopp( BNode input, ref Guid entityUid, ref SaveStatus status )
        {
            int entityRefId = 0;

            WPM.LearningOpportunityProfile entity = LearningOpportunityManager.GetByName_SubjectWebpage( input.Name, input.SubjectWebpage );
            if ( entity != null && entity.Id > 0 )
            {
                entityUid = entity.RowId;
                return entity.Id;
            }
            entity = new workIT.Models.ProfileModels.LearningOpportunityProfile()
            {
                Name = input.Name,
                SubjectWebpage = input.SubjectWebpage,
                Description = input.Description
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
                var cp = new WPM.ConditionProfile();
                cp.Name = input.Name;
                LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile Name " + ( input.Name ?? "no name" ) );
                cp.Description = input.Description;
                cp.SubjectWebpage = input.SubjectWebpage;
                cp.AudienceLevelType = MapCAOListToEnumermation( input.AudienceLevelType );
                cp.AudienceType = MapCAOListToEnumermation( input.AudienceType );
                cp.DateEffective = MapDate( input.DateEffective, "DateEffective", ref status );
                cp.Condition = MapToTextValueProfile( input.Condition );
                cp.SubmissionOf = MapToTextValueProfile( input.SubmissionOf );

                //while the input is a list, we are only handling single
                if ( input.AssertedBy != null && input.AssertedBy.Count > 0)
                {
                    cp.AssertedByAgent = MapOrganizationReferenceGuids( input.AssertedBy, ref status );

                }

                cp.Experience = input.Experience;
                cp.MinimumAge = input.MinimumAge;
                cp.YearsOfExperience = input.YearsOfExperience;
                cp.Weight = input.Weight;

                cp.CreditUnitTypeDescription = input.CreditUnitTypeDescription;
                cp.CreditUnitType = MapCAOToEnumermation( input.CreditUnitType );
                cp.CreditUnitValue = input.CreditUnitValue;
                cp.CreditHourType = input.CreditHourType;
                cp.CreditHourValue = input.CreditHourValue;

                if ( input.AlternativeCondition != null & input.AlternativeCondition.Count > 0 )
                    cp.AlternativeCondition = FormatConditionProfile( input.AlternativeCondition, ref status );

                cp.EstimatedCosts = FormatCosts( input.EstimatedCost, ref status );

                //jurisdictions
                cp.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );
                cp.ResidentOf = MapToJurisdiction( input.ResidentOf, ref status );

                //targets
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    cp.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                if ( input.TargetAssessment != null && input.TargetAssessment.Count > 0 )
                    cp.TargetAssessmentIds = MapEntityReferences( input.TargetAssessment, CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE, ref status );
                if ( input.TargetLearningOpportunity != null && input.TargetLearningOpportunity.Count > 0 )
                {
                    LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile. Has learning opportunities: " + input.TargetLearningOpportunity.Count.ToString() );
                    cp.TargetLearningOpportunityIds = MapEntityReferences( input.TargetLearningOpportunity, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, ref status );

                    LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile. Has learning opportunities. Mapped to list: " + cp.TargetLearningOpportunityIds.Count.ToString() );
                }
           
                cp.TargetCompetencies = MapCAOListToCompetencies( input.TargetCompetency );
                list.Add( cp );
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
                var cp = new WPM.RevocationProfile
                {
                    Description = input.Description,
                    DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
                    Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status ),
                    RevocationCriteriaDescription = input.RevocationCriteriaDescription,
                    RevocationCriteriaUrl = input.RevocationCriteria,
                };
                list.Add( cp );
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
                var cp = new WPM.CostProfileMerged
                {
                    Name = item.Name,
                    Description = item.Description,
                    Jurisdiction = MapToJurisdiction( item.Jurisdiction, ref status ),

                    StartDate = item.StartDate,
                    EndDate = item.EndDate,

                    CostDetails = item.CostDetails,
                    Currency = item.Currency,
                    CurrencySymbol = item.Currency,

                    Condition = MapToTextValueProfile( item.Condition ),

                    ProfileName = item.Name,
                    AudienceType = MapCAOListToEnumermation( item.AudienceType ),

                    CostType = MapCAOToEnumermation( item.DirectCostType ),

                    Price = item.Price,

                    PaymentPattern = item.PaymentPattern,
                    ResidencyType = MapCAOListToEnumermation( item.ResidencyType )
                };
                //cp.CostType = MapCAOToEnumermation(item.DirectCostType);

                list.Add( cp );
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
                var fa = new MC.FinancialAlignmentObject
                {
                    AlignmentType = item.AlignmentType,
                    Framework = item.Framework ?? "",
                    FrameworkName = item.FrameworkName,
                    TargetNode = item.TargetNode ?? "",
                    TargetNodeDescription = item.TargetNodeDescription,
                    TargetNodeName = item.TargetNodeName,
                    Weight = item.Weight
                };
                fa.AlignmentDate = MapDate( item.AlignmentDate, "AlignmentDate", ref status );
                fa.CodedNotation = item.CodedNotation;
                list.Add( fa );
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
            var cp = new WPM.DurationProfile();

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
                    cp = new WPM.DurationProfile
                    {
                        Description = item.Description,

                        ExactDuration = FormatDurationItem( item.ExactDuration ),
                        MaximumDuration = FormatDurationItem( item.MaximumDuration ),
                        MinimumDuration = FormatDurationItem( item.MinimumDuration )
                    };
                    // only allow an exact duration or a range, not both 
                    if ( cp.IsRange && cp.ExactDuration != null )
                    {
                        status.AddWarning( "Duration Profile error - provide either an exact duration or a minimum and maximum range, but not both. Defaulting to range" );
                        cp.ExactDuration = null;
                    }
                    list.Add( cp );
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
                var vsp = new WPM.VerificationServiceProfile
                {
                    //properties
                    DateEffective = MapDate( input.DateEffective, "DateEffective", ref status ),
                    Description = input.Description,
                    VerificationMethodDescription = input.VerificationMethodDescription,
                    HolderMustAuthorize = input.HolderMustAuthorize

                };
                vsp.SubjectWebpage = input.SubjectWebpage;
                //VerificationService is hidden in the publisher!
                vsp.VerificationServiceUrl = MapListToString( input.VerificationService );
                vsp.OfferedByAgentUid = MapOrganizationReferencesGuid( input.OfferedBy, ref status );
                if ( input.TargetCredential != null && input.TargetCredential.Count > 0 )
                    vsp.TargetCredentialIds = MapEntityReferences( input.TargetCredential, CodesManager.ENTITY_TYPE_CREDENTIAL, ref status );
                vsp.VerificationDirectory = MapListToString( input.VerificationDirectory );
                vsp.ClaimType = MapCAOListToEnumermation( input.VerifiedClaimType );
                vsp.EstimatedCost = FormatCosts( input.EstimatedCost, ref status );
                vsp.Jurisdiction = MapToJurisdiction( input.Jurisdiction, ref status );

                list.Add( vsp );
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
