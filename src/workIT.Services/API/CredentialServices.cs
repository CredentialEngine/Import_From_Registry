using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using workIT.Factories;
using workIT.Models.Common;
using workIT.Models.Search;
using workIT.Utilities;

using CachedEntity = workIT.Services.API.CachedCredential;
using ResourceManager = workIT.Factories.CredentialManager;
using ThisResource = workIT.Models.Common.Credential;
using OutputResource = workIT.Models.API.CredentialDetail;
using WMA = workIT.Models.API;
using WMP = workIT.Models.ProfileModels;
using WorkITSearchServices = workIT.Services.SearchServices;

namespace workIT.Services.API
{
	public class CredentialServices
	{
		static string thisClassName = "API.CredentialServices";
		public static string searchType = "Credential";

		public static OutputResource GetDetailForAPI( int id, bool skippingCache = false )
		{
			CredentialRequest cr = new CredentialRequest();
			cr.IsAPIRequest();
			cr.IncludingProcessProfiles = UtilityManager.GetAppKeyValue( "includeProcessProfileDetails",false);
			cr.AllowCaching = !skippingCache;

			OutputResource outputEntity = new OutputResource();
			if ( UsingCache( id, cr, ref outputEntity ) )
			{
				return outputEntity;
			}
			//only cache longer processes
			DateTime start = DateTime.Now;
			//var entity = EntityHelper.GetDetail( id, cr, skippingCache );
			var entity = ResourceManager.GetForDetail( id, cr );

			DateTime end = DateTime.Now;
			//for now don't include the mapping in the elapsed
			int elasped = ( DateTime.Now - start ).Seconds;
			outputEntity = MapToAPI( entity );
			if ( elasped > 5 )
				CacheEntity( outputEntity );
			return outputEntity;

		}
		public static OutputResource GetDetailByCtidForAPI( string ctid, bool skippingCache = false )
		{
			var credential = ResourceManager.GetMinimumByCtid( ctid );
			return GetDetailForAPI( credential.Id, skippingCache );

		}
		private static OutputResource MapToAPI( ThisResource input )
		{

			var output = new OutputResource()
			{
				Meta_Id = input.Id,
				Meta_RowId = input.RowId,
				CTID = input.CTID,
				Name = input.Name,
				Description = input.Description,
				SubjectWebpage = input.SubjectWebpage,
				EntityTypeId = 1,
				CTDLTypeLabel = input.CredentialType,
				CTDLType = input.CredentialTypeSchema,
				CredentialRegistryURL = RegistryServices.GetResourceUrl( input.CTID )

			};
			if (input.EntityStateId == 0)
			{
				return output;
			}
			output.RegistryData = ServiceHelper.FillRegistryData( input.CTID, searchType );
			//experimental - not used in UI yet
			output.RegistryDataList.Add( output.RegistryData );
			//check for others

			//
			//output.CTDLType = record.CredentialType; ;
			//output.AgentSectorType = ServiceHelper.MapPropertyLabelLinks( org.AgentSectorType, "organization" );
			output.Meta_FriendlyName = HttpUtility.UrlPathEncode( input.Name );
			output.AlternateName = input.AlternateName;

			//owned by and offered by 
			//need a label link for header
			if (input.OwningOrganizationId > 0)
			{
				output.OwnedByLabel = ServiceHelper.MapDetailLink( "Organization", input.OrganizationName, input.OwningOrganizationId, input.PrimaryOrganization.FriendlyName );
			}
			//if there is just one org, and has both owned and offered by
			//if ( ServiceHelper.AreOnlyRolesOwnsOffers( input.OrganizationRole ) )
			//{

			//}
			//else
			{
				var ownedBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
				var offeredBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY );
				if (ownedBy != null && offeredBy != null && ownedBy.Count == 1 && offeredBy.Count == 1
					&& ownedBy[0].Meta_Id == offeredBy[0].Meta_Id)
				{
					//if ( ownedBy[ 0 ].Meta_Id == offeredBy[ 0 ].Meta_Id )
					//{
					//	//var ooBy = ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_OWNER );
					output.OwnedOfferedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
					//}
				}
				else
				{
					output.OwnedBy = ServiceHelper.MapOutlineToAJAX( ownedBy, "" );
					//
					output.OfferedBy = ServiceHelper.MapOutlineToAJAX( offeredBy, "Offered by {0} Organization(s)" );
				}
			}
			//should only do if different from owner! Actually only populated if by a 3rd party
			output.Publisher = ServiceHelper.MapOutlineToAJAX( ServiceHelper.MapOrganizationRoleProfileToOutline( input.OrganizationRole, Entity_AgentRelationshipManager.ROLE_TYPE_PUBLISHEDBY ), "Published By" );
			//QA for owner,not offerer
			if (input.OwningOrganizationQAReceived != null && input.OwningOrganizationQAReceived.Any())
			{
				output.OwnerQAReceived = ServiceHelper.MapQAReceived( input.OwningOrganizationQAReceived, searchType );
			}
			//
			output.EntityLastUpdated = input.EntityLastUpdated;
			output.Meta_StateId = input.EntityStateId;
			output.EntityTypeId = input.EntityTypeId;
			if (input.InLanguageCodeList != null && input.InLanguageCodeList.Any())
			{
				//output.Meta_Language = input.InLanguageCodeList[ 0 ].TextTitle;
				output.InLanguage = new List<string>();
				foreach (var item in input.InLanguageCodeList)
				{
					output.InLanguage.Add( item.TextTitle );
				}
			}
			//
			if ( string.IsNullOrWhiteSpace( input.CTID ) )
			{
				output.IsReferenceVersion = true;
				output.Name += " [reference]";
			}
			else
				output.IsReferenceVersion = false;

			try
			{
				if (input.HasVerificationType_Badge)
				{
					//output.CTDLTypeLabel += " + Badge Issued";
					output.Meta_HasVerificationBadge = true;
				}
				output.Image = input.Image;
				//
				if (!string.IsNullOrWhiteSpace( input.AvailabilityListing ))
					output.AvailabilityListing = new List<string>() { input.AvailabilityListing };
				if (!string.IsNullOrWhiteSpace( input.AvailableOnlineAt ))
					output.AvailableOnlineAt = new List<string>() { input.AvailableOnlineAt };

				if (input.CopyrightHolderOrganization != null && input.CopyrightHolderOrganization.Any())
				{
					output.CopyrightHolder = new List<WMA.Outline>();
					//output.CopyrightHolder2 = new List<WMA.LabelLink>();
					foreach (var target in input.CopyrightHolderOrganization)
					{
						if (target != null && target.Id > 0)
						{
							//TODO - add overload to only get minimum data - like Link
							output.CopyrightHolder.Add( ServiceHelper.MapToOutline( target, "organization" ) );

							//OR return AjaxSettings?
							var copyrightHolder = ServiceHelper.MapOutlineToAJAX( output.CopyrightHolder, "Copyright Holder {0}" );
						}
					}
					//or Link objects

				}

				output.CredentialStatusType = ServiceHelper.MapPropertyLabelLink( input.CredentialStatusType, searchType );
				output.CredentialType = ServiceHelper.MapPropertyLabelLink( input.CredentialTypeEnum, searchType );

				output.DateEffective = input.DateEffective;
				output.ExpirationDate = input.ExpirationDate;
				output.EstimatedDuration = ServiceHelper.MapDurationProfiles( input.EstimatedDuration );
				output.RenewalFrequency = ServiceHelper.MapDurationItem( input.RenewalFrequency );
				//
				if (input.DegreeConcentration != null && input.DegreeConcentration.Any())
					output.DegreeConcentration = ServiceHelper.MapPropertyLabelLinks( input.DegreeConcentration, searchType );
				if (input.DegreeMajor != null && input.DegreeMajor.Any())
					output.DegreeMajor = ServiceHelper.MapPropertyLabelLinks( input.DegreeMajor, searchType );
				if (input.DegreeMinor != null && input.DegreeMinor.Any())
					output.DegreeMinor = ServiceHelper.MapPropertyLabelLinks( input.DegreeMinor, searchType );
				//
				if (input.EmbeddedCredentials != null && input.EmbeddedCredentials.Any())
				{
					var hasPart = new List<WMA.Outline>();
					foreach (var target in input.EmbeddedCredentials)
					{
						if (target != null && !string.IsNullOrWhiteSpace( target.Name ))
							hasPart.Add( ServiceHelper.MapToOutline( target, searchType ) );
					}
					output.HasPart = ServiceHelper.MapOutlineToAJAX( hasPart, "Includes {0} Credential(s)" );
					//output.HasPart2 = null;

				}
				//
				if (input.IsPartOf != null && input.IsPartOf.Any())
				{
					var isPartOf = new List<WMA.Outline>();
					foreach (var target in input.IsPartOf)
					{
						if (target != null && !string.IsNullOrWhiteSpace( target.Name ))
							isPartOf.Add( ServiceHelper.MapToOutline( target, searchType ) );
					}
					output.IsPartOf = ServiceHelper.MapOutlineToAJAX( isPartOf, "Is Part of {0} Credential(s)" );
					//output.IsPartOf2 = null;
				}
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 1, Name: {0}, Id: {1}", input.Name, input.Id ) );

			}

			//addresses
			//MapAddress( input, ref output );
			output.AvailableAt = ServiceHelper.MapAddress( input.Addresses );
			output.InCatalog = input.InCatalog;
			//
			if (input.CollectionMembers != null && input.CollectionMembers.Count > 0)
			{
				output.Collections = ServiceHelper.MapCollectionMemberToOutline( input.CollectionMembers );
			}
			//
			output.Image = input.Image;
			if (!string.IsNullOrWhiteSpace( input.CredentialTypeSchema ))
				output.Meta_Icon = WorkITSearchServices.GetCredentialIcon( input.CredentialTypeSchema.ToLower() );
			//new
			output.IndustryType = ServiceHelper.MapReferenceFramework( input.IndustryTypes, searchType, CodesManager.PROPERTY_CATEGORY_NAICS );
			output.OccupationType = ServiceHelper.MapReferenceFramework( input.OccupationTypes, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			output.InstructionalProgramType = ServiceHelper.MapReferenceFramework( input.InstructionalProgramTypes, searchType, CodesManager.PROPERTY_CATEGORY_CIP );
			//Old
			//output.OccupationTypeOld = ServiceHelper.MapReferenceFrameworkLabelLink( input.OccupationType, searchType, CodesManager.PROPERTY_CATEGORY_SOC );
			//
			if (input.IsNonCredit != null && input.IsNonCredit == true)
				output.IsNonCredit = input.IsNonCredit;

			//
			if (input.Keyword != null && input.Keyword.Any())
				output.Keyword = ServiceHelper.MapPropertyLabelLinks( input.Keyword, searchType );
			if (input.Subject != null && input.Subject.Any())
				output.Subject = ServiceHelper.MapPropertyLabelLinks( input.Subject, searchType );
			//
			output.AssessmentDeliveryType = ServiceHelper.MapPropertyLabelLinks( input.AssessmentDeliveryType, searchType );
			output.AudienceLevelType = ServiceHelper.MapPropertyLabelLinks( input.AudienceLevelType, searchType );
			output.AudienceType = ServiceHelper.MapPropertyLabelLinks( input.AudienceType, searchType );
			output.LearningDeliveryType = ServiceHelper.MapPropertyLabelLinks( input.LearningDeliveryType, searchType );

			output.ProvidesTransferValueFor = ServiceHelper.MapResourceSummaryAJAXSettings( input.ProvidesTransferValueFor, "TransferValue" );
			output.ReceivesTransferValueFrom = ServiceHelper.MapResourceSummaryAJAXSettings( input.ReceivesTransferValueFrom, "TransferValue" );
			output.HasRubric = ServiceHelper.MapResourceSummaryAJAXSettings( input.HasRubric, "Rubric" );
			//
			//condition profiles
			try
			{
				output.Corequisite = ServiceHelper.MapToConditionProfiles( input.Corequisite, searchType );
				output.CoPrerequisite = ServiceHelper.MapToConditionProfiles( input.CoPrerequisite, searchType );

				output.Recommends = ServiceHelper.MapToConditionProfiles( input.Recommends, searchType );
				output.Renewal = ServiceHelper.MapToConditionProfiles( input.Renewal, searchType );
				output.Requires = ServiceHelper.MapToConditionProfiles( input.Requires, searchType );
				if (input.CommonConditions != null && input.CommonConditions.Any())
				{
					//these will likely just be mapped to specific conditions
					output.CommonConditions = ServiceHelper.MapConditionManifests( input.CommonConditions, searchType );
					if (output.CommonConditions != null && output.CommonConditions.Any())
					{
						//foreach ( var item in output.CommonConditions )
						//{
						//	if ( item.Requires != null && item.Requires.Any() )
						//	{
						//		output.Requires = AppendConditions( item.Requires, output.Requires );
						//	}
						//	if ( item.Recommends != null && item.Recommends.Any() )
						//	{
						//		output.Recommends = AppendConditions( item.Recommends, output.Recommends );
						//	}
						//	if ( item.Corequisite != null && item.Corequisite.Any() )
						//	{
						//		output.Corequisite = AppendConditions( item.Requires, output.Corequisite );
						//	}
						//	if ( item.Renewal != null && item.Renewal.Any() )
						//	{
						//		output.Renewal = AppendConditions( item.Renewal, output.Renewal );
						//	}
						//}
					}
				}
				//connection profiles
				if (input.CredentialConnections != null && input.CredentialConnections.Any())
				{
					//var list = input.CredentialConnections.Where( s => s.ConditionSubTypeId == 2 && s.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor ).ToList();
					//foreach ( var item in input.CredentialConnections )
					//{
					//	//some default for 1??
					//	if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFor )
					//		input.IsAdvancedStandingFor.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_AdvancedStandingFrom )
					//		input.AdvancedStandingFrom.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_PreparationFor )
					//		input.IsPreparationFor.Add( item );
					//	else if ( item.ConditionSubTypeId == 2 && item.ConnectionProfileTypeId == Entity_ConditionProfileManager.ConnectionProfileType_PreparationFrom )
					//		input.PreparationFrom.Add( item );
					//	{
					//		//????;
					//	}


					//}
				}
				output.AdvancedStandingFrom = ServiceHelper.MapToConditionProfiles( input.AdvancedStandingFrom, searchType );
				output.IsAdvancedStandingFor = ServiceHelper.MapToConditionProfiles( input.IsAdvancedStandingFor, searchType );
				//
				output.PreparationFrom = ServiceHelper.MapToConditionProfiles( input.PreparationFrom, searchType );
				output.IsPreparationFor = ServiceHelper.MapToConditionProfiles( input.IsPreparationFor, searchType );
				//
				output.IsRequiredFor = ServiceHelper.MapToConditionProfiles( input.IsRequiredFor, searchType );
				output.IsRecommendedFor = ServiceHelper.MapToConditionProfiles( input.IsRecommendedFor, searchType );
				//check for any inverse connections 
				//if hasPartOfConditionProfile, add to appropriate connection. 
				if (input.IsPartOfConditionProfile != null && input.IsPartOfConditionProfile.Any())
				{
					//the parent will be the target
					foreach (var item in input.IsPartOfConditionProfile)
					{
						if (item.ParentCredential != null && item.ParentCredential.Id > 0)
						{
							//see what we get with a straight map and then add the target credential
							var cp = ServiceHelper.MapToConditionProfile( item, "credential" );
							if (cp != null)
							{
								// 
								var clist = new List<Credential>();
								clist.Add( item.ParentCredential );
								cp.TargetCredential = ServiceHelper.MapCredentialToAJAXSettings( clist, "Condition Target {0} Credential(s)" );
								//proposed:
								cp.Description = "";
								cp.Name = "The following are 'Required For' this credential";
								//initially assume required, then customize
								if (output.IsRequiredFor == null)
									output.IsRequiredFor = new List<WMA.ConditionProfile>();
								else
								{
									//Should also check for dups if both sides were published
								}
								output.IsRequiredFor.Add( cp );
							}
						}
						//else if ( item.ParentLearningOpportunity != null && item.ParentLearningOpportunity.Id > 0 )
						//{
						//	//see what we get with a straight map and then add the target credential
						//	var cp = ServiceHelper.MapToConditionProfile( item, "LearningOpportunity" );
						//	if ( cp != null )
						//	{
						//		// could get many required for this way instead of one with many credentials
						//		var list = new List<ThisResource>();
						//		list.Add( item.ParentLearningOpportunity );
						//		cp.TargetLearningOpportunity = ServiceHelper.MapLearningOppToAJAXSettings( list, "Condition Target {0} Learning Opportunities" );

						//		if ( output.IsRequiredFor == null )
						//			output.IsRequiredFor = new List<WMA.ConditionProfile>();
						//		else
						//		{
						//			//Should also check for dups if both sides were published
						//		}
						//		output.IsRequiredFor.Add( cp );
						//	}
						//}
					}
				}
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 2, Name: {0}, Id: {1}", input.Name, input.Id ) );

			}
			//====================================
			//the following can be made a common method
			var dataMergedRequirements = new MergedConditions();
			var dataMergedRecommendations = new MergedConditions();
			var dataConnections = new ConnectionData();
			ServiceHelper.GetAllChildren( dataMergedRequirements, dataMergedRecommendations, dataConnections, input, null, null );
			//now pull out estimated durations
			if (dataMergedRequirements.TargetCredential != null && dataMergedRequirements.TargetCredential.Any())
			{
				//21-07-12 currently not being used.
				output.CredentialEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.CredentialsSansSelf( input.Id ), "Estimated Time to Complete Required Embedded Credentials" );
			}
			if (dataMergedRequirements.TargetAssessment != null && dataMergedRequirements.TargetAssessment.Any())
			{
				//output.AssessmentEstimatedDuration = ServiceHelper.GetAllDurationsOLD( dataMergedRequirements.TargetAssessment, "Estimated Time to Complete Required Assessments" );
				output.AssessmentEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.TargetAssessment, "Estimated Time to Complete Required Assessments" );
			}
			if (dataMergedRequirements.TargetLearningOpportunity != null && dataMergedRequirements.TargetLearningOpportunity.Any())
			{
				output.LearningOpportunityEstimatedDuration = ServiceHelper.GetAllDurations( dataMergedRequirements.TargetLearningOpportunity, "Estimated Time to Complete Required Learning Opportunities" );
			}


			//competencies
			var started2 = DateTime.Now;
			var dataAllCompetencies = ServiceHelper.GetAllCompetencies( new List<WMP.ConditionProfile>() { dataMergedRequirements }, true );
			var saveDuration = DateTime.Now.Subtract( started2 );
			LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".MapToAPI. ServiceHelper.GetAllCompetencies Duration: {0:N2} seconds", saveDuration.TotalSeconds ) );
			started2 = DateTime.Now;
			//
			output.RequiresCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Requires {#} Competenc{ies}", dataAllCompetencies.RequiresByFramework );
			saveDuration = DateTime.Now.Subtract( started2 );
			LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".MapToAPI. ServiceHelper.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail Duration: {0:N2} seconds", saveDuration.TotalSeconds ) );
			started2 = DateTime.Now;
			//
			output.AssessesCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Assesses {#} Competenc{ies}", dataAllCompetencies.AssessesByFramework );
			saveDuration = DateTime.Now.Subtract( started2 );
			LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".MapToAPI. ServiceHelper.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail Duration: {0:N2} seconds", saveDuration.TotalSeconds ) );
			started2 = DateTime.Now;
			//
			//slow?
			output.TeachesCompetencies = API.CompetencyFrameworkServices.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail( "Teaches {#} Competenc{ies}", dataAllCompetencies.TeachesByFramework );
			saveDuration = DateTime.Now.Subtract( started2 );
			LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".MapToAPI. ServiceHelper.ConvertCredentialAlignmentObjectFrameworkProfileToAJAXSettingsForDetail Duration: {0:N2} seconds", saveDuration.TotalSeconds ) );
			//=======================================
			try
			{
				//
				if (input.CommonCosts != null && input.CommonCosts.Any())
				{
					output.CommonCosts = ServiceHelper.MapCostManifests( input.CommonCosts, searchType );
					//output.EstimatedCost = new List<Models.Elastic.CostProfile>();
					//foreach ( var item in output.CommonCosts )
					//{
					//	output.EstimatedCost.AddRange( item.EstimatedCost );
					//}
					//output.CommonCosts = null;
				}

				if (input.EstimatedCost != null && input.EstimatedCost.Any())
				{
					if (output.EstimatedCost == null)
						output.EstimatedCost = new List<Models.Elastic.CostProfile>();

					var estimatedCost = ServiceHelper.MapCostProfiles( input.EstimatedCost, searchType );
					if (estimatedCost != null && estimatedCost.Any())
						output.EstimatedCost.AddRange( estimatedCost );
				}
				//loop costs
				if (input.Requires.SelectMany( x => x.TargetLearningOpportunity.Where( y => y.EstimatedCost.Count() + y.CommonCosts.Count() > 0 ) ).Count() > 0)
				{
					var list = input.Requires.SelectMany( x => x.TargetLearningOpportunity ).ToList();
					foreach (var item in list)
					{
						if (item.CommonCosts.Any() || item.EstimatedCost.Any())
						{
							var commonCosts = ServiceHelper.MapCostManifests( item.CommonCosts, searchType );
							output.LearningOpportunityCost = new List<Models.Elastic.CostProfile>();
							if (commonCosts != null && commonCosts.Any())
							{
								foreach (var cc in commonCosts)
								{
									output.LearningOpportunityCost.AddRange( cc.EstimatedCost );
								}
							}
						}
						//
						if (item.EstimatedCost != null && item.EstimatedCost.Any())
						{
							if (output.LearningOpportunityCost == null)
								output.LearningOpportunityCost = new List<Models.Elastic.CostProfile>();

							var estimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
							if (estimatedCost != null && estimatedCost.Any())
								output.LearningOpportunityCost.AddRange( estimatedCost );
						}
					}
				}
				//asmt costs
				if (input.Requires.SelectMany( x => x.TargetAssessment.Where( y => y.EstimatedCost.Count() + y.CommonCosts.Count() > 0 ) ).Count() > 0)
				{
					var list = input.Requires.SelectMany( x => x.TargetAssessment ).ToList();
					foreach (var item in list)
					{
						if (item.CommonCosts.Any() || item.EstimatedCost.Any())
						{
							var commonCosts = ServiceHelper.MapCostManifests( item.CommonCosts, searchType );
							output.AssessmentCost = new List<Models.Elastic.CostProfile>();
							if (commonCosts != null && commonCosts.Any())
							{
								foreach (var cc in commonCosts)
								{
									output.AssessmentCost.AddRange( cc.EstimatedCost );
								}
							}
						}
						//
						if (item.EstimatedCost.Any() || item.EstimatedCost.Any())
						{
							if (output.AssessmentCost == null)
								output.AssessmentCost = new List<Models.Elastic.CostProfile>();

							var estimatedCost = ServiceHelper.MapCostProfiles( item.EstimatedCost, searchType );
							if (estimatedCost != null && estimatedCost.Any())
								output.AssessmentCost.AddRange( estimatedCost );
						}
					}
				}
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 3, Name: {0}, Id: {1}", input.Name, input.Id ) );
			}
			//
			try
			{
				output.AggregateData = ServiceHelper.MapToAggregateDataProfile( input.AggregateData, searchType );
				if (output.AggregateData != null)
				{
					//hmm check for dataSetProfile to add to RegistryDataList.
					//Might be better to do this in the managers
				}
				//TBD - need to exclude those that are already in the AggregateDataProfile ==> try to handle this in the managers!
				output.ExternalDataSetProfiles = ServiceHelper.MapToDatasetProfileList( input.ExternalDataSetProfiles, searchType );
				//could add these to RegistryDataList??
				if (output.ExternalDataSetProfiles != null && output.ExternalDataSetProfiles.Any())
				{
                    foreach (var item in output.ExternalDataSetProfiles)
					{
						if ( item == null) 
							continue;

                        var regData = ServiceHelper.FillRegistryData( item.CTID, searchType );
						output.RegistryDataList.Add( regData );
					}
				}
			}
			catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 4, Name: {0}, Id: {1}", input.Name, input.Id ) );
			}
			//
			if ( input.FinancialAssistance != null && input.FinancialAssistance.Any() )
			{
				output.FinancialAssistance = ServiceHelper.MapFinancialAssistanceProfiles( input.FinancialAssistance, searchType );
			}
			//
			output.CredentialId = input.CredentialId;
			output.ISICV4 = input.ISICV4;
			//InLanguage

			//
			output.Identifier = ServiceHelper.MapIdentifierValue( input.Identifier );
			//
			MapProcessProfiles( input, ref output );
			output.SameAs = ServiceHelper.MapTextValueProfileTextValue( input.SameAs );
			//
			//output.ProcessStandards = input.ProcessStandards;
			//output.ProcessStandardsDescription = input.ProcessStandardsDescription;
			output.ProcessStandards = ServiceHelper.MapPropertyLabelLink( input.ProcessStandards, "Process Standards", input.ProcessStandardsDescription );

			//
			output.Revocation = ServiceHelper.MapRevocationProfile( searchType, input.Revocation );

			//these are can be links to existing credentials, likely to be in finder
			output.LatestVersion = ServiceHelper.MapPropertyLabelLink( input.LatestVersion, "Latest Version" );
			output.NextVersion = ServiceHelper.MapPropertyLabelLink( input.NextVersion, "Next Version" );
			output.PreviousVersion = ServiceHelper.MapPropertyLabelLink( input.PreviousVersion, "Previous Version" );
			output.Supersedes = ServiceHelper.MapPropertyLabelLink( input.Supersedes, "Supersedes" );
			output.SupersededBy = ServiceHelper.MapPropertyLabelLink( input.SupersededBy, "Superseded By" );
			//
			output.TargetPathway = ServiceHelper.MapPathwayToAJAXSettings( input.TargetPathway, "Has {0} Target Pathway(s)" );
			output.RelatedActions = ServiceHelper.MapResourceSummaryAJAXSettings( input.RelatedAction, "CredentialingAction" );

			//
			output.VersionIdentifier = ServiceHelper.MapIdentifierValue( input.VersionIdentifier, "Version Identifier" );
			try
			{
				MapJurisdictions( input, ref output );
				//
				//QA received
				//==> need to exclude 30-published by 
				if ( input.OrganizationRole.Any() )
				{
					output.QAReceived = ServiceHelper.MapQAReceived( input.OrganizationRole, searchType );
					//old
					var renewedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy );
					var revokedBy2 = ServiceHelper.MapRoleReceived( input.OrganizationRole, searchType, Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy );
					//new
					output.RenewedBy = ServiceHelper.MapOutlineToAJAX( renewedBy2, "Renewed by {0} Organization(s)" );
					output.RevokedBy = ServiceHelper.MapOutlineToAJAX( revokedBy2, "Revoked by {0} Organization(s)" );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, string.Format( thisClassName + ".MapToAPI. Section 5, Name: {0}, Id: {1}", input.Name, input.Id ) );
			}
			//
			var work = new List<WMA.Outline>();
			if ( input.HasTransferValueProfile != null && input.HasTransferValueProfile.Any() )
			{
				
				foreach ( var target in input.HasTransferValueProfile )
				{
					if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
						work.Add( ServiceHelper.MapToOutline( target, "transfervalue" ) );
				}
				//
				output.HasTransferValue = new AJAXSettings()
				{
					Label = string.Format( "Has {0} Transfer Values", input.HasTransferValueProfile.Count ),
					Total = input.HasTransferValueProfile.Count
				};
				List<object> obj = work.Select( f => ( object )f ).ToList();
				output.HasTransferValue.Values = obj;
			}
            //
            if ( input.HasSupportService?.Count > 0 )
            {
                work = new List<WMA.Outline>();
                foreach ( var target in input.HasSupportService )
                {
                    if ( target != null && !string.IsNullOrWhiteSpace( target.Name ) )
                        work.Add( ServiceHelper.MapToOutline( target, target.EntityType ) );
                }
                output.HasSupportService = ServiceHelper.MapOutlineToAJAX( work, "Has {0} Support Services" );
            }
            //
            return output;
		}

		private static List<WMA.ConditionProfile> AppendConditions( List<WMA.ConditionProfile> input, List<WMA.ConditionProfile> existing )
		{
			if ( input != null && input.Any() )
			{
				if ( existing == null )
					existing = new List<WMA.ConditionProfile>();
				existing.AddRange( input );
			}

			return existing;
		}

		private static void MapJurisdictions( ThisResource input, ref OutputResource output )
		{
			if ( input.Jurisdiction != null && input.Jurisdiction.Any() )
			{
				output.Jurisdiction = ServiceHelper.MapJurisdiction( input.Jurisdiction );

			}
			//return if no assertions
			if ( input.JurisdictionAssertions == null || !input.JurisdictionAssertions.Any() )
			{
				return;
			}
			//TODO - return all in a group or individual?
			//output.JurisdictionAssertion = ServiceHelper.MapJurisdiction( input.JurisdictionAssertions, "OfferedIn" );
			//OR

			var assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_AccreditedBy ).ToList();
			if ( assertions != null && assertions.Any() )
			{
				output.AccreditedIn = ServiceHelper.MapJurisdiction( assertions, "AccreditedIn" );
			}
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_ApprovedBy ).ToList();
			if ( assertions != null && assertions.Any() )
				output.ApprovedIn = ServiceHelper.MapJurisdiction( assertions, "ApprovedIn" );
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_OFFERED_BY ).ToList();
			if ( assertions != null && assertions.Any() )
				output.OfferedIn = ServiceHelper.MapJurisdiction( assertions, "OfferedIn" );
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RecognizedBy ).ToList();
			if ( assertions != null && assertions.Any() )
				output.RecognizedIn = ServiceHelper.MapJurisdiction( assertions, "RecognizedIn" );
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RegulatedBy ).ToList();
			if ( assertions != null && assertions.Any() )
				output.RegulatedIn = ServiceHelper.MapJurisdiction( assertions, "RegulatedIn" );
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RenewedBy ).ToList();
			if ( assertions != null && assertions.Any() )
				output.RenewedIn = ServiceHelper.MapJurisdiction( assertions, "RenewedIn" );
			//
			assertions = input.JurisdictionAssertions.Where( s => s.AssertedInTypeId == Entity_AgentRelationshipManager.ROLE_TYPE_RevokedBy ).ToList();
			if ( assertions != null && assertions.Any() )
				output.RevokedIn = ServiceHelper.MapJurisdiction( assertions, "RevokedIn" );
		}
		private static void MapProcessProfiles( ThisResource input, ref OutputResource output )
		{
			//will use summary, where the UI will get the detail on click
			if ( input.ProcessProfilesSummary != null && input.ProcessProfilesSummary.Any() )
			{
				var url = string.Format( "detail/ProcessProfile/{0}/", input.RowId.ToString() );
				output.ProcessProfiles = null;// new List<AJAXSettings>();
				foreach ( var item in input.ProcessProfilesSummary )
				{
					var ajax = new AJAXSettings()
					{
						Label = item.Name,
						Description = "",
						Total = item.Totals,
						//
						URL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
						TestURL = ServiceHelper.finderApiSiteURL + url + item.Id.ToString(),
					};
					//not sure we need this as part of the URL
					var qd = new ProcessProfileAjax()
					{
						Id = input.RowId.ToString(),
						ProcessTypeId = item.Id,
					};
					ajax.QueryData = qd;
					/*need to know
					 * endpoint: detail/processprofile
					 * id:		 input.RowId
					 * processProfileTypeId: item.Id?
					 * 
					 */
					//var filter = ( new MD.LabelLink()
					//{
					//	Label = item.Name,
					//	Count = item.Totals,
					//} );
					var processType = item.Name.Replace( " ", "" );
					switch ( processType )
					{
						case "AdministrationProcess":
							output.AdministrationProcess = ajax;
							break;
						case "AppealProcess":
							output.AppealProcess = ajax;
							break;
						case "ComplaintProcess":
							output.ComplaintProcess = ajax;
							break;
						case "DevelopmentProcess":
							output.DevelopmentProcess = ajax;
							break;
						case "MaintenanceProcess":
							output.MaintenanceProcess = ajax;
							break;
						case "ReviewProcess":
							output.ReviewProcess = ajax;
							break;
						case "RevocationProcess":
							output.RevocationProcess = ajax;
							break;
						default:
							break;
					}

					//output.ProcessProfiles.Add( ajax );
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

		private static bool UsingCache( int id, CredentialRequest request, ref OutputResource output )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
			string key = "credentialbyapi_" + id.ToString();

			if ( request.AllowCaching
				&& HttpRuntime.Cache[ key ] != null && cacheMinutes > 0 )
			{
				try
				{
					var cache = ( CachedEntity )HttpRuntime.Cache[ key ];
					if ( cache.LastUpdated > maxTime )
					{
						LoggingHelper.DoTrace( BaseFactory.appSectionDurationTraceLevel, string.Format( thisClassName + ".UsingCache === Using cached version of record, Id: {0}, {1}", cache.Item.Meta_Id, cache.Item.Name ) );
						output = cache.Item;
						return true;
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, thisClassName + ".UsingCache === exception " + ex.Message );
				}
			}
			else
			{
				LoggingHelper.DoTrace( 8, thisClassName + string.Format( ".UsingCache === Will retrieve full version of record, Id: {0}", id ) );
			}
			return false;
		}
		private static void CacheEntity( OutputResource entity )
		{
			int cacheMinutes = UtilityManager.GetAppKeyValue( "credentialCacheMinutes", 0 );
			DateTime maxTime = DateTime.Now.AddMinutes( cacheMinutes * -1 );
			string key = "credentialapi_" + entity.Meta_Id.ToString();

			if ( key.Length > 0 && cacheMinutes > 0 )
			{
				try
				{
					var newCache = new CachedEntity()
					{
						Item = entity,
						LastUpdated = DateTime.Now
					};
					if ( HttpContext.Current != null )
					{
						if ( HttpContext.Current.Cache[ key ] != null )
						{
							HttpRuntime.Cache.Remove( key );
							HttpRuntime.Cache.Insert( key, newCache );

							LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".CacheEntity $$$ Updating cached version of record, Id: {0}, {1}", entity.Meta_Id, entity.Name ) );
						}
						else
						{
							LoggingHelper.DoTrace( 6, string.Format( thisClassName + ".CacheEntity ****** Inserting new cached version of record, Id: {0}, {1}", entity.Meta_Id, entity.Name ) );

							System.Web.HttpRuntime.Cache.Insert( key, newCache, null, DateTime.Now.AddMinutes( cacheMinutes ), TimeSpan.Zero );
						}
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 6, thisClassName + ".CacheEntity. Updating Cache === exception " + ex.Message );
				}
			}

		}
	}
	public class CachedCredential
	{
		public CachedCredential()
		{
			LastUpdated = DateTime.Now;
		}
		public DateTime LastUpdated { get; set; }
		public OutputResource Item { get; set; }

	}
		//

		//
}
