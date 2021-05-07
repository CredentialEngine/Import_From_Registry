﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using workIT.Models;
using workIT.Models.Common;
using WMA = workIT.Models.API;
using ME = workIT.Models.Elastic;
using workIT.Models.Search;
using workIT.Factories;
using workIT.Utilities;
using ElasticHelper = workIT.Services.ElasticServices;

using EntityHelper = workIT.Services.OrganizationServices;
using ThisEntity = workIT.Models.Common.Organization;
using EntityMgr = workIT.Factories.OrganizationManager;
namespace workIT.Services.API
{
	public class OrganizationServices
	{
		public static string externalFinderSiteURL = UtilityManager.GetAppKeyValue( "externalFinderSiteURL" );
		public static string searchType = "organization";
		#region methods for new API
		public static WMA.OrganizationDetail GetDetailForAPI( int id, bool skippingCache = false, bool includingAll=false )
		{
			//var output = EntityHelper.GetDetail( id, skippingCache );
			var request = new OrganizationManager.OrganizationRequest(2);
			if ( includingAll )
				request = new OrganizationManager.OrganizationRequest( 1 );

			if ( UtilityManager.GetAppKeyValue( "includeProcessProfileDetails", true ) )
				request.IncludingProcessProfiles = true;
			if ( UtilityManager.GetAppKeyValue( "includeManifestDetails", true ) )
				request.IncludingManifests = true;

			var output = EntityMgr.GetDetailForAPI( id, request );

			return MapToAPI( output, request );

		}
		//called by the search
		public static WMA.OrganizationDetail GetDetailForAPI( int id, bool skippingCache = false)
		{
			//var output = EntityHelper.GetDetail( id, skippingCache );
			var request = new OrganizationManager.OrganizationRequest( 2 );

			if ( UtilityManager.GetAppKeyValue( "includeProcessProfileDetails", true ) )
				request.IncludingProcessProfiles = true;
			if ( UtilityManager.GetAppKeyValue( "includeManifestDetails", true ) )
				request.IncludingManifests = true;

			var output = EntityMgr.GetDetailForAPI( id, request );

			return MapToAPI( output, request );

		}
		public static WMA.OrganizationDetail GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var request = new OrganizationManager.OrganizationRequest( 2 );
			var output = EntityMgr.GetDetailForAPI( ctid, request );

			return MapToAPI( output, request );
		}
		private static WMA.OrganizationDetail MapToAPI( Organization input, OrganizationManager.OrganizationRequest request )
		{



			var output = new WMA.OrganizationDetail()
			{
				Meta_Id = input.Id,
				Name = input.Name,
				FriendlyName = HttpUtility.UrlPathEncode( input.Name ),
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 2,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID ),
				RegistryData = ServiceHelper.FillRegistryData( input.CTID )

			};
			if ( input.ISQAOrganization )
			{
				output.CTDLType = "ceterms:QACredentialOrganization";
				output.CTDLTypeLabel = "Quality Assurance Organization";
			}
			else
			{
				output.CTDLType = "ceterms:CredentialOrganization";
				output.CTDLTypeLabel = "Credentialing Organization";
			}
			
			//output.CTDLType = record.AgentDomainType;
			output.AgentSectorType = ServiceHelper.MapPropertyLabelLinks( input.AgentSectorType, "organization" );
			output.AgentType = ServiceHelper.MapPropertyLabelLinks( input.AgentType, "organization" );
			//TODO consider using LabelLink to provide both the URL and description
			output.AgentPurpose = ServiceHelper.MapPropertyLabelLink( input.AgentPurpose, "Purpose", input.AgentPurposeDescription );
			//output.AgentPurpose = ServiceHelper.MapPropertyLabelLink( input.AgentPurpose, "Purpose" );

			//output.AgentPurposeDescription = input.AgentPurposeDescription;
			//
			output.AlternateName = input.AlternateName;
			if (!string.IsNullOrWhiteSpace( input.AvailabilityListing ) )
				output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
			else
				output.AvailabilityListing = input.AvailabilityListings;
			//
			output.CTID = input.CTID;
			if ( input.Emails != null && input.Emails.Any() )
				output.Email = input.Emails.Select( s => s.TextValue ).ToList();

			output.Meta_LastUpdated = input.EntityLastUpdated;
			output.Meta_StateId = input.EntityStateId;
			output.EntityTypeId = input.EntityTypeId;
			output.FoundingDate = input.FoundingDate;
			output.FriendlyName = input.FriendlyName;
			//identifiers
			output.Identifier = ServiceHelper.MapIdentifierValue(input.Identifier);
			output.DUNS = input.ID_DUNS;
			output.FEIN = input.ID_FEIN;
			output.IPEDSID = input.ID_IPEDSID;
			output.ISICV4 = input.ID_ISICV4;
			output.LEICode = input.ID_LEICode;
			output.NECS = input.ID_NECS;
			output.OPEID = input.ID_OPEID;
			//
			output.Image = input.Image;
			output.IndustryType = ServiceHelper.MapReferenceFrameworkLabelLink( input.IndustryType, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			//output.IsReferenceVersion = record.IsReferenceVersion;
			//
			if ( input.Keyword != null && input.Keyword.Any() )
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, "organization" );


			//output.MissionAndGoalsStatement = ServiceHelper.MapPropertyLabelLink( input.MissionAndGoalsStatement, "Mission Statement" );
			//output.MissionAndGoalsStatementDescription = input.MissionAndGoalsStatementDescription;
			output.MissionAndGoalsStatement = ServiceHelper.MapPropertyLabelLink( input.MissionAndGoalsStatement, "Mission Statement", input.MissionAndGoalsStatementDescription );

			//this is NOT pertinent to organization
			//output.OrganizationId = org.OrganizationId;
			//output.OrganizationName = org.OrganizationName;
			//output.OrganizationSubjectWebpage = "";
			output.ServiceType = ServiceHelper.MapPropertyLabelLinks( input.ServiceType, "organization" );
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			output.SocialMedia = ServiceHelper.MapTextValueProfileTextValue( input.SocialMediaPages );

			//output.TransferValueStatement = ServiceHelper.MapPropertyLabelLink( input.TransferValueStatement, "Transfer Value Statement" );
			//output.TransferValueStatementDescription = input.TransferValueStatementDescription;
			output.TransferValueStatement = ServiceHelper.MapPropertyLabelLink( input.TransferValueStatement, "Transfer Value Statement", input.TransferValueStatementDescription );


			//input.FriendlyName = HttpUtility.UrlPathEncode ( input.Name );
			//searches
			var links = new List<WMA.LabelLink>();
			output.Connections = null;
			if ( input.TotalCredentials > 0 )
			{
				//output.CredentialsSearch = ServiceHelper.MapEntitySearchLink( org.Id, org.Name, org.TotalCredentials, "Owns/Offers {0} Credential(s)", "credential" );

				//output.Connections.Add( output.CredentialsSearch );
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalCredentials, "Owns/Offers {0} Credential(s)", "credential", ref links );
			}
			if ( input.TotalLopps > 0 )
			{
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalLopps, "Owns/Offers {0} Learning Opportunity(ies)", "learningopportunity", ref links );
			}
			if ( input.TotalAssessments > 0 )
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalAssessments, "Owns/Offers {0} Assesment(s)", "assessment", ref links );

			if ( input.TotalPathwaySets > 0 )
			{
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalPathwaySets, "Owns {0} Pathway Set(s)", "pathwayset", ref links );
			}
			if ( input.TotalPathways > 0 )
			{
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalPathways, "Owns {0} Pathway(s)", "pathway", ref links );
			}
			if ( input.TotalTransferValueProfiles > 0 )
			{
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalTransferValueProfiles, "Owns {0} Transfer Value Profiles(s)", "transfervalue", ref links );
			}

			if ( input.TotalFrameworks > 0 )
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalFrameworks, "Owns {0} Competency Framework(s)", "competencyframework", ref links );

			if ( input.TotalConceptSchemes > 0 )
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.TotalConceptSchemes, "Owns {0} Concept Scheme(s)", "conceptscheme", ref links );

			//21-03-10 combining revokes and renews 
			if ( input.RevokesCredentials > 0 || input.RenewsCredentials > 0 )
				ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.RevokesCredentials + input.RenewsCredentials, "Renews/Revokes {0} Credential(s)", "credential", ref links, "11, 13" );
			//if ( org.RegulatesCredentials > 0 )
			//	ServiceHelper.MapEntitySearchLink( org.Id, org.Name, org.RegulatesCredentials, "Regulates {0} Credential(s)", "credential", ref links, "12" );
			//if ( input.RenewsCredentials > 0 )
			//	ServiceHelper.MapEntitySearchLink( input.Id, input.Name, input.RenewsCredentials, "Renews {0} Credential(s)", "credential", ref links, "13" );

			//
			if ( links.Any() )
				output.Connections = links;
			//need to handle other roles: renews, revokes, regulates
			//QA performed
			output.QAPerformed = new List<WMA.LabelLink>();
			links = new List<WMA.LabelLink>();
			if ( input.QAPerformedOnCredentialsCount > 0 )
				ServiceHelper.MapQAPerformedLink( input.Id, input.Name, input.QAPerformedOnCredentialsCount, "QA Identified as Performed on {0} Credential(s)", "credential", ref links );

			if ( input.QAPerformedOnOrganizationsCount > 0 )
				ServiceHelper.MapQAPerformedLink( input.Id, input.Name, input.QAPerformedOnOrganizationsCount, "QA Identified as Performed on {0} Organization(s)", "organization", ref links );
			if ( input.QAPerformedOnAssessmentsCount > 0 )
				ServiceHelper.MapQAPerformedLink( input.Id, input.Name, input.QAPerformedOnAssessmentsCount, "QA Identified as Performed on {0} Assessment(s)", "assessment", ref links );

			if ( input.QAPerformedOnLoppsCount > 0 )
				ServiceHelper.MapQAPerformedLink( input.Id, input.Name, input.QAPerformedOnLoppsCount, "QA Identified as Performed on {0} Learning Opportunity(ies)", "learningopportunity", ref links );

			if ( links.Any() )
				output.QAPerformed = links;

			//21-03-12 these should be populated now. Be sure to remove them from QaReceived.
			//need to be consistent. Seems this property will use acting agent, but dept/sub will use participating agent
			output.ParentOrganization = ServiceHelper.MapOrganizationRoleProfileToAJAX( input.ParentOrganizations, "Has Parent Organization" );
			if ( input.ParentOrganization != null && input.ParentOrganization.Any() )
			{
				var parents = ServiceHelper.MapOrganizationRoleProfileToOutline( input.ParentOrganizations,Entity_AgentRelationshipManager.ROLE_TYPE_PARENT_ORG );
				if (parents != null && parents.Any())
				{
					//just return one for now
					//output.ParentOrganizationOutline = parents[ 0 ];
				}
			}
			//
			output.Department = ServiceHelper.MapOrganizationRoleProfileToAJAX( input.OrganizationRole_Dept, "Has {0} Departments(s)" );
			output.SubOrganization = ServiceHelper.MapOrganizationRoleProfileToAJAX( input.OrganizationRole_Subsidiary, "Has {0} Suborganization(s)" );
			//

			//QAReceived
			//==> need to exclude 30-published by 
			//also check for 20, 21 and move to dept, subsidiary, parent
			var qaroles = "1,2,10,12";
			if ( input.OrganizationRole_Recipient.Any() )
			{
				output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole_Recipient, searchType );
				/*
				output.QAReceived = new List<WMA.OrganizationRoleProfile>();
				foreach ( var item in input.OrganizationRole_Recipient )
				{
					var orp = new WMA.OrganizationRoleProfile()
					{
						Label = string.IsNullOrWhiteSpace( item.ProfileName ) ? item.ParentSummary : item.ProfileName,
						Description = item.Description ?? ""
					};
					if ( string.IsNullOrWhiteSpace( orp.Label ) )
					{
						if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
						{
							orp.Label = item.ActingAgent.Name;
							orp.Description = item.ActingAgent.Description;
						}
					}
					if ( string.IsNullOrEmpty( item.ActingAgent.CTID ) )
						orp.URL = item.ActingAgent.SubjectWebpage;
					else
						orp.URL = externalFinderSiteURL + string.Format( "organization/{0}", input.Id );
					bool isPublishedByRole = false;
					if ( item.AgentRole != null && item.AgentRole.Items.Any() )
					{
						foreach ( var ar in item.AgentRole.Items )
						{
							//no link
							if ( ar.Id == 30 )
							{
								//if published by, probably will not have other roles!
								//continue;
								isPublishedByRole = true;
								break;
							} else if ( ar.Id == 20 || ar.Id == 21 || ar.Id == 22)
							{
								//skip dept, subsidiary and parent
								continue;
							}
							//should this be the reverseTitle?
							if ( item.ActingAgent != null && item.ActingAgent.Id > 0 )
							{
								//if role is QA, include all 4 in link
								ServiceHelper.MapEntitySearchLink( item.ActingAgent.Id, item.ActingAgent.Name, 0, ar.Name, searchType, ref orp.Roles, qaroles );//ar.Id.ToString()
							} else 
								orp.Roles.Add( new WMA.LabelLink() { Label = ar.Name } );
						}
					}
					if ( !isPublishedByRole && orp.Roles.Any() )
						output.QAReceived.Add( orp );
				}
				*/
			}
			//
			MapAddress( input, ref output );

			if(request.IncludingManifests)
			{

			} else
			{

			}
			//manifests
			MapManifests( input, ref output, request );


			//process profiles
			MapProcessProfiles( input, ref output, request );

			//
			MapJurisdictions( input, ref output );
			//
			MapVerificationServiceProfile( input, ref output, request );

			return output;
		}
		#region Manifests
		public static void MapManifests( Organization org, ref WMA.OrganizationDetail output, OrganizationManager.OrganizationRequest request )
		{
			if ( org.HasCostManifest != null && org.HasCostManifest.Any() )
			{
				if ( request.IncludingManifests )
				{
					//convert this to ajax
					output.HasCostManifest2 = ServiceHelper.MapCostManifests( org.HasCostManifest, searchType );
					output.HasCostManifest = new AJAXSettings()
					{
						//Type=null,
						Label = string.Format( "Has {0} Cost Manifest(s)", org.HasCostManifest.Count() ),
						Total = org.HasCostManifest.Count()
					};
					List<object> obj = output.HasCostManifest2.Select( f => ( object )f ).ToList();
					output.HasCostManifest.Values = obj;
					output.HasCostManifest2 = null;
				}
				else
				{
					var url = string.Format( "detail/costManifest/{0}/", org.Id.ToString() );
					output.HasCostManifest = new AJAXSettings()
					{
						Label = string.Format("Has {0} Cost Manifest(s)", org.HasCostManifest.Count()),
						Total = org.HasCostManifest.Count(),
						URL = externalFinderSiteURL + url,
						TestURL = ServiceHelper.finderApiSiteURL + url
					};
				}				
			}

			if ( org.HasConditionManifest != null && org.HasConditionManifest.Any() )
			{
				if ( request.IncludingManifests )
				{
					output.HasConditionManifest2 = ServiceHelper.MapConditionManifests( org.HasConditionManifest, searchType );
					output.HasConditionManifest = new AJAXSettings()
					{
						//Type=null,
						Label = string.Format( "Has {0} Condition Manifest(s)", org.HasConditionManifest.Count() ),
						Total = org.HasConditionManifest.Count()
					};
					List<object> obj = output.HasConditionManifest2.Select( f => ( object )f ).ToList();
					output.HasConditionManifest.Values = obj;
					output.HasConditionManifest2 = null;
				}
				else
				{
					var url = string.Format( "detail/conditionManifest/{0}/", org.Id.ToString() );
					output.HasConditionManifest = new AJAXSettings()
					{
						Label = string.Format( "Has {0} Condition Manifest(s)", org.HasConditionManifest.Count() ),
						Total = org.HasCostManifest.Count(),
						URL = externalFinderSiteURL + url,
						TestURL = ServiceHelper.finderApiSiteURL + url
					};
				}
			}
		}
		public static List<WMA.CostManifest> GetCostManifests( int organizationId )
		{
			var output = new List<WMA.CostManifest>();
			//make a common method - then can pass parent to use for details
			var plist = CostManifestManager.GetAll( organizationId, false );
			if ( plist != null && plist.Any() )
			{
				output = ServiceHelper.MapCostManifests( plist, searchType );
			}
			return output;
		}

		public static List<WMA.ConditionManifest> GetConditionManifests( int organizationId )
		{
			var output = new List<WMA.ConditionManifest>();
			//make a common method - then can pass parent to use for details
			var plist = ConditionManifestManager.GetAll( organizationId, false );
			if ( plist != null && plist.Any() )
			{
				output = ServiceHelper.MapConditionManifests( plist, searchType );
			}
			return output;
		}
		#endregion

		private static void MapAddress( Organization input, ref WMA.OrganizationDetail output )
		{

			output.Address = ServiceHelper.MapAddress( input.Addresses );

			//handle 'orphans' contact points not associated with an address
			if ( input.ContactPoint.Any() )
			{
				//where to put these?
				var address = new WMA.Address();
				address.TargetContactPoint = new List<WMA.ContactPoint>();

				foreach ( var cp in input.ContactPoint )
				{
					var cpOutput = new WMA.ContactPoint()
					{
						Name = cp.Name,
						ContactType = string.IsNullOrWhiteSpace( cp.ContactType ) ? cp.ProfileName : cp.ContactType,
						Email = cp.Emails,
						Telephone = cp.PhoneNumbers,
						SocialMedia = cp.SocialMediaPages
					};
					if ( cp.PhoneNumber != null && cp.PhoneNumber.Any() )
						cpOutput.Telephone = ServiceHelper.MapTextValueProfileToStringList( cp.PhoneNumber );
					if ( cp.FaxNumber != null && cp.FaxNumber.Any() )
						cpOutput.FaxNumber = cp.FaxNumber;

					if ( cp.SocialMedia != null && cp.SocialMedia.Any() )
						cpOutput.SocialMedia = ServiceHelper.MapTextValueProfileToStringList( cp.SocialMedia );

					if ( cpOutput.Email.Any() || cpOutput.Telephone.Any() || cpOutput.SocialMedia.Any() )
						address.TargetContactPoint.Add( cpOutput );
				}
				output.Address.Add( address );

			}

		}

		private static void MapJurisdictions( Organization org, ref WMA.OrganizationDetail output )
		{
			if ( org.Jurisdiction != null && org.Jurisdiction.Any() )
			{
				output.Jurisdiction = ServiceHelper.MapJurisdiction( org.Jurisdiction );

			}
			//return if no assertions
			if ( org.JurisdictionAssertions == null || !org.JurisdictionAssertions.Any() )
			{
				return;
			}
			//TODO - return all in a group or individual?
			//output.JurisdictionAssertion = ServiceHelper.MapJurisdiction( org.JurisdictionAssertions, "OfferedIn" );
			//OR

			var assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
			{
				output.AccreditedIn = ServiceHelper.MapJurisdiction( assertedIn, "AccreditedIn" );
			}
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.ApprovedIn = ServiceHelper.MapJurisdiction( assertedIn, "ApprovedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertedIn, "RecognizedIn" );
			//
			assertedIn = org.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertedIn != null && assertedIn.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertedIn, "RegulatedIn" );
		}

		private static void MapProcessProfiles( Organization input, ref WMA.OrganizationDetail output, OrganizationManager.OrganizationRequest request )
		{

			//TBD - both for now???

			if ( input.ProcessProfilesSummary != null && input.ProcessProfilesSummary.Any() )
			{
				var url = string.Format( "detail/ProcessProfile/{0}/", input.RowId.ToString() );
				output.ProcessProfiles = new List<AJAXSettings>();
				foreach ( var item in input.ProcessProfilesSummary )
				{
					var ajax = new AJAXSettings()
					{
						Label = item.Name,
						Description = "",
						Total = item.Totals,
						URL = externalFinderSiteURL + url + item.Id.ToString(),
						TestURL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
					};
					//not sure we need this as part of the URL
					var qd = new ProcessProfileAjax()
					{
						Id = input.RowId.ToString(),
						ProcessTypeId = item.Id,
						//EndPoint = externalFinderSiteURL + url + item.Id.ToString()
					};
					ajax.QueryData = qd;
					/*need to know
					 * endpoint: detail/processprofile
					 * id:		 input.RowId
					 * processProfileTypeId: item.Id?
					 * 
					 */
					//var filter = ( new WMA.LabelLink()
					//{
					//	Label = item.Name,
					//	Count = item.Totals,
					//	URL = externalFinderSiteURL + url + item.Id.ToString()
					//} );


					output.ProcessProfiles.Add( ajax );
				}

				return;
			}

			//process profiles
			if ( input.AdministrationProcess.Any() )
			{
				output.AdministrationProcess = ServiceHelper.MapAJAXProcessProfile( "Administration Process", "", input.AdministrationProcess );
			}
			if ( input.AppealProcess.Any() )
			{
				output.AppealProcess = ServiceHelper.MapAJAXProcessProfile( "Appeal Process", "", input.AppealProcess );
			}
			if ( input.ComplaintProcess.Any() )
			{
				output.ComplaintProcess = ServiceHelper.MapAJAXProcessProfile( "Complaint Process", "", input.ComplaintProcess );
			}
			if ( input.DevelopmentProcess.Any() )
			{
				output.DevelopmentProcess = ServiceHelper.MapAJAXProcessProfile( "Development Process", "", input.DevelopmentProcess );
			}
			if ( input.MaintenanceProcess.Any() )
			{
				output.MaintenanceProcess = ServiceHelper.MapAJAXProcessProfile( "Maintenance Process", "", input.MaintenanceProcess );
			}
			if ( input.ReviewProcess.Any() )
			{
				output.ReviewProcess = ServiceHelper.MapAJAXProcessProfile( "Review Process", "", input.ReviewProcess );
			}
			if ( input.RevocationProcess.Any() )
			{
				output.RevocationProcess = ServiceHelper.MapAJAXProcessProfile( "Revocation Process", "", input.RevocationProcess );
			}
		}
		private static void MapVerificationServiceProfile( Organization org, ref WMA.OrganizationDetail output, OrganizationManager.OrganizationRequest request )
		{

			if (request.IncludingVerificationProfiles)
			{
				if ( org.VerificationServiceProfiles == null || !org.VerificationServiceProfiles.Any() )
					return;

				output.HasVerificationServiceTemp = new List<WMA.VerificationServiceProfile>();
				foreach ( var item in org.VerificationServiceProfiles )
				{
					WMA.VerificationServiceProfile vsp = new WMA.VerificationServiceProfile()
					{
						DateEffective = item.DateEffective,
						Description = item.Description,
						HolderMustAuthorize = item.HolderMustAuthorize,
						SubjectWebpage = item.SubjectWebpage,
						VerificationDirectory = item.VerificationDirectory,
						VerificationMethodDescription = item.VerificationMethodDescription,
						VerificationService = item.VerificationServiceUrl
					};
					if ( item.Jurisdiction != null && item.Jurisdiction.Any() )
					{
						vsp.Jurisdiction = ServiceHelper.MapJurisdiction( item.Jurisdiction );
					}
					if ( item.Region != null && item.Region.Any() )
					{
						vsp.Region = ServiceHelper.MapJurisdiction( item.Region );
					}
					//CostProfiles
					if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
					{
						vsp.EstimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
					}

					//OfferredIn
					if ( item.JurisdictionAssertions != null && item.JurisdictionAssertions.Any() )
					{
						vsp.OfferedIn = ServiceHelper.MapJurisdiction( item.JurisdictionAssertions, "OfferedIn" );
					}
					//
					//if ( item.OfferedByAgent != null && !string.IsNullOrWhiteSpace( item.OfferedByAgent.Name ) )
					//	vsp.OfferedBy = ServiceHelper.MapToEntityReference( item.OfferedByAgent, searchType );
					//
					if ( item.OfferedByAgent != null && !string.IsNullOrWhiteSpace( item.OfferedByAgent.Name ) )
					{
						var ab = ServiceHelper.MapToOutline( item.OfferedByAgent, searchType );
						vsp.OfferedBy = ServiceHelper.MapOutlineToAJAX( ab, "Offered by {0} Organization(s)" );
					}
					vsp.VerifiedClaimType = ServiceHelper.MapPropertyLabelLinks( item.ClaimType, "organization", false );
					if ( item.TargetCredential != null && item.TargetCredential.Any() )
					{
						vsp.TargetCredential = ServiceHelper.MapCredentialToAJAXSettings( item.TargetCredential, "Has {0} Target Credential(s)" );
						//vsp.TargetCredential = new List<TopLevelEntityReference>();
						//foreach ( var target in item.TargetCredential )
						//{
						//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						//		vsp.TargetCredential.Add( ServiceHelper.MapToEntityReference( target, "credential" ) );
						//}
						//vsp.TargetCredential = ServiceHelper.MapToEntityReference( item.TargetCredential );
					}

					output.HasVerificationServiceTemp.Add( vsp );
					
				}
				output.HasVerificationService = new AJAXSettings()
				{
					//Type=null,
					Label = string.Format( "Has {0} Verification Service(s)", output.HasVerificationServiceTemp.Count() ),
					Total = output.HasVerificationServiceTemp.Count()
				};
				List<object> obj = output.HasVerificationServiceTemp.Select( f => ( object )f ).ToList();
				output.HasVerificationService.Values = obj;
				output.HasVerificationServiceTemp = null;

			} else if (org.VerificationServiceProfileCount > 0)
			{
				var url = string.Format( "detail/VerificationService/{0}", org.RowId.ToString() );
				output.HasVerificationService = new AJAXSettings();
				var ajax = new AJAXSettings()
				{
					Label = string.Format( "Has {0} Verification Service(s)", org.HasCostManifest.Count() ),
					Total = org.VerificationServiceProfileCount,
					URL = externalFinderSiteURL + url ,
					TestURL = ServiceHelper.finderApiSiteURL + url
				};
				//don't need this, as have the URL
				//var qd = new ProcessProfileAjax()
				//{
				//	Id = org.RowId.ToString(),
				//	//EndPoint = externalFinderSiteURL + url + item.Id.ToString()
				//};
				//ajax.QueryData = qd;
				output.HasVerificationService = ajax ;
				//var filter = ( new WMA.LabelLink()
				//{
				//	Label = "Has Verification Service",
				//	Count = org.VerificationServiceProfileCount,
				//	URL = externalFinderSiteURL + url 
				//} );
				//output.HasVerificationServiceSummary.Add( filter );
			
				//return;
			}

			

		}
		/// <summary>
		/// Get verification service profiles on demand**********
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static List<WMA.VerificationServiceProfile> GetVerificationServiceProfiles( Guid parentUid )
		{
			var output = new List<WMA.VerificationServiceProfile>();
			//make a common method - then can pass parent to use for details
			var plist = Entity_VerificationProfileManager.GetAll( parentUid );
			if ( plist != null && plist.Any() )
			{
				foreach ( var item in plist )
				{
					WMA.VerificationServiceProfile vsp = new WMA.VerificationServiceProfile()
					{
						DateEffective = item.DateEffective,
						Description = item.Description,
						HolderMustAuthorize = item.HolderMustAuthorize,
						SubjectWebpage = item.SubjectWebpage,
						VerificationDirectory = item.VerificationDirectory,
						VerificationMethodDescription = item.VerificationMethodDescription,
						VerificationService = item.VerificationServiceUrl
					};
					if ( item.Jurisdiction != null && item.Jurisdiction.Any() )
					{
						vsp.Jurisdiction = ServiceHelper.MapJurisdiction( item.Jurisdiction );
					}
					if ( item.Region != null && item.Region.Any() )
					{
						vsp.Region = ServiceHelper.MapJurisdiction( item.Region );
					}
					//CostProfiles
					if ( item.EstimatedCost != null && item.EstimatedCost.Any() )
					{
						vsp.EstimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
					}

					//OfferredIn
					if ( item.JurisdictionAssertions != null && item.JurisdictionAssertions.Any() )
					{
						vsp.OfferedIn = ServiceHelper.MapJurisdiction( item.JurisdictionAssertions, "OfferedIn" );
					}
					//
					if ( item.OfferedByAgent != null && !string.IsNullOrWhiteSpace( item.OfferedByAgent.Name ) )
					{
						var ab = ServiceHelper.MapToOutline( item.OfferedByAgent, searchType );
						vsp.OfferedBy = ServiceHelper.MapOutlineToAJAX( ab, "Offered by {0} Organization(s)" );
					}

					vsp.VerifiedClaimType = ServiceHelper.MapPropertyLabelLinks( item.ClaimType, searchType, false );
					if ( item.TargetCredential != null && item.TargetCredential.Any() )
					{
						vsp.TargetCredential = ServiceHelper.MapCredentialToAJAXSettings( item.TargetCredential, "Has {0} Target Credential(s)" );
						//vsp.TargetCredential = new List<TopLevelEntityReference>();
						//foreach ( var target in item.TargetCredential )
						//{
						//	if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						//		vsp.TargetCredential.Add( ServiceHelper.MapToEntityReference( target, "credential" ) );
						//}
						//vsp.TargetCredential = ServiceHelper.MapToEntityReference( item.TargetCredential );
					}

					output.Add( vsp );
				}
			}
			return output;
		}


		#endregion
	}

	public class QueryData
	{
		public string EndPoint { get; set; }

	}
	public class ProcessProfileAjax : QueryData
	{
		public string Id { get; set; }
		public int? ProcessTypeId { get; set; }

	}
}
