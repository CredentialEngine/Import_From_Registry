using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MJ = RA.Models.Json;
using InputAddress1 = RA.Models.Json.Address;
using InputAddress = RA.Models.Json.Place;
using workIT.Models;
using MC = workIT.Models.Common;
using WPM = workIT.Models.ProfileModels;
using workIT.Factories;
using workIT.Utilities;
using workIT.Services;


namespace Import.Services
{
    public class MappingHelper
    {
        #region  mapping IdProperty
        public static string MapIdentityToString( MJ.IdProperty property )
        {
            if ( property == null )
                return "";
            else
                return property.Id;
        }
        public static string MapIdentityListToString( List<MJ.IdProperty> property )
        {
            if ( property == null || property.Count == 0 )
                return "";
            else //assuming these types will only have one entry
                return property[0].Id;
        }

        public static List<string> MapIdentityListToList( List<MJ.IdProperty> property )
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
		public static string MapIdentifierValueListToString( List<MJ.IdentifierValue> property )
		{
			if ( property == null || property.Count == 0 )
				return "";
			//assuming these types will only have one entry
			if ( !string.IsNullOrWhiteSpace( property[ 0 ].Name ) )
				return property[ 0 ].Name;
			else if( !string.IsNullOrWhiteSpace( property[ 0 ].IdentifierValueCode ) )
				return property[ 0 ].IdentifierValueCode;
			else
				return "";
		}
		public void MapIdentifierValueListToTextValueProfile( List<MJ.IdentifierValue> list, List<WPM.TextValueProfile> output )
        {
            if ( list == null || list.Count == 0 )
                return;


            foreach ( var property in list )
            {
                var tvp = new WPM.TextValueProfile
                {
                    TextTitle = property.Name,
                    TextValue = property.IdentifierValueCode
                };
                output.Add( tvp );
            }

        }

        public static List<WPM.Entity_IdentifierValue> MapIdentifierValueList( List<MJ.IdentifierValue> property )
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
        public static List<WPM.TextValueProfile> MapToTextValueProfile( List<string> list )
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

        public static List<WPM.TextValueProfile> MapToTextValueProfile( List<MJ.IdProperty> property )
        {
            List<WPM.TextValueProfile> list = new List<WPM.TextValueProfile>();
            if ( property == null || property.Count == 0 )
                return list;

            foreach ( var item in property )
            {
                var tvp = new WPM.TextValueProfile { TextValue = item.Id };
                list.Add( tvp );
            }
            return list;
        } //

        public static List<WPM.TextValueProfile> MapToTextValueProfile( List<MJ.OrganizationBase> property )
        {
            List<WPM.TextValueProfile> list = new List<WPM.TextValueProfile>();
            if ( property == null || property.Count == 0 )
                return list;

            foreach ( var item in property )
            {
                var tvp = new WPM.TextValueProfile { TextValue = item.CtdlId };
                list.Add( tvp );
            }
            return list;
        } //
        #endregion

        #region  CredentialAlignmentObject
        public static MC.Enumeration MapCAOListToEnumermation( List<MJ.CredentialAlignmentObject> input )
        {
            //TBD = do we need anything for emumeration, or just items?
            MC.Enumeration output = new workIT.Models.Common.Enumeration();
            if ( input == null || input.Count == 0 )
                return output;

            foreach ( MJ.CredentialAlignmentObject item in input )
            {
                if ( item != null 
					&& (item.TargetNode != null || !string.IsNullOrEmpty( item.TargetNodeName ) )
					)
                {
					output.Items.Add( new workIT.Models.Common.EnumeratedItem()
					{
						SchemaName = item.TargetNode != null ? item.TargetNode : "",
						Name = item.TargetNodeName ?? ""
					} );
                }

            }
            return output;
        }
		public static MC.Enumeration MapCAOToEnumermation( MJ.CredentialAlignmentObject input )
		{
			//TBD = do we need anything for emumeration, or just items?
			MC.Enumeration output = new workIT.Models.Common.Enumeration();
			if ( input == null  )
				return output;

			if ( input != null
				&& ( input.TargetNode != null || !string.IsNullOrEmpty( input.TargetNodeName ) )
				)
			{
				output.Items.Add( new workIT.Models.Common.EnumeratedItem()
				{
					SchemaName = input.TargetNode != null ? input.TargetNode : "",
					Name = input.TargetNodeName ?? ""
				} );
			}

			return output;
		}
		public static List<MC.CredentialAlignmentObjectProfile> MapCAOListToFramework( List<MJ.CredentialAlignmentObject> input )
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();

			if ( input == null || input.Count == 0 )
				return output;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item != null && !string.IsNullOrEmpty( item.TargetNodeName ) )
				{
					output.Add( new MC.CredentialAlignmentObjectProfile()
					{
						TargetNode = item.TargetNode,
						CodedNotation = item.CodedNotation,

						TargetNodeName = item.TargetNodeName,
						TargetNodeDescription = item.TargetNodeDescription,

						FrameworkName = item.FrameworkName,
						FrameworkUrl = item.Framework,
						Weight = item.Weight
						//Weight = StringtoDecimal( item.Weight )
					} );
				}

			}
			return output;
		}

		public static List<MC.CredentialAlignmentObjectProfile> MapCAOListToCompetencies( List<MJ.CredentialAlignmentObject> input )
		{
			List<MC.CredentialAlignmentObjectProfile> output = new List<workIT.Models.Common.CredentialAlignmentObjectProfile>();
			
			if ( input == null || input.Count == 0 )
				return output;

			foreach ( MJ.CredentialAlignmentObject item in input )
			{
				if ( item != null && !string.IsNullOrEmpty(item.TargetNodeName) )
				{
					output.Add( new MC.CredentialAlignmentObjectProfile()
					{
						TargetNodeName = item.TargetNodeName,
						TargetNodeDescription = item.TargetNodeDescription,
						TargetNode = item.TargetNode ,
						CodedNotation = item.CodedNotation,
						FrameworkName = item.FrameworkName,
						FrameworkUrl = item.Framework,
						Weight = item.Weight
						//Weight = StringtoDecimal(item.Weight)
					} );
				}

            }
            return output;
        }
        public static List<WPM.TextValueProfile> MapCAOListToTextValueProfile( List<MJ.CredentialAlignmentObject> input, int categoryId )
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
        public static string MapListToString( List<string> property )
        {

            if ( property == null || property.Count == 0 )
                return "";
            //assuming only handling first one
            return property[0];
        } //
        #endregion

        #region  Addressess, contact point, jurisdiction

		/// <summary>
		/// oCT. 20, 2017 - AvailableAt is now the same as address. 
		/// Not sure of its fate
		/// </summary>
		/// <param name="addresses"></param>
		/// <param name="status"></param>
		/// <returns></returns>
        public static List<MC.Address> FormatAvailableAtAddresses( List<MJ.Place> addresses, ref SaveStatus status )
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
                            cp.ContactOption = MapListToString( cpi.ContactOption );
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

        public static List<MC.Address> FormatAddresses( List<InputAddress> addresses, ref SaveStatus status )
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
                        cp.ContactOption = MapListToString( cpi.ContactOption );
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

        public static List<MC.ContactPoint> FormatContactPoints( List<MJ.ContactPoint> contactPoints, ref SaveStatus status )
        {
            List<MC.ContactPoint> list = new List<MC.ContactPoint>();
            if ( contactPoints == null || contactPoints.Count == 0 )
                return list;

            MC.ContactPoint cp = new MC.ContactPoint();

            foreach ( var a in contactPoints )
            {
                cp = new MC.ContactPoint()
                {
                    Name = a.Name,
                    ContactType = a.ContactType
                };
                cp.ContactOption = MapListToString( a.ContactOption );
                cp.PhoneNumbers = a.PhoneNumbers;
                cp.Emails = a.Emails;
                cp.SocialMediaPages = a.SocialMediaPages;
				if (cp.PhoneNumber.Count > 0
					|| cp.Email.Count > 0
					|| cp.SocialMediaPages.Count > 0)
					list.Add( cp );
            }

            return list;
        } //

        public static List<MC.JurisdictionProfile> MapToJurisdiction( List<MJ.JurisdictionProfile> jps, ref SaveStatus status )
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
					njp.AssertedBy = MapOrganizationReferenceGuidFromObject( jp.AssertedBy, ref status );
					//assertedByReference = jp.AssertedBy.ToString();
					//if ( assertedByReference.IndexOf( "@id" ) > -1 )
					//{
					//	njp.AssertedBy = MapOrganizationReferenceGuid( jp.AssertedBy, ref status );

					//}
					//else
					//{
					//	//org references
					//	//need to determine the json pattern
					//}
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

		private static MC.GeoCoordinates ResolveGeoCoordinates( MJ.Place input )
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


			} else
			{
				//can we have different lat/lng for the same geoUri?
				gc.Latitude = input.Latitude;
				gc.Longitude = input.Longitude;

				
			}
			return gc;
		}
        private static MC.Address MapAddress( InputAddress input )
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

		private static MC.Address MapAddress( MJ.AvailableAt input )
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
		public static List<WPM.ProcessProfile> FormatProcessProfile( List<MJ.ProcessProfile> profiles, ref SaveStatus status )
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
                cp.ProcessMethod = input.ProcessMethod != null ? input.ProcessMethod : "";
                cp.ProcessStandards = input.ProcessStandards != null ? input.ProcessStandards : "";
                cp.ScoringMethodExample = input.ScoringMethodExample != null ? input.ScoringMethodExample : "";
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

        public static string MapDate( string date, string dateName, ref SaveStatus status, bool doingReasonableCheck = true )
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
        public static Guid MapOrganizationReferencesGuid( List<MJ.OrganizationBase> input, ref SaveStatus status )
        {
            //not sure if isResolved is necessary
            bool isResolved = false;

            Guid orgRef = new Guid();
            string registryAtId = "";
            //var or = new List<RA.Models.Input.OrganizationReference>();
            if ( input == null || input.Count < 1 )
                return orgRef;

            //just take first one
            foreach ( var target in input )
            {
                //determine if just Id, or base
                if ( !string.IsNullOrWhiteSpace( target.CtdlId ) )
                {
                    registryAtId = target.CtdlId;
                    return ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
                    //break;
                }
                else
                {
                    //should be a org reference
                    //if type present,can use
                    return ResolveOrgBaseToGuid( target, ref status, ref isResolved );
                }
            }

            return orgRef;
        }


		public static Guid MapOrganizationReferenceGuidFromObject( object orgReference, ref SaveStatus status )
		{

			try
			{
				var output = JsonConvert.SerializeObject( orgReference );
				//map to org base
				//MJ.OrganizationBase input = JsonConvert.DeserializeObject<MJ.OrganizationBase>( output );

				List<MJ.OrganizationBase> items = JsonConvert.DeserializeObject<List<MJ.OrganizationBase>>( output );
				if ( items != null && items.Count > 0)
				{
					return MapOrganizationReferenceGuid( items[0], ref status );
				}
				//return MapOrganizationReferenceGuid( input, ref status );
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( "MappingHelper.MapOrganizationReferenceGuidFromObject(object orgReference), objectString: {0}", orgReference.ToString() ) );
				status.AddError( string.Format( "MappingHelper.MapOrganizationReferenceGuidFromObject(object input), objectString: {0}", orgReference.ToString() ) );
			}

			return new Guid(); ;
		}
		public static Guid MapOrganizationReferenceGuid( MJ.OrganizationBase input, ref SaveStatus status )
        {
            //not sure if isResolved is necessary
            bool isResolved = false;

            Guid orgRef = new Guid();
            string registryAtId = "";
            if ( input == null )
                return orgRef;

            //determine if just Id, or base
            if ( !string.IsNullOrWhiteSpace( input.CtdlId ) )
            {
                registryAtId = input.CtdlId;
                return ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
                //break;
            }
            else
            {
                //should be a org reference
                //if type present,can use
                return ResolveOrgBaseToGuid( input, ref status, ref isResolved );
            }

        }

        /// <summary>
        /// Input will be of format:
        /// {[  {    "@id": "http://lr-staging.learningtapestry.com/resources/ce-8c5e42ea-724a-40e2-b082-efdc2299205e"  }]}
        /// 
        /// </summary>
        /// <param name="input">object with json string</param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static Guid MapOrganizationReferenceGuid( object input, ref SaveStatus status )
        {
            string registryAtId = "";
            bool isResolved = false;
            Guid entityRef = new Guid();
            string objectString = input.ToString().Trim();
            try
            {
                //add a property name for the DeserializeObject
                objectString = objectString.Replace( "[", "IdList:[ " );
                if ( objectString.IndexOf( "{" ) > 0 )
                    objectString = "{ " + objectString + "}";

                EntityReferenceHelper data = Newtonsoft.Json.JsonConvert.DeserializeObject<EntityReferenceHelper>( objectString );

                //extract the @Id
                if ( data != null && data.IdList != null && data.IdList.Count > 0 )
                {
                    //this method only cares about the first entry
                    registryAtId = data.IdList[0].CtdlId;
                    return ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );

                }
            }
            catch ( Exception ex )
            {
                LoggingHelper.LogError( ex, string.Format( "MappingHelper.MapOrganizationReferenceGuid(object input), objectString: {0}", objectString ) );
                status.AddError( string.Format( "MappingHelper.MapOrganizationReferenceGuid(object input), objectString: {0}", objectString ) );
            }
            return entityRef;
        }


		public static List<Guid> MapOrganizationReferenceGuids( List<MJ.OrganizationBase> input, ref SaveStatus status )
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
                //determine if just Id, or base
                if ( !string.IsNullOrWhiteSpace( target.CtdlId ) )
                {
                    registryAtId = target.CtdlId;
                    orgRef = ResolveOrgRegistryAtIdToGuid( registryAtId, ref status, ref isResolved );
                    //break;
                }
                else
                {
                    //should be a org reference
                    //if type present,can use
                    orgRef = ResolveOrgBaseToGuid( target, ref status, ref isResolved );
                }
                if ( BaseFactory.IsGuidValid( orgRef ) )
                    orgRefs.Add( orgRef );
            }

            return orgRefs;
        }
        private static Guid ResolveOrgRegistryAtIdToGuid( string registryAtId, ref SaveStatus status, ref bool isResolved )
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
        private static Guid ResolveOrgBaseToGuid( MJ.OrganizationBase input, ref SaveStatus status, ref bool isResolved )
        {
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            if ( string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid OrganizationBase, missing name" );
            //if ( string.IsNullOrWhiteSpace( input.Description ) )
            //    status.AddWarning( "Invalid OrganizationBase, missing Description" );
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage )  )
                status.AddError( "Invalid OrganizationBase, missing SubjectWebpage" );
            //any messages return
            if ( start < status.Messages.Count )
                return entityRef;

            //look up by subject webpage
            string url = input.SubjectWebpage;
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            MC.Organization org = OrganizationManager.GetBySubjectWebpage( url );
            if ( org != null && org.Id > 0 )
                return org.RowId;

            //if not found, need to create!!!
            //TODO _ also handle type!!!!
            org = new workIT.Models.Common.Organization()
            {
                Name = input.Name,
                Description = input.Description,
                SubjectWebpage = input.SubjectWebpage,
                SocialMediaPages = MappingHelper.MapToTextValueProfile( input.SocialMedia ),
                RowId = Guid.NewGuid()
            };
            if ( new OrganizationManager().AddOrganizationReference( org, ref status ) > 0 )
                entityRef = org.RowId;

            return entityRef;
        }
       #endregion


        #region  Entities
        /// <summary>
        /// Handle mapping entity ref to a single reference
        /// NOTE: need to handle possibility of org references!!
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Guid MapEntityReferenceGuid( List<MJ.EntityBase> input, int entityTypeId, ref SaveStatus status )
        {

            Guid entityRef = new Guid();
            string registryAtId = "";
            //var or = new List<RA.Models.Input.EntityReference>();
            if ( input == null || input.Count < 1 )
                return entityRef;

            //just take first one
            foreach ( var target in input )
            {
                //determine if just Id, or base
                if ( !string.IsNullOrWhiteSpace( target.CtdlId ) )
                {
                    registryAtId = target.CtdlId;
                    return ResolveEntityRegistryAtIdToGuid( registryAtId, entityTypeId, ref status );
                    //break;
                }
                else
                {
                    //should be a org reference
                    //if type present,can use
                    return ResolveEntityBaseToGuid( target, entityTypeId, ref status );
                }
            }

            return entityRef;
        }

        /// <summary>
        /// Map a List of EntityBases to a List of Guids
        /// </summary>
        /// <param name="input"></param>
        /// <param name="entityTypeId">If zero, look up by ctid, or </param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static List<Guid> MapEntityReferenceGuids( List<MJ.EntityBase> input, int entityTypeId, ref SaveStatus status )
        {

            Guid entityRef = new Guid();
            List<Guid> entityRefs = new List<Guid>();
            string registryAtId = "";
            //var or = new List<RA.Models.Input.EntityReference>();
            if ( input == null || input.Count < 1 )
                return entityRefs;

            if ( entityTypeId == 0 )
            {
                //attempt to get type from resource
            }

            //just take first one
            foreach ( var target in input )
            {
                //determine if just Id, or base
                if ( !string.IsNullOrWhiteSpace( target.CtdlId ) )
                {
                    registryAtId = target.CtdlId;
                    entityRef = ResolveEntityRegistryAtIdToGuid( registryAtId, entityTypeId, ref status );
                    //break;
                }
                else
                {
                    //should be a org reference
                    //if type present,can use
                    entityRef = ResolveEntityBaseToGuid( target, entityTypeId, ref status );
                }
                if ( BaseFactory.IsGuidValid( entityRef ) )
                    entityRefs.Add( entityRef );
            }

            return entityRefs;
        }
        private static Guid ResolveEntityRegistryAtIdToGuid( string registryAtId, int entityTypeId, ref SaveStatus status )
        {
            bool isResolved = false;
            Guid entityRef = new Guid();
            if ( !string.IsNullOrWhiteSpace( registryAtId ) )
            {
                entityRef = ResolutionServices.ResolveEntityByRegistryAtIdToGuid( registryAtId, entityTypeId, ref status, ref isResolved );
            }

            return entityRef;
        }

        private static Guid ResolveEntityBaseToGuid( MJ.EntityBase input, int entityTypeId, ref SaveStatus status )
        {
            Guid entityRef = new Guid();
            int start = status.Messages.Count;
            if ( !string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid EntityBase, missing name" );
            //if ( !string.IsNullOrWhiteSpace( input.Description ) )
            //    status.AddError( "Invalid EntityBase, missing Description" );
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
				entityRef = entity.EntityUid;

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
        public static List<int> MapEntityReferences( List<MJ.EntityBase> input, int entityTypeId, ref SaveStatus status )
        {
            int entityRef = 0;
            List<int> entityRefs = new List<int>();
            string registryAtId = "";
            //var or = new List<RA.Models.Input.EntityReference>();
            if ( input == null || input.Count < 1 )
                return entityRefs;

            //just take first one
            foreach ( var target in input )
            {
                entityRef = 0;
                //determine if just Id, or base
                if ( !string.IsNullOrWhiteSpace( target.CtdlId ) )
                {
					LoggingHelper.DoTrace( 6, string.Format("MappingHelper.MapEntityReferences: EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target.CtdlId ) );
					registryAtId = target.CtdlId;
                    entityRef = ResolveEntityRegistryAtId( registryAtId, entityTypeId, ref status );
					if ( entityRef == 0)
					{
						LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: FAILED TO RESOLVE EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target.CtdlId ) );
					}
                    //break;
                }
                else
                {
					LoggingHelper.DoTrace( 6, string.Format( "MappingHelper.MapEntityReferences: EntityReference EntityTypeId: {0}, target.CtdlId: {1} ", entityTypeId, target.CtdlId ) );
					//if type present,can use
					entityRef = ResolveEntityBaseToInt( target, entityTypeId, ref status );
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
		public static List<int> MapEntityReferences( List<string> input, int entityReferenceTypeId, int parentEntityTypeId, ref SaveStatus status )
		{
			int entityRef = 0;
			List<int> entityRefs = new List<int>();
			string registryAtId = "";
			//var or = new List<RA.Models.Input.EntityReference>();
			if ( input == null || input.Count < 1 )
				return entityRefs;

			int cntr = 0;
			foreach ( var target in input )
			{
				cntr++;
				entityRef = 0;
				//only allow a valid id
				if ( !string.IsNullOrWhiteSpace( target ))
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

		private static int ResolveEntityRegistryAtId( string registryAtId, int entityTypeId, ref SaveStatus status )
        {
            bool isResolved = false;
            int entityRef = 0;
            if ( !string.IsNullOrWhiteSpace( registryAtId ) )
            {
                entityRef = ResolutionServices.ResolveEntityByRegistryAtId( registryAtId, entityTypeId, ref status, ref isResolved );
            }

            return entityRef;
        }
        private static int ResolveEntityBaseToInt( MJ.EntityBase input, int entityTypeId, ref SaveStatus status )
        {

            int entityRefId = 0;
            int start = status.Messages.Count;
            if ( string.IsNullOrWhiteSpace( input.Name ) )
                status.AddError( "Invalid EntityBase, missing name" );
            //if ( string.IsNullOrWhiteSpace( input.Description ) )
            //    status.AddError( "Invalid EntityBase, missing Description" );
            if ( string.IsNullOrWhiteSpace( input.SubjectWebpage )  )
                status.AddError( "Invalid EntityBase, missing SubjectWebpage" );

            if ( start < status.Messages.Count )
                return entityRefId;

            string swp = input.SubjectWebpage;
            //look up by subject webpage
            //to be strict, we could use EntityStateId = 2. However, we could cover bases and get full if present
            //NOTE: need to avoid duplicate swp's; so  should combine
            if ( entityTypeId == CodesManager.ENTITY_TYPE_CREDENTIAL )
                entityRefId = ResolveBaseEntityAsCredential( input, swp, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_ASSESSMENT_PROFILE )
                entityRefId = ResolveBaseEntityAsAssessment( input, swp, ref status );
            else if ( entityTypeId == CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE )
                entityRefId = ResolveBaseEntityAsLopp( input, swp, ref status );
            else
            {
                //unexpected, should not have entity references for manifests
                status.AddError( string.Format( "Error - unexpected entityTypeId: {0}, Name: {1}, SWP: {2}", entityTypeId, input.Name, swp ) );
            }


            return entityRefId;
        }
        private static int ResolveBaseEntityAsCredential( MJ.EntityBase input, string swp, ref SaveStatus status )
        {
            int entityRefId = 0;

            MC.Credential entity = CredentialManager.GetBySubjectWebpage( swp );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
			entity = new workIT.Models.Common.Credential()
			{
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description
			};
			if ( new CredentialManager().AddBaseReference( entity, ref status ) > 0 )
                entityRefId = entity.Id;

            return entityRefId;
        }
        private static int ResolveBaseEntityAsAssessment( MJ.EntityBase input, string swp, ref SaveStatus status )
        {
            int entityRefId = 0;

            WPM.AssessmentProfile entity = AssessmentManager.GetBySubjectWebpage( swp );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
			entity = new workIT.Models.ProfileModels.AssessmentProfile()
			{
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description
			};
			if ( new AssessmentManager().AddBaseReference( entity, ref status ) > 0 )
                entityRefId = entity.Id;

            return entityRefId;
        }
        private static int ResolveBaseEntityAsLopp( MJ.EntityBase input, string swp, ref SaveStatus status )
        {
            int entityRefId = 0;

            WPM.LearningOpportunityProfile entity = LearningOpportunityManager.GetBySubjectWebpage( swp );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
			entity = new workIT.Models.ProfileModels.LearningOpportunityProfile()
			{
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description
			};
            if ( new LearningOpportunityManager().AddBaseReference( entity, ref status ) > 0 )
                entityRefId = entity.Id;

            return entityRefId;
        }

		///THIS SHOULD NOT BE POSSIBLE
		private static int ResolveBaseEntityAsConditionManifest( MJ.EntityBase input, string swp, ref SaveStatus status )
        {
            int entityRefId = 0;

            MC.ConditionManifest entity = ConditionManifestManager.GetBySubjectWebpage( swp );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
			entity = new workIT.Models.Common.ConditionManifest()
			{
				Name = input.Name,
				SubjectWebpage = input.SubjectWebpage,
				Description = input.Description
			};
			if ( new ConditionManifestManager().AddBaseReference( entity, ref status ) > 0 )
                entityRefId = entity.Id;

            return entityRefId;
        }

		///THIS SHOULD NOT BE POSSIBLE
		private static int ResolveBaseEntityAsCostManifest( MJ.EntityBase input, string swp, ref SaveStatus status )
        {
            int entityRefId = 0;

            MC.CostManifest entity = CostManifestManager.GetBySubjectWebpage( swp );
            if ( entity != null && entity.Id > 0 )
                return entity.Id;
			entity = new workIT.Models.Common.CostManifest()
			{
				Name = input.Name,
				CostDetails = input.SubjectWebpage,
				Description = input.Description
			};
			if ( new CostManifestManager().AddBaseReference( entity, ref status ) > 0 )
                entityRefId = entity.Id;

            return entityRefId;
        }
        #endregion


        #region  Condition profiles
        public static List<WPM.ConditionProfile> FormatConditionProfile( List<MJ.ConditionProfile> profiles, ref SaveStatus status )
        {
            if ( profiles == null || profiles.Count == 0 )
                return null;

            var list = new List<WPM.ConditionProfile>();
			
            foreach ( var input in profiles )
            {
                var cp = new WPM.ConditionProfile();
                cp.Name = input.Name;
				LoggingHelper.DoTrace( 6, "MappingHelper.FormatConditionProfile Name " + (input.Name ?? "no name") );
				cp.Description = input.Description;
                cp.SubjectWebpage = input.SubjectWebpage;
                cp.AudienceLevelType = MapCAOListToEnumermation( input.AudienceLevelType );
                cp.AudienceType = MapCAOListToEnumermation( input.AudienceType );
                cp.DateEffective = MapDate( input.DateEffective, "DateEffective", ref status );
                cp.Condition = MapToTextValueProfile( input.Condition );
                cp.SubmissionOf = MapToTextValueProfile( input.SubmissionOf );

                //while the input is a list, we are only handling single
                //cp.AssertedByAgentUid = MapOrganizationReferenceGuid( input.AssertedBy, ref status );
                //string assertedByReference = "";
                if ( input.AssertedBy != null )
                {
					cp.AssertedByAgentUid = MapOrganizationReferenceGuidFromObject( input.AssertedBy, ref status );
					//assertedByReference = input.AssertedBy.ToString();
					//Dictionary<string, object> dictionary = RegistryServices.JsonToDictionary( assertedByReference );
					//if ( assertedByReference.IndexOf( "@id" ) > -1 )
     //               {
     //                   cp.AssertedByAgentUid = MapOrganizationReferenceGuid( input.AssertedBy, ref status );

     //               }
     //               else
     //               {
					//	//org references
					//	//need to determine the json pattern
						
					//}
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

				if ( input.AlternativeCondition != null & input.AlternativeCondition.Count > 0)
					cp.AlternativeCondition = FormatConditionProfile( input.AlternativeCondition, ref status );

                cp.EstimatedCosts = FormatCosts( input.EstimatedCosts, ref status );

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

				//if ( input.TargetCompetency != null && input.TargetCompetency.Count > 0 )
				//cp.TargetCompetency = FormatCompetencies( input.RequiresCompetency, "Requires", ref messages );
				cp.TargetCompetencies = MappingHelper.MapCAOListToCompetencies( input.TargetCompetency );
				list.Add( cp );
            }

            return list;
        }

        #endregion

        #region  RevocationProfile
        public static List<WPM.RevocationProfile> FormatRevocationProfile( List<MJ.RevocationProfile> profiles, ref SaveStatus status )
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

        public static List<WPM.CostProfile> FormatCosts( List<MJ.CostProfile> costs, ref SaveStatus status )
        {
            return FormatCostProfileMerged( costs, ref status );

            //return OFormatCosts( costs, ref status );
        }

        private static List<WPM.CostProfile> OFormatCosts( List<MJ.CostProfile> costs, ref SaveStatus status )
        {
            if ( costs == null || costs.Count == 0 )
                return null;

            List<WPM.CostProfile> list = new List<WPM.CostProfile>();
            var cp = new WPM.CostProfile();

            foreach ( MJ.CostProfile item in costs )
            {
                cp = new WPM.CostProfile();
                cp.Description = item.Description;
                cp.CostDetails = item.CostDetails;

                //NEED validation
                cp.Currency = item.Currency;
                cp.ProfileName = item.Name;
                cp.EndDate = item.EndDate;
                cp.StartDate = item.StartDate;

                cp.Jurisdiction = MapToJurisdiction( item.Jurisdiction, ref status );

                WPM.CostProfileItem cpi = null;

                if ( item.DirectCostType != null && item.DirectCostType.Count > 0 )
                {
                    if ( cpi == null )
                        cpi = new workIT.Models.ProfileModels.CostProfileItem();
                    cpi.DirectCostType = MapCAOListToEnumermation( item.DirectCostType );
                }

                if ( item.ResidencyType != null && item.ResidencyType.Count > 0 )
                {
                    if ( cpi == null )
                        cpi = new workIT.Models.ProfileModels.CostProfileItem();
                    cpi.ResidencyType = MapCAOListToEnumermation( item.ResidencyType );
                }

                if ( item.AudienceType != null && item.AudienceType.Count > 0 )
                {
                    if ( cpi == null )
                        cpi = new workIT.Models.ProfileModels.CostProfileItem();
                    cpi.ApplicableAudienceType = MapCAOListToEnumermation( item.AudienceType );
                }

                if ( cpi == null )
                    cpi = new workIT.Models.ProfileModels.CostProfileItem();
                cpi.PaymentPattern = item.PaymentPattern;
                cpi.Price = item.Price;
                if ( cpi != null )
                    cp.Items.Add( cpi );

                //cp.Region = MapRegions( item.Region, ref messages );

                //cost items - should be an array
                //foreach ( MJ.CostProfileItem cpi in item.CostItems )
                //{
                //}
                //cp.Price = item.Price <= 0 ? null : item.Price.ToString() ;
                //cp.AudienceType = FormatCredentialAlignmentVocabs( item.AudienceType, "audience", ref messages );
                //cp.ResidencyType = FormatCredentialAlignmentVocabs( item.ResidencyType, "residency", ref messages );

                //foreach ( var type in item.AudienceType ) cp.AudienceType.Add( FormatCredentialAlignment( type ) );
                //foreach ( var type in item.ResidencyType ) cp.ResidencyType.Add( FormatCredentialAlignment( type ) );

                list.Add( cp );
            }

            return list;
        }

        public static List<WPM.CostProfile> FormatCostProfileMerged( List<MJ.CostProfile> costs, ref SaveStatus status )
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

                    CostType = MapCAOListToEnumermation( item.DirectCostType ),

                    Price = item.Price,

                    PaymentPattern = item.PaymentPattern,
                    ResidencyType = MapCAOListToEnumermation( item.ResidencyType )
                };
                list.Add( cp );
            }
            return WPM.CostProfileMerged.ExpandCosts( list );
        }

        //TO BE CHECKED HAS ERRORS
        //public static List<WPM.CostProfile> FormatCostProfileItems( List<MJ.CostProfile> costs, ref SaveStatus status )
        //{
        //    if ( costs == null || costs.Count == 0 )
        //        return null;

        //    List<WPM.CostProfile> list = new List<WPM.CostProfile>();
        //    var cp = new WPM.CostProfile();
        //    var prevCp = "";

        //    foreach ( MJ.CostProfile item in costs )
        //    {
        //        if ( item.Name != prevCp && prevCp != "" )
        //            list.Add();

        //        cp = new WPM.CostProfile();
        //        cp.Description = item.Description;
        //        cp.CostDetails = MapIdentityToString( item.CostDetails );

        //        //NEED validation
        //        cp.Currency = item.Currency;
        //        cp.ProfileName = item.Name;
        //        cp.EndDate = item.EndDate;
        //        cp.StartDate = item.StartDate;

        //        cp.Jurisdiction = MapToJurisdiction( item.Jurisdiction, ref status );

        //        WPM.CostProfileItem cpi = null;

        //        if ( item.DirectCostType != null && item.DirectCostType.Count > 0 )
        //        {
        //            if ( cpi == null )
        //                cpi = new workIT.Models.ProfileModels.CostProfileItem();
        //            cpi.DirectCostType = MapCAOListToEnumermation( item.DirectCostType );
        //        }

        //        if ( item.ResidencyType != null && item.ResidencyType.Count > 0 )
        //        {
        //            if ( cpi == null )
        //                cpi = new workIT.Models.ProfileModels.CostProfileItem();
        //            cpi.ResidencyType = MapCAOListToEnumermation( item.ResidencyType );
        //        }

        //        if ( item.AudienceType != null && item.AudienceType.Count > 0 )
        //        {
        //            if ( cpi == null )
        //                cpi = new workIT.Models.ProfileModels.CostProfileItem();
        //            cpi.ApplicableAudienceType = MapCAOListToEnumermation( item.AudienceType );
        //        }

        //        if ( cpi == null )
        //            cpi = new workIT.Models.ProfileModels.CostProfileItem();
        //        cpi.PaymentPattern = item.PaymentPattern;
        //        cpi.Price = item.Price;
        //        if ( cpi != null )
        //            cp.Items.Add( cpi );

        //        //cp.Region = MapRegions( item.Region, ref messages );

        //        //cost items - should be an array
        //        //foreach ( MJ.CostProfileItem cpi in item.CostItems )
        //        //{
        //        //}
        //        //cp.Price = item.Price <= 0 ? null : item.Price.ToString() ;
        //        //cp.AudienceType = FormatCredentialAlignmentVocabs( item.AudienceType, "audience", ref messages );
        //        //cp.ResidencyType = FormatCredentialAlignmentVocabs( item.ResidencyType, "residency", ref messages );

        //        //foreach ( var type in item.AudienceType ) cp.AudienceType.Add( FormatCredentialAlignment( type ) );
        //        //foreach ( var type in item.ResidencyType ) cp.ResidencyType.Add( FormatCredentialAlignment( type ) );

        //        list.Add( cp );
        //    }

        //    return list;
        //}
        #endregion

        #region  FinancialAlignmentObject
        public static List<MC.FinancialAlignmentObject> FormatFinancialAssistance( List<MJ.FinancialAlignmentObject> input, ref SaveStatus status )
        {
            if ( input == null || input.Count == 0 )
                return null;

            var list = new List<MC.FinancialAlignmentObject>();
            foreach ( var item in input )
            {
                var fa = new MC.FinancialAlignmentObject
                {
                    AlignmentType = item.AlignmentType,
                    Framework = item.Framework != null ? item.Framework : "",
                    FrameworkName = item.FrameworkName,
                    TargetNode = item.TargetNode != null ? item.TargetNode : "",
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
        public static List<WPM.DurationProfile> FormatDuration( List<MJ.DurationProfile> input, ref SaveStatus status )
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
        public static WPM.DurationItem FormatDurationItem( string duration )
        {
            if ( string.IsNullOrEmpty( duration ) )
                return null;
                       
            var match = new System.Text.RegularExpressions.Regex( @"^P(?!$)(\d+Y)?(\d+M)?(\d+W)?(\d+D)?(T(?=\d)(\d+H)?(\d+M)?(\d+S)?)?$" ).Match( duration );

            if ( !match.Success ) throw new FormatException( "ISO8601 duration format" );

            int years = 0, months = 0, weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;

            if ( match.Groups[1].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[1].Value, @"\d+" ).Value, out years );
            if ( match.Groups[2].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[2].Value, @"\d+" ).Value, out months );
            if ( match.Groups[3].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[3].Value, @"\d+" ).Value, out weeks );
            if ( match.Groups[4].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[4].Value, @"\d+" ).Value, out days );
            if ( match.Groups[6].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[6].Value, @"\d+" ).Value, out hours );
            if ( match.Groups[7].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[7].Value, @"\d+" ).Value, out minutes );
            if ( match.Groups[8].Success )
                int.TryParse( System.Text.RegularExpressions.Regex.Match( match.Groups[8].Value, @"\d+" ).Value, out seconds );

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
        private static WPM.DurationItem FormatDurationItem( MJ.DurationItem duration )
        {
            if ( duration == null )
                return null;
            var output = new WPM.DurationItem
            {
                Days = duration.Days,
                Hours = duration.Hours,
                Minutes = duration.Minutes,
                Months = duration.Months,
                Weeks = duration.Weeks,
                Years = duration.Years
            };

            return output;
        }

        #endregion

        #region Verification Profile 
        public static List<WPM.VerificationServiceProfile> MapVerificationServiceProfiles( List<MJ.VerificationServiceProfile> verificationServiceProfiles, ref SaveStatus status )
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

		public static decimal StringtoDecimal(string value)
		{
			decimal output = 0m;
			if ( string.IsNullOrWhiteSpace( value ) )
				return output;

			decimal.TryParse( value, out output );
			return output;
		}
    }
    public class EntityReferenceHelper
    {
        public List<IdProperty> IdList { get; set; }
    }
    public class IdProperty
    {
        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }
    }
}
