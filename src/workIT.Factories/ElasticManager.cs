﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using workIT.Data.Tables;
using workIT.Models;
using workIT.Models.Elastic;
using workIT.Models.Search;
using workIT.Utilities;
using APIM = workIT.Models.API;
using MC = workIT.Models.Common;
using ManyInOneIndex = workIT.Models.Elastic.GenericIndex;
using PM = workIT.Models.ProfileModels;

namespace workIT.Factories
{
	public class ElasticManager : BaseFactory
	{
		static readonly string thisClassName = "ElasticManager";
		static readonly string illinoisLWIAIdentityType = "illinois:LWIA";
		static readonly string illinoisEDRIdentityType = "illinois:EDR";
		static readonly string LWIAIdentityType = "identifier:LWIA";
		//may wan to make this a config value
		static readonly string DefaultSearchOrder = "newest";
		#region Common Elastic Index
		public static List<MC.CommonSearchSummary> CommonIndex_MapFromElastic( List<ManyInOneIndex> searchResults, int entityTypeId, int pageNbr, int pageSize )
		{
			var list = new List<MC.CommonSearchSummary>();
			int rowNbr = ( pageNbr - 1 ) * pageSize;


			foreach ( var item in searchResults )
			{
				rowNbr++;

				var index = new MC.CommonSearchSummary
				{
					Id = item.Id,
					ResultNumber = rowNbr,

					Name = item.Name,
					FriendlyName = item.FriendlyName,
					RowId = item.RowId,
					Description = item.Description,
					CTID = item.CTID,
					EntityType = item.EntityType,
					EntityTypeId = item.EntityTypeId,
					SubjectWebpage = item.SubjectWebpage,
					PrimaryOrganizationCTID = item.PrimaryOrganizationCTID,
					PrimaryOrganizationId = item.PrimaryOrganizationId,
					PrimaryOrganizationName = item.PrimaryOrganizationName,
					//FrameworkUri = oi.FrameworkUri,
					//CredentialRegistryId = item.CredentialRegistryId,
					EntityStateId = item.EntityStateId,
					Created = item.Created,
					LastUpdated = item.LastUpdated,
                    CostProfilesCount = item.CostProfilesCount,
                    CommonCostsCount = item.CommonCostsCount,
                    CommonConditionsCount = item.CommonConditionsCount,
                    FinancialAidCount = item.FinancialAidCount,

                };
				if ( string.IsNullOrWhiteSpace( index.Name ) )
				{
					if ( !string.IsNullOrWhiteSpace(item.Description))
					{
						index.Name = item.Description?.Length < PortionOfDescriptionToUseForName ? item.Description : item.Description.Substring(0, PortionOfDescriptionToUseForName) + " ...";
					} else
					{
						index.Name = item.EntityType;
					}
				}
				if ( string.IsNullOrWhiteSpace( index.FriendlyName ) )
					index.FriendlyName = FormatFriendlyTitle( index.Name );
				index.PrimaryOrganizationFriendlyName = FormatFriendlyTitle( index.PrimaryOrganizationName );
				//TBD for format
				//index. = oi.TransferValueGraph;

				if ( IsValidDate( item.Created ) )
					index.Created = item.Created;

				if ( IsValidDate( item.LastUpdated ) )
					index.LastUpdated = item.LastUpdated;

				if ( IsValidDate( item.StartDate ) )
					index.StartDate = DateTime.Parse( item.StartDate ).ToString( "yyyy-MM-dd" );
				if ( IsValidDate( item.EndDate ) )
					index.EndDate = DateTime.Parse( item.EndDate ).ToString( "yyyy-MM-dd" );

				if ( IsValidDate( item.DateEffective ) )
					index.DateEffective = DateTime.Parse( item.DateEffective ).ToString( "yyyy-MM-dd" );
				if ( IsValidDate( item.ExpirationDate ) )
					index.ExpirationDate = DateTime.Parse( item.ExpirationDate ).ToString( "yyyy-MM-dd" );


				if ( item.Industries != null && item.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( item.Occupations != null && item.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( item.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );
				//
				index.Subjects = item.SubjectAreas;

				#region transfer value specific - which is moving away from generic
				index.TransferValueForCredentialsCount = item.TransferValueForCredentialsCount;
				index.TransferValueFromCredentialsCount = item.TransferValueFromCredentialsCount;

				index.TransferValueForAssessmentsCount = item.TransferValueForAssessmentsCount;
				index.TransferValueFromAssessmentsCount = item.TransferValueFromAssessmentsCount;

				index.TransferValueForLoppsCount = item.TransferValueForLoppsCount;
				index.TransferValueFromLoppsCount = item.TransferValueFromLoppsCount;

				index.TransferIntermediariesFor = item.TransferIntermediariesFor != null ? item.TransferIntermediariesFor.Count : 0;
				//
				if (item.ProvidesTransferValueFor.Count>0 ) 
					index.ProvidesTransferValueFor = Fill_CodeItemResults( item.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				if ( item.ReceivesTransferValueFrom.Count > 0 )
					index.ReceivesTransferValueFrom = Fill_CodeItemResults( item.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				#endregion

				//support service
				index.AccommodationTypes = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_ACCOMMODATION ).ToList(), CodesManager.PROPERTY_CATEGORY_ACCOMMODATION, false, false );
                index.SupportServiceCategories = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY ).ToList(), CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY, false, false );
                index.DeliveryMethodTypes = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
				//
				//DSP
				//or maybe should format here???
				if ( !string.IsNullOrWhiteSpace( item.AboutCredentialList ) )
					index.AboutCredentials = Fill_CodeItemResults( item.AboutCredentialList, CodesManager.ENTITY_TYPE_CREDENTIAL, false, false );
				else if ( !string.IsNullOrWhiteSpace( item.AboutLoppList ) )
					index.AboutLearningOpportunities = Fill_CodeItemResults( item.AboutLoppList, CodesManager.ENTITY_TYPE_LEARNING_OPP_PROFILE, false, false );

				//
				//credentialing action
				//reuse about
				if ( !string.IsNullOrWhiteSpace( item.AboutCredentialList ) )
					index.AboutCredentials = Fill_CodeItemResults( item.AboutCredentialList, CodesManager.ENTITY_TYPE_CREDENTIAL, false, false );
				//
				if ( item.ResourceDetail != null )
				{
					index.ResourceDetail = item.ResourceDetail;
				}
                //addresses
                index.AvailableAt = item.Addresses.Select( x => new MC.Address
                {
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    StreetAddress = x.StreetAddress,
                    AddressLocality = x.AddressLocality,
                    AddressRegion = x.AddressRegion,
                    PostalCode = x.PostalCode,
                    Identifier = x.Identifier,
                    AddressCountry = x.AddressCountry
                } ).ToList();

                //scheduled offering
                index.OfferFrequencyType = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY ).ToList(), CodesManager.PROPERTY_CATEGORY_OFFER_FREQUENCY, false, false );

                index.ScheduleFrequencyType = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY ).ToList(), CodesManager.PROPERTY_CATEGORY_SCHEDULE_FREQUENCY, false, false );
                index.ScheduleTiming = Fill_CodeItemResults( item.Properties.Where( x => x.CategoryId == ( int ) CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING ).ToList(), CodesManager.PROPERTY_CATEGORY_SCHEDULE_TIMING, false, false );

                list.Add( index );
			}

			return list;
		}
		#endregion
		#region Credential Elastic Index
		public static List<CredentialIndex> Credential_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new CredentialIndex();
			var list = new List<CredentialIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 6, "Credential_SearchForElastic - Starting. filter\r\n " + filter );
			bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );
			bool usingEntityLastUpdatedDate = UtilityManager.GetAppKeyValue( "usingEntityLastUpdatedDateForIndexLastUpdated", true );
			int cntr = 0;
			DateTime started = DateTime.Now;
			//
			//string credentialSearchProc= UtilityManager.GetAppKeyValue( "credentialElasticSearchProc", "[Credential.ElasticSearch]" );
			//special
			bool populatingCredentialJsonProperties = UtilityManager.GetAppKeyValue( "populatingCredentialJsonProperties", false );

			//
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Credential.ElasticSearch2]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );

					command.CommandTimeout = 600;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", pTotalRows );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						LoggingHelper.DoTrace( 2, "Credential_SearchForElastic - Exception:\r\n " + ex.Message );
						LoggingHelper.LogError( ex, "Credential_SearchForElastic" );
						index = new CredentialIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );
						pTotalRows = -1;
						return list;
					}
				}
			}
			//Used for costs. Only need to get these once. See below. - NA 5/12/2017
			//var currencies = CodesManager.GetCurrencies();
			//var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
			//int costProfilesCount = 0;

			LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic - Page: {0} - loading {1} rows ", pageNumber, result.Rows.Count ) );
			try
			{

				foreach ( DataRow dr in result.Rows )
				{
					cntr++;
					if ( cntr % 200 == 0 )
						LoggingHelper.DoTrace( 2, string.Format( " Page: {0} - loading record: {1}", pageNumber, cntr ) );
					//avgMinutes = 0;
					index = new CredentialIndex
					{
						EntityTypeId = 1,
						Id = GetRowColumn( dr, "Id", 0 ),
						//fine for a full reindex, need algorithm for small updates - could add to sql table
						NameIndex = cntr * 1000,

						//only full entities (3) will be in the index
						EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 ),
						Name = dr[ "Name" ].ToString()
					};
					if ( index.Id == 342 )
					{

					}
					Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
					index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

					if ( !string.IsNullOrWhiteSpace( dr[ "AlternateName" ].ToString() ) )
						index.AlternateNames.Add( dr[ "AlternateName" ].ToString() );

					index.FriendlyName = FormatFriendlyTitle( index.Name );

					index.SubjectWebpage = dr[ "SubjectWebpage" ].ToString();
					index.ImageURL = GetRowColumn( dr, "ImageUrl", string.Empty );

					string rowId = dr[ "EntityUid" ].ToString();
					index.RowId = new Guid( rowId );

					index.Description = dr[ "Description" ].ToString();
					//21-03-22 mparsons - need to anticipate handling creds etc. with no owner, just an offeredBy
					//index.OwnerOrganizationId = GetRowPossibleColumn( dr, "OwningOrganizationId", 0 );
					index.PrimaryOrganizationId = Int32.Parse( dr[ "OwningOrganizationId" ].ToString() );
					//used for autocomplete and phrase prefix queries
					index.PrimaryOrganizationName = dr[ "OwningOrganization" ].ToString();
					index.NameOrganizationKey = index.Name;
					index.ListTitle = index.Name;
					if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf(index.PrimaryOrganizationName) == -1)
					{
						index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName ;
						//ListTitle is not used anymore
						index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
					}
					
					//add helpers
					index.PrimaryOrganizationCTID = dr[ "OwningOrganizationCtid" ].ToString();
					//index.PrimaryOrganizationId = index.OwnerOrganizationId;
					//index.PrimaryOrganizationName = index.OwnerOrganizationName;

					index.CTID = dr[ "CTID" ].ToString();
					//
					index.CredentialType = dr[ "CredentialType" ].ToString();

					index.CredentialTypeSchema = dr[ "CredentialTypeSchema" ].ToString();
					index.CredentialStatusId = GetRowColumn( dr, "CredentialStatusId", 0 );
					index.CredentialStatus = dr[ "CredentialStatus" ].ToString();
					if ( !string.IsNullOrWhiteSpace( index.CredentialStatus )
						&& index.CredentialStatus != "Active" && index.Name.IndexOf( index.CredentialStatus ) == -1 )
					{
						//index.Name += string.Format( " ({0})", index.CredentialStatus );
					}
					

					index.CredentialTypeId = Int32.Parse( dr[ "CredentialTypeId" ].ToString() );
					//index.CredentialRegistryId = dr[ "CredentialRegistryId" ].ToString();
					try
					{
						var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                        if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                        {
                            var resource = JsonConvert.DeserializeObject<APIM.CredentialDetail>( resourceDetail );
                            index.ResourceDetail = JObject.FromObject( resource );
                            //TODO - start extracting from ResourceDetail rather than more stuff and joins in the proc
                            //addresses and QA would be a good start

                            HandleResourceHasSupportService( index, resource.HasSupportService );
							if(resource.ProvidesTransferValueFor != null )
                            {
								index.ProvidesTransferValueFor = HandleResource( resource.ProvidesTransferValueFor );

							}
							if ( resource.ReceivesTransferValueFrom != null )
							{
								index.ReceivesTransferValueFrom = HandleResource( resource.ReceivesTransferValueFrom );

							}

						}
                    }
                    catch ( Exception ex )
                    {
                        LoggingHelper.LogError( ex, string.Format( "Credential_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                    }
					//
					//#region TransferValues 
					//string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
					//if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
					//{
					//	if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
					//	{
					//		providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
					//	}
					//	providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
					//	var xDoc = new XDocument();
					//	xDoc = XDocument.Parse( providesTransferValueFor );
					//	foreach ( var child in xDoc.Root.Elements() )
					//	{
					//		var property = new IndexProperty
					//		{
					//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
					//			Id = ( int ) child.Attribute( "ResourceId" )
					//		};

					//		index.ProvidesTransferValueFor.Add( property );
					//	}
					//}
					//string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
					//if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
					//{
					//	if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
					//	{
					//		receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
					//	}
					//	receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
					//	var xDoc = new XDocument();
					//	xDoc = XDocument.Parse( receivesTransferValueFor );
					//	foreach ( var child in xDoc.Root.Elements() )
					//	{
					//		var property = new IndexProperty
					//		{
					//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
					//			Id = ( int ) child.Attribute( "ResourceId" )
					//		};

					//		index.ReceivesTransferValueFrom.Add( property );
					//	}
					//}
					//#endregion
					//
					string date = GetRowColumn( dr, "DateEffective", string.Empty );
					if ( IsValidDate( date ) )
						index.DateEffective = ( DateTime.Parse( date ).ToString( "yyyy-MM-dd" ) );
					
					date = GetRowColumn( dr, "Created", string.Empty );
					if ( IsValidDate( date ) )
						index.Created = DateTime.Parse( date );
					date = GetRowColumn( dr, "LastUpdated", string.Empty );
					if ( IsValidDate( date ) )
						index.LastUpdated = DateTime.Parse( date );
					//define LastUpdated to be EntityLastUpdated
					//TODO - add means to skip this for mass updates
					//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
					//if ( IsValidDate( date ) )
					//	index.LastUpdated = DateTime.Parse( date );

					index.AvailableOnlineAt = dr[ "AvailableOnlineAt" ].ToString();
					//AverageMinutes is a rough approach to sorting. If present, get the duration profiles

					//index.EstimatedTimeToEarn = GetRowPossibleColumn( dr, "AverageMinutes", 0 );
					//index.EstimatedTimeToEarn = Int32.Parse( dr[ "AverageMinutes" ].ToString() );
					index.IsAQACredential = GetRowColumn( dr, "IsAQACredential", false );
					index.IsNonCredit = GetRowColumn( dr, "IsNonCredit", false );
					if ( index.IsNonCredit )
					{
						index.Keyword.Add( "IsNonCredit" );
						index.Keyword.Add( "Is Non-Credit" );
					}
					#region counts 
					index.RequiresCompetenciesCount = Int32.Parse( dr[ "RequiresCompetenciesCount" ].ToString() );
					index.LearningOppsCompetenciesCount = Int32.Parse( dr[ "LearningOppsCompetenciesCount" ].ToString() );
					index.AssessmentsCompetenciesCount = Int32.Parse( dr[ "AssessmentsCompetenciesCount" ].ToString() );
					index.HasPartCount = Int32.Parse( dr[ "HasPartCount" ].ToString() );
					index.IsPartOfCount = Int32.Parse( dr[ "IsPartOfCount" ].ToString() );
					index.RequiresCount = Int32.Parse( dr[ "RequiresCount" ].ToString() );
					index.RecommendsCount = Int32.Parse( dr[ "RecommendsCount" ].ToString() );
					index.EntryConditionCount = Int32.Parse( dr[ "EntryConditionCount" ].ToString() );

					index.RequiredForCount = Int32.Parse( dr[ "isRequiredForCount" ].ToString() );
					index.IsRecommendedForCount = Int32.Parse( dr[ "IsRecommendedForCount" ].ToString() );
					index.RenewalCount = GetRowPossibleColumn( dr, "RenewalCount", 0 );
					index.IsAdvancedStandingForCount = Int32.Parse( dr[ "IsAdvancedStandingForCount" ].ToString() );
					index.AdvancedStandingFromCount = Int32.Parse( dr[ "AdvancedStandingFromCount" ].ToString() );
					index.PreparationForCount = Int32.Parse( dr[ "isPreparationForCount" ].ToString() );

					index.PreparationFromCount = Int32.Parse( dr[ "isPreparationFromCount" ].ToString() );
					//20-10-12 mp added back NumberOfCostProfileItems (had been commented)
					index.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );
					index.TotalCost = GetRowPossibleColumn( dr, "TotalCost", 0 );
					index.CostProfileCount = Int32.Parse( dr[ "costProfilesCount" ].ToString() );

					index.CommonConditionsCount = Int32.Parse( dr[ "CommonConditionsCount" ].ToString() );
					index.CommonCostsCount = Int32.Parse( dr[ "CommonCostsCount" ].ToString() );
					index.FinancialAidCount = Int32.Parse( dr[ "FinancialAidCount" ].ToString() );
					index.EmbeddedCredentialsCount = Int32.Parse( dr[ "EmbeddedCredentialsCount" ].ToString() );

					index.RequiredAssessmentsCount = Int32.Parse( dr[ "RequiredAssessmentsCount" ].ToString() );
					index.RequiredCredentialsCount = Int32.Parse( dr[ "RequiredCredentialsCount" ].ToString() );
					index.RequiredLoppCount = Int32.Parse( dr[ "RequiredLoppCount" ].ToString() );

					index.RecommendedAssessmentsCount = Int32.Parse( dr[ "RecommendedAssessmentsCount" ].ToString() );
					index.RecommendedCredentialsCount = Int32.Parse( dr[ "RecommendedCredentialsCount" ].ToString() );
					index.RecommendedLoppCount = Int32.Parse( dr[ "RecommendedLoppCount" ].ToString() );

					index.BadgeClaimsCount = Int32.Parse( dr[ "badgeClaimsCount" ].ToString() );
					index.RevocationProfilesCount = Int32.Parse( dr[ "RevocationProfilesCount" ].ToString() );
					index.ProcessProfilesCount = Int32.Parse( dr[ "ProcessProfilesCount" ].ToString() );
					//21-03-29 add aggregateDataProfile and dataSetProfile - what to do for summary?
					//			- will this be too costly for large numbers? If so, the summary profile could be assigned at import and stored. 
					index.AggregateDataProfileCount = GetRowColumn( dr, "AggregateDataProfileCount", 0 ); 
					//or would this be better to do in search results (saves having to re-index)
					//the benefit would be saving the hit now rather than when doing get search results - actually this would get lost with Nate's approach
					index.DataSetProfileCount = GetRowColumn( dr, "DataSetProfileCount", 0 );

					if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
					{
						//21-04-19 - decided to use a simple generic summary for all outcome data
						//index.AggregateDataProfileSummary = Entity_AggregateDataProfileManager.GetSummary( index.RowId, index.Name ) ;
						index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
					}
					index.JurisdictionProfilesCount = GetRowPossibleColumn( dr, "JurisdictionProfilesCount", 0 );
					#endregion
					//
					//21-03-29 - skipping holders, earnings and employment now
					//index.HoldersProfileCount = Int32.Parse( dr[ "HoldersProfileCount" ].ToString() );
					//if ( index.HoldersProfileCount > 0 )
					//	index.HoldersProfileSummary = Entity_HoldersProfileManager.GetSummary( index.RowId );
					////
					//index.EarningsProfileCount = Int32.Parse( dr[ "EarningsProfileCount" ].ToString() );
					//if ( index.EarningsProfileCount > 0 )
					//	index.EarningsProfileSummary = Entity_EarningsProfileManager.GetSummary( index.RowId );
					////
					//index.EmploymentOutcomeProfileCount = Int32.Parse( dr[ "EmploymentOutcomeProfileCount" ].ToString() );
					//if ( index.EmploymentOutcomeProfileCount > 0 )
					//	index.EmploymentOutcomeProfileSummary = Entity_EmploymentOutcomeProfileManager.GetSummary( index.RowId );

					//
					//#region TransferValues
					//string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
					//if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
					//{
					//	if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
					//	{
					//		providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
					//	}
					//	providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
					//	var xDoc = new XDocument();
					//	xDoc = XDocument.Parse( providesTransferValueFor );
					//	foreach ( var child in xDoc.Root.Elements() )
					//	{
					//		var competency = new IndexProperty
					//		{
					//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
					//			Id = ( int ) child.Attribute( "ResourceId" )
					//		};

					//		index.ProvidesTransferValueFor.Add( competency );
					//	}
					//}
					//string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
					//if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
					//{
					//	if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
					//	{
					//		receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
					//	}
					//	receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
					//	var xDoc = new XDocument();
					//	xDoc = XDocument.Parse( receivesTransferValueFor );
					//	foreach ( var child in xDoc.Root.Elements() )
					//	{
					//		var competency = new IndexProperty
					//		{
					//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
					//			Id = ( int ) child.Attribute( "ResourceId" )
					//		};

					//		index.ReceivesTransferValueFrom.Add( competency );
					//	}
					//}
					//#endregion

					// 8~Is Preparation For~ceterms:isPreparationFor~2
					index.ConnectionsList = dr[ "ConnectionsList" ].ToString();

					//connection type, plus Id, and name of credential
					//8~Is Preparation For~136~MSSC Certified Production Technician (CPT©)~| 8~Is Preparation For~272~MSSC Certified Logistics Technician (CLT©)~
					index.CredentialsList = dr[ "CredentialsList" ].ToString();
					index.IsPartOfList = dr[ "IsPartOfList" ].ToString();
					index.HasPartsList = dr[ "HasPartsList" ].ToString();
					if ( includingHasPartIsPartWithConnections )
					{
						index.CredentialsList += index.IsPartOfList;
						index.CredentialsList += index.HasPartsList;
					}
					string credentialConnections = dr[ "CredentialConnections" ].ToString();
					if ( !string.IsNullOrWhiteSpace( credentialConnections ) )
					{
						Connection conn = new Connection();
						var xDoc = XDocument.Parse( credentialConnections );
						foreach ( var child in xDoc.Root.Elements() )
						{
							conn = new Connection();
							conn.ConnectionType = ( string )child.Attribute( "ConnectionType" ) ?? string.Empty;
							conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

							//do something with counts for this type

							conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
							if ( conn.CredentialId > 0 )
							{
								//add credential
								conn.Credential = ( string )child.Attribute( "CredentialName" ) ?? string.Empty;
								//??????
								conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
								conn.CredentialOrganization = ( string )child.Attribute( "credOrganization" ) ?? string.Empty;
							}
							conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
							if ( conn.AssessmentId > 0 )
							{
								conn.Assessment = ( string )child.Attribute( "AssessmentName" ) ?? string.Empty;
								conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
								conn.AssessmentOrganization = ( string )child.Attribute( "asmtOrganization" ) ?? string.Empty;
							}
							conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
							if ( conn.LoppId > 0 )
							{
								conn.LearningOpportunity = ( string )child.Attribute( "LearningOpportunityName" ) ?? string.Empty;
								conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
								conn.LearningOpportunityOrganization = ( string )child.Attribute( "loppOrganization" ) ?? string.Empty;
							}

							index.Connections.Add( conn );
						}
					}


					#region QualityAssurance and org relationships
					//relationships 
					//may need a fall back to populate the entity cache if no data
					//or only do if a small number or maybe single implying simple reindex
					string agentRelationshipsForEntity = GetRowPossibleColumn( dr, "AgentRelationshipsForEntity" );
					if ( string.IsNullOrWhiteSpace( agentRelationshipsForEntity ) )
					{
						if ( result.Rows.Count == 1 && index.EntityStateId == 3 )
						{
							//NOTE: may want to disable this once everything rebuilt:
							//plus this could always be called, and never updated if there is no owner
							string statusMessage = string.Empty;
							if ( new EntityManager().EntityCacheUpdateAgentRelationshipsForCredential( index.RowId.ToString(), ref statusMessage ) )
							{
								var ec = EntityManager.EntityCacheGetByGuid( index.RowId );
								if ( ec != null && ec.Id > 1 && !string.IsNullOrWhiteSpace( ec.AgentRelationshipsForEntity ) )
								{
									HandleAgentRelationshipsForEntity( ec.AgentRelationshipsForEntity, index );
								}
							}
						}
					}
					else
						HandleAgentRelationshipsForEntity( dr, index );
					
					//handle QA asserted by a third party (versus by the owner)
					HandleDirectAgentRelationshipsForEntity( dr, index );

					//16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
					//QARolesList: includes roleId, roleName
					//1~Accredited~1~Credential| 2~Approved~1~Credential
					//index.QARolesResults = dr[ "QARolesList" ].ToString();

					//AgentAndRoles: includes roleId, roleName, OrgId, and orgName
					//1~Accredited~3~National Commission for Certifying Agencies (NCCA) [ reference ]~Credential
					index.AgentAndRoles = dr[ "AgentAndRoles" ].ToString();

					//QA on owning org
					//This should be combined with the credential QA - see publisher
					//this may not being used properly - no data other than blank and "not applicable" (remove this text). Should be:
					//QAOrgRolesList: includes roleId, roleName, number of organizations in role
					//10~Recognized~2~Organization| 12~Regulated~2~Organization
					//replacing the source
					//index.Org_QARolesList = GetRowPossibleColumn( dr, "QAOrgRolesList" );
					index.Org_QARolesList = dr[ "Org_QARolesList" ].ToString();

					//QAAgentAndRoles - List actual orgIds and names for roles
					//10~Owning Org is Recognized~1105~Accrediting Commission for Community and Junior Colleges - updated~Organization| 10~Owning Org is Recognized~64~AdvancED~Organization| 12~Owning Org is Regulated~55~TESTING_American National Standards Institute (ANSI)~Organization| 12~Owning Org is Regulated~64~AdvancED~Organization
					index.Org_QAAgentAndRoles = dr[ "Org_QAAgentAndRoles" ].ToString();
					//
					HandleDataProvidersForEntity( dr, index );

					#endregion

					#region Subjects
					var subjects = dr[ "Subjects" ].ToString();
					if ( !string.IsNullOrWhiteSpace( subjects ) )
					{
						var xDoc = new XDocument();
						xDoc = XDocument.Parse( subjects );
						foreach ( var child in xDoc.Root.Elements() )
						{
							var cs = new IndexSubject();
							var subject = child.Attribute( "Subject" );
							if ( subject != null )
							{
								cs.Name = subject.Value;

								//source is just direct/indirect, more want the sourceEntityType
								var source = child.Attribute( "Source" );
								if ( source != null )
									cs.Source = source.Value;

								int outputId = 0;
								var entityTypeId = child.Attribute( "EntityTypeId" );
								if ( entityTypeId != null )
									if ( Int32.TryParse( entityTypeId.Value, out outputId ) )
										cs.EntityTypeId = outputId;

								var referenceBaseId = child.Attribute( "ReferenceBaseId" );
								if ( referenceBaseId != null )
									if ( Int32.TryParse( referenceBaseId.Value, out outputId ) )
										cs.ReferenceBaseId = outputId;
								//may dump Subjects, for consistency
								index.Subjects.Add( cs );
								index.SubjectAreas.Add( cs.Name );
							}
						}
						if ( index.Subjects.Count() > 0 )
							index.HasSubjects = true;
					}
					#endregion

					#region Addresses 
					var addresses = dr["Addresses"].ToString();
					//future using json
					var jsonProperties = GetRowPossibleColumn( dr, "JsonProperties" );
					if ( !string.IsNullOrWhiteSpace( jsonProperties ) )
					{
						//20-10-23 - not active and deferred, so skipping
						//21-07-30 mp - JsonProperties has addresses, and Addresses does not!
						//22-04-01 mp - noticed that addresses in JsonProperties will have the short region, and may not have a country. So disable use of this, unless no addresses? The latter is not likely
						//perhaps this temp workaround to skip use of jsonProperties
						//will set populatingCredentialJsonProperties to true, so this step will be skipped and a later step will result in the jsonProperties to be properly set? But then can't really turn off until JP is set properly during import!!!!!
						//this will at least be useful to rebuild the WA stuff. Well probably have to do all
						//	skip for now and leave 'populatingCredentialJsonProperties' as false
						//if ( !populatingCredentialJsonProperties )
						//	HandleAddressesFromJson( index, jsonProperties );

					} else
					{
						//once latter is validated then will no longer look for the xml property
					}
					//- from XML


					
					if ( !string.IsNullOrWhiteSpace( addresses ) )
					{
						//check if there are addresses from json. If not, then handle address
						if ( index.Addresses?.Count == 0 )
						{
							FormatAddressesToElastic( index, addresses );
						}
						if ( index.Addresses?.Count > 0 && populatingCredentialJsonProperties )
						{
							//update credential.JsonProperties. This will be useful where the import populated the latter before normalizing the address.
							AddCredentialJsonProperties( index );
						}
					}
					if ( index.Addresses?.Count == 0 )
					{
						//future 
						jsonProperties = GetRowPossibleColumn( dr, "OrganizationJsonProperties" );
						if ( !string.IsNullOrWhiteSpace( jsonProperties ) )
						{
							//technically mapping to CredentialExternalProperties would be fine to extract addresses
							//20-10-23 - not active and deferred, so skipping
							//HandleAddressesFromJson( index, jsonProperties );

						}
						else
						{
							//remember could timing of cutover for use of this
						}
						//prototype: if no cred addresses, and one org address, then add to index (not detail page)
					}
					//just check for zero again
					if ( index.Addresses.Count == 0 )
					{
						//found issues if the org address is added, where there are many.
						//perhaps just region?
						if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
						{
							var orgAddresses = dr["OrgAddresses"].ToString();
							try
							{
								if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
								{
									//issue: a org could have a large number of addresses, that would be a problem for LWIA use. Abandon?
									//	 or only use if there is one address.
									FormatAddressesToElastic( index, orgAddresses, true );
								}
								//}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on OrgAddresses id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}
					}
					//prototype: append region to name org key
					//if this works out, can possibly skip the usingRegionHack 
					if ( index.Regions != null && index.Regions.Any())
					{
						index.NameOrganizationKey += " " + index.Regions[ 0 ];
					}
					#endregion

					if ( index.BadgeClaimsCount > 0 )
						index.HasVerificationType_Badge = true;  //Update this with appropriate source data

					#region properties
					try
					{
						var properties = dr["CredentialProperties"].ToString();
						if ( !string.IsNullOrEmpty( properties ) )
						{
							properties = properties.Replace( "&", " " );
							var xDoc = XDocument.Parse( properties );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
								var property = child.Attribute( "Property" ).Value;
								var schemaName = ( string )child.Attribute( "PropertySchemaName" );

								index.CredentialProperties.Add( new IndexProperty {
									CategoryId = categoryId,
									Id = propertyValueId,
									Name = property,
									SchemaName = schemaName
								} );
								if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
									index.LearningDeliveryMethodTypeIds.Add( propertyValueId );
								if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE )
									index.AsmntDeliveryMethodTypeIds.Add( propertyValueId );
								if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
									index.AudienceTypeIds.Add( propertyValueId );
								if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
									index.AudienceLevelTypeIds.Add( propertyValueId );

								//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
								//	AddTextValue( index, ( string )child.Attribute( "PropertySchemaName" ) );
								//	index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
							}
						}
					}
					catch ( Exception ex )
					{
						LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on CrendentialProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
					}
					#endregion

					#region TextValues, CodedNotation
					try
					{
						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
								{
									//?? what is this?
									//AddTextValue( index, textValue.Value );
									//if ( textValue.Value.IndexOf( "-" ) > -1 )
									//	AddTextValue( index, textValue.Value.Replace( "-", string.Empty ) );

									if ( categoryId == 35 ) //
										index.Keyword.Add( textValue.Value );
									//else
									//	index.TextValues.Add( textValue.Value );
								}
								//source is just direct/indirect, more want the sourceEntityType
								var codeNotation = child.Attribute( "CodedNotation" );
								if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
								{
									index.CodedNotation.Add( codeNotation.Value );
									//AddTextValue( index, codeNotation.Value );
									if ( codeNotation.Value.IndexOf( "-" ) > -1 ) {
										//AddTextValue( index, codeNotation.Value.Replace( "-", string.Empty ) );
										index.CodedNotation.Add( codeNotation.Value.Replace( "-", string.Empty ) );
									}
								}
							}
						}


						//if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
						//	AddTextValue( index, index.AvailableOnlineAt );

						//AddTextValue( index, index.CredentialType );
						//properties to add to textvalues

						string url = dr[ "AvailabilityListing" ].ToString();
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	AddTextValue( index, url );

						//if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
						//	index.TextValues.Add( index.CredentialRegistryId );
						string indexField = dr[ "CredentialId" ].ToString();
						if ( !string.IsNullOrWhiteSpace( indexField ) )
						{
							//AddTextValue( index, indexField );
							index.CodedNotation.Add( indexField );
						}
                        //index.TextValues.Add( indexField );
                        //drop this. Can search by just id
                        //AddTextValue( index, "credential " + index.Id.ToString() );
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );

                    }
					catch ( Exception ex )
					{
						LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on TextValues, CodedNotation id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
					}
					#endregion


					#region Languages
					index.InLanguage = GetLanguages( dr );
					#endregion

					#region Competencies
					//these are competencies from condition profiles
					//See [ConditionProfile_Competencies_Summary]
					//21-05-02 mp - the latter includes asmts and lopp related, not required???
					string requiresCompetencies = dr[ "Competencies" ].ToString();
					if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
					{
						if ( ContainsUnicodeCharacter( requiresCompetencies ) )
						{
							requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
						}
						requiresCompetencies = requiresCompetencies.Replace( "&", " " );
						var xDoc = new XDocument();
						xDoc = XDocument.Parse( requiresCompetencies );
						foreach ( var child in xDoc.Root.Elements() )
						{
							var competency = new IndexCompetency
							{
								Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
								Description = ( string )child.Attribute( "Description" ) ?? string.Empty
							};

							index.Competencies.Add( competency );
						}
					}
					#endregion

					#region Reference Frameworks - industries, occupations and classifications
					string frameworks = dr[ "Frameworks" ].ToString();
					if ( !string.IsNullOrWhiteSpace( frameworks ) )
					{
						HandleFrameworks( index, frameworks );
						//var xDoc = new XDocument();
						//xDoc = XDocument.Parse( frameworks );
						//foreach ( var child in xDoc.Root.Elements() )
						//{
						//	var framework = new IndexReferenceFramework
						//	{
						//		CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
						//		ReferenceFrameworkItemId = int.Parse( child.Attribute( "ReferenceFrameworkItemId" ).Value ),
						//		Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
						//		CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? string.Empty,
						//		SchemaName = ( string )child.Attribute( "SchemaName" ) ?? string.Empty,
						//		CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? string.Empty,
						//	};



						//	if ( framework.CategoryId == 11 )
						//	{
						//		index.Occupations.Add( framework );
						//		if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//			AddTextValue( index, "occupation " + framework.Name );
						//	}
						//	else if ( framework.CategoryId == 10 )
						//	{
						//		index.Industries.Add( framework );
						//		if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//			AddTextValue( index, "industry " + framework.Name );
						//	}
						//	else if ( framework.CategoryId == 23 )
						//	{
						//		index.InstructionalPrograms.Add( framework );
						//		if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//			AddTextValue( index, "program " + framework.Name );
						//	}

						//}//
						if ( index.Occupations.Count > 0 )
							index.HasOccupations = true;
						if ( index.Industries.Count > 0 )
							index.HasIndustries = true;
						if ( index.InstructionalPrograms.Count > 0 )
							index.HasInstructionalPrograms = true;

					}

					#endregion
					#region Widgets, collections
					//24-03-14 mp - check if this is right. The proc references [Entity.TransferValueProfile], which has no data!
					HandleMemberOfTransferValue( index, dr );
                    HandleCollectionSelections( index, dr );
					HandleWidgetSelections( index, dr, "CredentialFilters" );
					//string resourceForWidget = GetRowPossibleColumn( dr, "ResourceForWidget" );
					//if ( !string.IsNullOrWhiteSpace( resourceForWidget ) )
					//{
					//	var xDoc = new XDocument();
					//	xDoc = XDocument.Parse( resourceForWidget );
					//	foreach ( var child in xDoc.Root.Elements() )
					//	{
					//		var widgetId = int.Parse( child.Attribute( "WidgetId" ).Value );
					//		//future 
					//		var widgetSection = ( string )child.Attribute( "WidgetSection" ) ?? string.Empty;
					//		if ( widgetId > 0 && widgetSection == "CredentialFilters" )
					//			index.ResourceForWidget.Add( widgetId );
					//	}//
					//}

					#endregion
					#region Custom Reports
					int propertyId = 0;
					//indicator of in registry 
					if ( !string.IsNullOrWhiteSpace( index.CTID ) )
					{
						if ( GetPropertyId( 58, "credReport:IsInRegistry", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
					}
					else if ( GetPropertyId( 58, "credReport:IsNotInRegistry", ref propertyId ) )
						index.ReportFilters.Add( propertyId );
					//
					if ( index.IsNonCredit )
					{
						if ( GetPropertyId( 58, "credReport:IsNonCredit", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
					}
					//
					index.HasTransferValueProfilesCount = GetRowColumn( dr, "HasTransferValueProfileCount", 0 );
					AddReportProperty( index, index.HasTransferValueProfilesCount, 58, "Has Transfer Values", "credReport:HasTransferValues" );

					AddReportProperty( index, index.AvailableOnlineAt, 58, "Available Online", "credReport:AvailableOnline" );

					AddReportProperty( index, index.EmbeddedCredentialsCount, 58, "Has Embedded Credentials", "credReport:HasEmbeddedCredentials" );
					//
					AddReportProperty( index, index.CostProfileCount, 58, "Has cost profile", "credReport:HasCostProfile" );
					AddReportProperty( index, index.CommonConditionsCount, 58, "References Common Conditions", "credReport:ReferencesCommonConditions" );
					AddReportProperty( index, index.CommonCostsCount, 58, "References Common Costs", "credReport:ReferencesCommonCosts" );
					AddReportProperty( index, index.FinancialAidCount, 58, "Has Financial Assistance", "credReport:FinancialAid" );
					AddReportProperty( index, index.RequiredAssessmentsCount, 58, "Requires Assessment", "credReport:RequiresAssessment" );
					AddReportProperty( index, index.RequiredCredentialsCount, 58, "Requires Credential", "credReport:RequiresCredential" );
					AddReportProperty( index, index.RequiredLoppCount, 58, "Requires Learning Opportunity", "credReport:RequiresLearningOpportunity" );
					AddReportProperty( index, index.RecommendedAssessmentsCount, 58, "Recommends Assessment", "credReport:RecommendsAssessment" );
					AddReportProperty( index, index.RecommendedCredentialsCount, 58, "Recommends Credential", "credReport:RecommendsCredential" );
					AddReportProperty( index, index.RecommendedLoppCount, 58, "Recommends Learning Opportunity", "credReport:RecommendsLearningOpportunity" );
					AddReportProperty( index, index.BadgeClaimsCount, 58, "Has Verification Badges", "credReport:HasVerificationBadges" );
					AddReportProperty( index, index.RevocationProfilesCount, 58, "Has Revocation", "credReport:HasRevocation" );
					AddReportProperty( index, index.RenewalCount, 58, "Has Renewal", "credReport:HasRenewal" );
					AddReportProperty( index, index.ProcessProfilesCount, 58, "Has Process Profile", "credReport:HasProcessProfile" );
					//
					AddReportProperty( index, index.AggregateDataProfileCount, 58, "credReport:HasAggregateDataProfile" );
					AddReportProperty( index, index.DataSetProfileCount, 58, "credReport:HasDataSetProfile" );
					AddReportProperty( index, index.AggregateDataProfileCount + index.DataSetProfileCount, 58, "credReport:HasOutcomeData" );
					//AddReportProperty( index, index.HoldersProfileCount, 58, "Has Holders Profile", "credReport:HasHoldersProfile" );
					//AddReportProperty( index, index.EarningsProfileCount, 58, "Has Earnings Profile", "credReport:HasEarningsProfile" );
					//AddReportProperty( index, index.EmploymentOutcomeProfileCount, 58, "Has Employment Outcome Profile", "credReport:HasEmploymentOutcomeProfile" );
					AddReportProperty( index, index.JurisdictionProfilesCount, 58, "credReport:HasJurisdictionProfile" );

					//
					AddReportProperty( index, index.HasSubjects, 58, "credReport:HasSubjects" );
					AddReportProperty( index, index.HasOccupations, 58, "credReport:HasOccupations" );
					AddReportProperty( index, index.HasIndustries, 58, "credReport:HasIndustries" );
					AddReportProperty( index, index.HasInstructionalPrograms, 58, "credReport:HasCIP" );
					AddReportProperty( index, index.IsPartOfCount, 58, "credReport:IsPartOfCredential" );
					AddReportProperty( index, index.ResourceForCollection.Count, 58, "credReport:IsPartOfCollection" );

					AddReportProperty( index, index.Addresses.Count, 58, "Has Addresses", "credReport:HasAddresses" );

					AddReportProperty( index, index.RequiresCompetenciesCount, 58, "Requires Competencies", "credReport:RequiresCompetencies" );
					AddReportProperty( index, index.RequiresCompetenciesCount + index.AssessmentsCompetenciesCount + index.LearningOppsCompetenciesCount, 58, "credReport:HasCompetencies" );
					//
					var HasConditionProfileCount = GetRowPossibleColumn( dr, "HasConditionProfileCount", 0 );
					AddReportProperty( index, HasConditionProfileCount, 58, "Has Condition Profile", "credReport:HasConditionProfile" );
					//						
					var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
					AddReportProperty( index, DurationProfileCount, 58, "Has Duration Profile", "credReport:HasDurationProfile" );
					//
					if ( index.ProvidesTransferValueFor.Count > 0 )
						if ( GetPropertyId( 58, "credReport:ProvidesTransferValueFor", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
					if ( index.ReceivesTransferValueFrom.Count > 0 )
						if ( GetPropertyId( 58, "credReport:ReceivesTransferValueFrom", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
					#endregion

					list.Add( index );
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic. Last row: {0}, CredentialId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
			}
			finally
			{
				DateTime completed = DateTime.Now;
				var duration = completed.Subtract( started ).TotalSeconds;

				LoggingHelper.DoTrace( 2, string.Format( "Credential_SearchForElastic - Completed. loaded {0} records, in {1} seconds", cntr, duration ) );
			}

			return list;
		
		}
		/// <summary>
		/// populate Credential JsonProperties - starting with addresses
		/// </summary>
		/// <param name="index"></param>
		/// <param name="input"></param>
		/// <param name="addingToPremium"></param>
		public static void AddCredentialJsonProperties( CredentialIndex index )
		{
			if ( index.Addresses == null || !index.Addresses.Any() )
				return;
			//
			var entity = new MC.CredentialExternalProperties();
			foreach ( var item in index.Addresses )
			{
				MC.Address adr = new MC.Address()
				{
					StreetAddress = item.StreetAddress,
					//Address2 = item.Address2,
					AddressLocality = item.AddressLocality,
					AddressRegion = item.AddressRegion,
					PostalCode = item.PostalCode,
					AddressCountry = item.AddressCountry,
					Latitude = item.Latitude,
					Longitude = item.Longitude,
					GeoCoordinates = null
				};
				entity.Addresses.Add( adr );
			}
			var json = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
			//will custom method that just gets and updates additional property
			new CredentialManager().UpdateJson( index.Id, json );
		}

		public static List<MC.CredentialSummary> Credential_MapFromElastic( List<CredentialIndex> credentials, int pageNbr, int pageSize )
		{
			var list = new List<MC.CredentialSummary>();
			LoggingHelper.DoTrace( 6, "ElasticManager.Credential_MapFromElastic - entered" );
			var currencies = CodesManager.GetCurrencies();
			var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );
			bool includingHasPartIsPartWithConnections = UtilityManager.GetAppKeyValue( "includeHasPartIsPartWithConnections", false );
			int rowNbr = (pageNbr - 1) * pageSize;
			foreach ( var item in credentials )
			{
				var index = new MC.CredentialSummary();
				rowNbr++;
				//avgMinutes = 0;
				index = new MC.CredentialSummary
				{
					Id = item.Id,
					ResultNumber = rowNbr,
					Name = item.Name,
					FriendlyName = item.FriendlyName,
					SubjectWebpage = item.SubjectWebpage,
					EntityStateId = item.EntityStateId,
					RowId = item.RowId,
					Description = item.Description,
					OwnerOrganizationId = item.PrimaryOrganizationId,
					OwnerOrganizationName = item.PrimaryOrganizationName,
					CTID = item.CTID,
					PrimaryOrganizationCTID = item.PrimaryOrganizationCTID,
					//CredentialRegistryId = ci.CredentialRegistryId,
					DateEffective = item.DateEffective,
					Created = item.Created,
					//define LastUpdated to be EntityLastUpdated
					LastUpdated = item.LastUpdated,
					CredentialType = item.CredentialType,
					CredentialTypeSchema = item.CredentialTypeSchema,
					CredentialStatus = item.CredentialStatus,
					TotalCost = item.TotalCost,
					CostProfileCount = item.CostProfileCount,
					IsAQACredential = item.IsAQACredential,
					IsNonCredit=item.IsNonCredit,
					LearningOppsCompetenciesCount = item.LearningOppsCompetenciesCount,
					AssessmentsCompetenciesCount = item.AssessmentsCompetenciesCount,
					RequiresCompetenciesCount = item.RequiresCompetenciesCount,
					//QARolesCount = ci.QARolesCount,
					HasPartCount = item.HasPartCount,
					IsPartOfCount = item.IsPartOfCount,
					AggregateDataProfileCount = item.AggregateDataProfileCount,
					DataSetProfileCount = item.DataSetProfileCount,
					HoldersProfileCount = item.HoldersProfileCount,
					EarningsProfileCount = item.EarningsProfileCount,
					EmploymentOutcomeProfileCount = item.EmploymentOutcomeProfileCount,
					
					RequiresCount = item.RequiresCount,
					RecommendsCount = item.RecommendsCount,
					RequiredForCount = item.RequiredForCount,
					//20-04-22 mparsons - supposedly there used to be gray boxes for asmts, and lopps. Not sure where set? Seems has to be done in search? Or ajax callbacks?
					RequiredAssessmentsCount = item.RequiredAssessmentsCount,
					RecommendedAssessmentsCount = item.RecommendedAssessmentsCount,
					RequiredLoppCount = item.RequiredLoppCount,
					RecommendedLoppCount = item.RecommendedLoppCount,

					IsRecommendedForCount = item.IsRecommendedForCount,
					RenewalCount = item.RenewalCount,
					IsAdvancedStandingForCount = item.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = item.AdvancedStandingFromCount,
					PreparationForCount = item.PreparationForCount,
					PreparationFromCount = item.PreparationFromCount,
					NumberOfCostProfileItems = item.NumberOfCostProfileItems,
					HasVerificationType_Badge = item.HasVerificationType_Badge,
					//TotalCostCount = ci.TotalCostCount,
					CommonCostsCount = item.CommonCostsCount,
					CommonConditionsCount = item.CommonConditionsCount,
					FinancialAidCount = item.FinancialAidCount,
					TransferValueCount = item.HasTransferValueProfilesCount

				};
				index.PrimaryOrganizationFriendlyName = FormatFriendlyTitle( index.OwnerOrganizationName );

				if ( item.ImageURL != null && item.ImageURL.Trim().Length > 0 )
					index.ImageUrl = item.ImageURL;
				else
					index.ImageUrl = null;

				if ( index.EntityStateId == 2 )
				{
					index.Name += " [reference]";
				}
				if ( item.ResourceDetail != null )
				{
					index.ResourceDetail = item.ResourceDetail;
				} else
				{
					//TODO - fake out something, or skip
				}
				
                if ( item.ResourceForCollection.Count > 0 )
				{
					index.InCollectionCount = item.ResourceForCollection.Count;
				}
				//AverageMinutes is a rough approach to sorting. If present, get the duration profiles
				//if ( ci.EstimatedTimeToEarn > 0 )
				index.EstimatedTimeToEarn = DurationProfileManager.GetAll( index.RowId );

				if ( item.Industries != null && item.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( item.Occupations != null && item.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( item.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( item.InstructionalPrograms != null && item.InstructionalPrograms.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( item.InstructionalPrograms.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

				index.Org_QARolesResults = Fill_CodeItemResults( item.Org_QARolesList, 130, false, true );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( item.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
				index.AgentAndRoles = Fill_AgentRelationship( item.AgentAndRoles, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false, true, "Credential" );
				//why was this missing - usually part of AgentAndRoles, but not for Sophia??
				var qa=Fill_AgentRelationship( item.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "Credential" );
				if (qa != null && qa.Results.Count() > 0)
				{
					foreach ( var qaitem in qa.Results )
					{
						var exists = index.AgentAndRoles.Results.Where( s => s.RelationshipId == qaitem.RelationshipId && s.AgentId == qaitem.AgentId ).ToList();
						if ( exists == null || !exists.Any() )
							index.AgentAndRoles.Results.Add( qaitem );
						//if ( index.AgentAndRoles.Results.Where ( s => s.))
					}
					//index.Org_QAAgentAndRoles.Results.AddRange( qa.Results );
				}
				//t
				if ( item.ProvidesTransferValueFor.Count > 0 )
					index.ProvidesTransferValueFor = Fill_CodeItemResults( item.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				if ( item.ReceivesTransferValueFrom.Count > 0 )
					index.ReceivesTransferValueFrom = Fill_CodeItemResults( item.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				index.ConnectionsList = Fill_CodeItemResults( item.ConnectionsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE, true, true );
				if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
				{
					//21-04-19 - decided to use a simple generic summary for all outcome data
					index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
					//index.AggregateDataProfileSummary = ci.AggregateDataProfileSummary;
					//hack 
					if( index.AggregateDataProfileCount ==0)
						index.AggregateDataProfileCount += index.DataSetProfileCount;
				}
				if (index.HoldersProfileCount > 0)
					index.HoldersProfileSummary = item.HoldersProfileSummary;
				if ( index.EarningsProfileCount > 0 )
					index.EarningsProfileSummary = item.EarningsProfileSummary;
				if ( index.EmploymentOutcomeProfileCount > 0 )
					index.EmploymentOutcomeProfileSummary = item.EmploymentOutcomeProfileSummary;
				//
				if ( includingHasPartIsPartWithConnections )
				{
					//manually add other connections
					if ( item.HasPartCount > 0 )
					{
						index.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Includes", SchemaName = "hasPart", Totals = item.HasPartCount } );
					}
					if ( item.IsPartOfCount > 0 )
					{
						index.ConnectionsList.Results.Add( new CodeItem() { Id = 0, Title = "Included With", SchemaName = "isPartOf", Totals = item.IsPartOfCount } );
					}
				}

				index.HasPartsList = Fill_CredentialConnectionsResult( item.HasPartsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

				index.IsPartOfList = Fill_CredentialConnectionsResult( item.IsPartOfList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );

				index.CredentialsList = Fill_CredentialConnectionsResult( item.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.ListTitle = item.ListTitle;
				index.Subjects = item.Subjects.Select( x => x.Name ).Distinct().ToList();

				index.AssessmentDeliveryType = Fill_CodeItemResults( item.CredentialProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_ASMT_DELIVERY_TYPE, false, false );
				index.LearningDeliveryType = Fill_CodeItemResults( item.CredentialProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );

				//16-09-12 mp - changed to use pipe (|) rather than ; due to conflicts with actual embedded semicolons
				index.AudienceLevelsResults = Fill_CodeItemResults( item.CredentialProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );
				index.AudienceTypesResults = Fill_CodeItemResults( item.CredentialProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );

				//addressess                

				index.Addresses = item.Addresses.Select( x => new MC.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					StreetAddress = x.StreetAddress,
					//Address2 = x.Address2,
					AddressLocality = x.AddressLocality,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Identifier  = x.Identifier,
					AddressCountry = x.AddressCountry
				} ).ToList();

				list.Add( index );
			}

			LoggingHelper.DoTrace( 6, "ElasticManager.Credential_MapFromElastic - exit" );
			return list;
		}


		#endregion

		#region Organization Elastic Index
		//public static List<OrganizationIndex> Organization_SearchForElastic( string filter )
		//{
		public static List<OrganizationIndex> Organization_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new OrganizationIndex();
			var list = new List<OrganizationIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Organization_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			int thisReportCategoryId = 59;
			//special
			bool populatingOrganizationJsonProperties = UtilityManager.GetAppKeyValue( "populatingOrganizationJsonProperties", false );

			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Organization.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						index = new OrganizationIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic - Page: {0} - loading {1} rows ", pageNumber, result.Rows.Count ) );
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Page: {0} - loading record: {1}", pageNumber, cntr ) );
						if ( cntr == 33 )
						{

						}

						index = new OrganizationIndex();
						
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						index.EntityTypeId = GetRowColumn( dr, "EntityTypeId", 2 );
						index.EntityType = OrganizationManager.MapEntityType( index.EntityTypeId );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.FriendlyName = FormatFriendlyTitle( index.Name );
						if ( string.IsNullOrWhiteSpace( index.CTID ) )
							index.Name += " [reference]";
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						
						//add helpers
						index.PrimaryOrganizationCTID = index.CTID;
						index.PrimaryOrganizationId = index.Id;
						index.PrimaryOrganizationName = index.Name;
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						index.ImageURL = GetRowColumn( dr, "ImageUrl", string.Empty );
						if ( GetRowColumn( dr, "CredentialCount", 0 ) > 0 )
							index.IsACredentialingOrg = true;
						index.ISQAOrganization = GetRowColumn( dr, "IsAQAOrganization", false );
                        //index.MainPhoneNumber = MC.PhoneNumber.DisplayPhone( GetRowColumn( dr, "MainPhoneNumber", string.Empty ) );
                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );							
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.OrganizationDetail>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }                       
                        }
                        catch( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Organization_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        index.OwnedByResults = dr[ "OwnedByList" ].ToString();

						index.OfferedByResults = dr[ "OfferedByList" ].ToString();

						index.AsmtsOwnedByResults = dr[ "AsmtsOwnedByList" ].ToString();
						index.LoppsOwnedByResults = dr[ "LoppsOwnedByList" ].ToString();
						index.FrameworksOwnedByResults = dr[ "FrameworksOwnedByList" ].ToString();


						index.ApprovedByResults = dr[ "ApprovedByList" ].ToString();
						index.AccreditedByResults = dr[ "AccreditedByList" ].ToString();
						index.RecognizedByResults = dr[ "RecognizedByList" ].ToString();
						index.RegulatedByResults = dr[ "RegulatedByList" ].ToString();
						index.VerificationProfilesCount = GetRowPossibleColumn( dr, "VerificationProfilesCount", 0 );
						index.CostManifestsCount = GetRowPossibleColumn( dr, "CostManifestsCount", 0 );
						index.ConditionManifestsCount = GetRowPossibleColumn( dr, "ConditionManifestsCount", 0 );
						index.SubsidiariesCount = GetRowPossibleColumn( dr, "SubsidiariesCount", 0 );
						index.DepartmentsCount = GetRowPossibleColumn( dr, "DepartmentsCount", 0 );
						index.HasIndustriesCount = GetRowPossibleColumn( dr, "HasIndustriesCount", 0 );
						//
						index.PathwaysCount = GetRowPossibleColumn( dr, "PathwayCount", 0 );
						index.PathwaySetsCount = GetRowPossibleColumn( dr, "PathwaySetCount", 0 );
						index.TransferValueProfilesCount = GetRowPossibleColumn( dr, "TransferValueCount", 0 );
						index.DataSetProfileCount = GetRowPossibleColumn( dr, "DataSetProfileCount", 0 );
						index.JurisdictionProfilesCount = GetRowPossibleColumn( dr, "JurisdictionProfilesCount", 0 );

						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							FormatAddressesToElastic( index, addresses );

							if ( index.Addresses != null && index.Addresses.Any() && populatingOrganizationJsonProperties )
							{
								AddOrganizationJsonProperties( index );
							}

						}
						#endregion


						#region TextValues - alternate name + ?

						//string textValues = GetRowPossibleColumn( dr, "TextValues" );
						//if ( !string.IsNullOrWhiteSpace( textValues ) )
						//{
						//    foreach ( var text in textValues.Split( '|' ) )
						//    {
						//        if ( !string.IsNullOrWhiteSpace( text ) )
						//            index.TextValues.Add( text );
						//    }
						//}
						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );

							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null )
								{
									//if ( textValue.Value.IndexOf( "-" ) > -1 )
									//	index.TextValues.Add( textValue.Value.Replace( "-", string.Empty ) );

									if ( categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
										index.Keyword.Add( textValue.Value );
									//else
									//	index.TextValues.Add( textValue.Value );
								}
							}
						}

						string alternatesNames = GetRowPossibleColumn( dr, "AlternatesNames" );
						if ( !string.IsNullOrWhiteSpace( alternatesNames ) )
						{
							var xDoc = XDocument.Parse( alternatesNames );
							foreach ( var child in xDoc.Root.Elements() )
							{
								string textValue = ( string )( child.Attribute( "TextValue" ) ) ?? string.Empty;
								if ( !string.IsNullOrWhiteSpace( textValue ) )
								{
									index.AlternateNames.Add( textValue );
									//index.TextValues.Add( textValue );
								}
							}
						}

						string identifiers = GetRowPossibleColumn( dr, "AlternateIdentifiers" );
						if ( !string.IsNullOrWhiteSpace( identifiers ) )
						{
							var xDoc = XDocument.Parse( identifiers );
							foreach ( var child in xDoc.Root.Elements() )
							{
								string title = ( string )( child.Attribute( "Title" ) ) ?? string.Empty;
								string textValue = ( string )( child.Attribute( "TextValue" ) ) ?? string.Empty;
								if ( !string.IsNullOrWhiteSpace( title ) && !string.IsNullOrWhiteSpace( textValue ) )
								{
									index.CodedNotation.Add( title + " " + textValue.Replace( "-", string.Empty ) );
									//index.TextValues.Add( title + " " + textValue.Replace( "-", string.Empty ) );
								}
							}
						}

						////properties to add to textvalues
						//string url = GetRowPossibleColumn( dr, "AvailabilityListing", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	index.TextValues.Add( url );
						//if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
						//	index.TextValues.Add( index.CredentialRegistryId );
						//index.TextValues.Add( index.Id.ToString() );
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );

						#endregion

						#region PropertyValues  - ServiceTypes, OrgTypes, SectorTypes 
						//THESE ARE THE SAME AS FOR PropertyValues, DO WE NEED THEM
						string propertyValues = GetRowPossibleColumn( dr, "PropertyValues" );
						if ( !string.IsNullOrWhiteSpace( propertyValues ) )
						{
							var xDoc = XDocument.Parse( propertyValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var prop = new IndexProperty
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
									Name = ( string )child.Attribute( "Property" ),
									SchemaName = ( string )child.Attribute( "PropertySchemaName" )
								};
								if ( prop.CategoryId == 6 )
								{
									index.OrganizationServiceTypes.Add( prop );
									index.OrganizationServiceTypeIds.Add( prop.Id );
									//AddTextValue( index, prop.SchemaName );
								} else if ( prop.CategoryId == 7 )
								{
									index.OrganizationTypes.Add( prop );
									index.OrganizationTypeIds.Add( prop.Id );
									//AddTextValue( index, prop.SchemaName );
								} else if ( prop.CategoryId == 30 )
								{
									index.OrganizationSectorTypes.Add( prop );
									index.OrganizationSectorTypeIds.Add( prop.Id );
								}
								//claim types are handled separately now
								//else if ( prop.CategoryId == 41 )
								//{
								//	index.OrganizationClaimTypes.Add( prop );
								//	index.OrganizationClaimTypeIds.Add( prop.Id );
								//}
								//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
								//	index.TextValues.Add( ( string )child.Attribute( "Property" ) );
								//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
								//	index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
							}
						}

						string claimTypes = GetRowPossibleColumn( dr, "ClaimTypes" );
						if ( !string.IsNullOrWhiteSpace( claimTypes ) )
						{
							var xDoc = XDocument.Parse( claimTypes );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var prop = new IndexProperty
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									Id = int.Parse( child.Attribute( "PropertyValueId" ).Value ),
									Name = ( string )child.Attribute( "Property" ),
									SchemaName = ( string )child.Attribute( "PropertySchemaName" )
								};

								index.OrganizationClaimTypes.Add( prop );
								index.OrganizationClaimTypeIds.Add( prop.Id );

								//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "Property" ) ) )
								//	index.TextValues.Add( ( string )child.Attribute( "Property" ) );
							}
						}
						#endregion

						#region ReferenceFrameworks
						/*not used - need to use:
                        * Reference.Frameworks
                        * Entity.ReferenceFramework
                        * Entity_ReferenceFramework_Summary
                       */

						string Industries = GetRowPossibleColumn( dr, "Industries" );
						if ( !string.IsNullOrWhiteSpace( Industries ) )
						{
							var xDoc = XDocument.Parse( GetRowPossibleColumn( dr, "Industries" ) );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var framework = new IndexReferenceFramework
								{
									CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
									ReferenceFrameworkItemId = int.Parse( child.Attribute( "ReferenceFrameworkItemId" ).Value ),
									Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
									CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? string.Empty,
									SchemaName = ( string )child.Attribute( "SchemaName" ) ?? string.Empty,
									CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? string.Empty
								};

								if ( framework.CategoryId == 10 )
								{
									index.Industries.Add( framework );
									index.Industry.Add( framework.Name );
								}
							}
						}
						#endregion

						#region AgentRelationships
						//string agentRelations = GetRowPossibleColumn( dr, "AgentRelationships" );
						//if ( !string.IsNullOrWhiteSpace( agentRelations ) )
						//{
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( agentRelations );
						//	foreach ( var child in xDoc.Root.Elements() )
						//		index.AgentRelationships.Add( int.Parse( ( string ) child.Attribute( "RelationshipTypeId" ) ) );
						//}
						#endregion

						#region AgentRelationshipsForEntity
						//TODO replacing: QualityAssurance with AgentRelationshipsForEntity
						HandleAgentRelationshipsForEntity( dr, index );
						//handle QA asserted by a third party (versus by the owner)
						HandleDirectAgentRelationshipsForEntity( dr, index );
						//}
						#endregion

						#region QualityAssurancePerformed
						string qAPerformed = GetRowPossibleColumn( dr, "QualityAssurancePerformedCSV" );
						if ( !string.IsNullOrWhiteSpace( qAPerformed ) )
						{
							if ( ContainsUnicodeCharacter( qAPerformed ) )
							{
								qAPerformed = Regex.Replace( qAPerformed, @"[^\u0000-\u007F]+", string.Empty );
							}
							qAPerformed = qAPerformed.Replace( "&", " " );
							var xDoc = XDocument.Parse( qAPerformed );

							foreach ( var child in xDoc.Root.Elements() )
							{
								string targetName = ( string )child.Attribute( "TargetEntityName" ) ?? string.Empty;
								string entityStatId = ( string )child.Attribute( "TargetEntityStateId" ) ?? string.Empty;
								var entityStateId = 0;
								int.TryParse( entityStatId, out entityStateId );
								if ( entityStateId > 1 )
								{
									var assertions = new List<int>();
									foreach ( var s in child.Attribute( "Assertions" ).Value.Split( new char[] { ',' } ) )
									{
										assertions.Add( int.Parse( s.Trim() ) );
									}
									index.QualityAssurancePerformed.Add( new QualityAssurancePerformed
									{
										TargetEntityBaseId = int.Parse( child.Attribute( "TargetEntityBaseId" ).Value ),
										TargetEntityTypeId = int.Parse( child.Attribute( "TargetEntityTypeId" ).Value ),
										AssertionTypeIds = assertions,
										TargetEntityName = ( string )child.Attribute( "TargetEntityName" ) ?? string.Empty,
										TargetEntityStateId = entityStateId,
									} );
								}
								if ( index.QualityAssurancePerformed.Count() > 0 )
								{
									index.HasQualityAssurancePerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 1 ).Count() > 0 )
										index.HasCredentialsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 2 ).Count() > 0 )
										index.HasOrganizationsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 3 ).Count() > 0 )
										index.HasAssessmentsQAPerformed = true;
									if ( index.QualityAssurancePerformed.Where( s => s.TargetEntityTypeId == 7 ).Count() > 0 )
										index.HasLoppsQAPerformed = true;
								}
							}
							//done above
							//index.HasQualityAssurancePerformed = index.QualityAssurancePerformed.Count() > 0 ? true : false;
						}
						#endregion

						#region Widgets, collections
						HandleCollectionSelections( index, dr );
						HandleWidgetSelections( index, dr, "OrganizationFilters" );

						#endregion
						#region Custom Reports
						int propertyId = 0;
						AddReportProperty( index, index.ResourceForCollection.Count, 59, "orgReport:IsPartOfCollection" );

						index.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
						AddReportProperty( index, index.ProcessProfilesCount, thisReportCategoryId, "Has Process Profile", "orgReport:HasProcessProfile" );
						AddReportProperty( index, index.JurisdictionProfilesCount, thisReportCategoryId, "orgReport:HasJurisdictionProfile" );

						//TODO - could replace this approach and just add the schema text the text index
						if ( index.VerificationProfilesCount > 0 )
						{
							//index.TextValues.Add( "HasVerificationService" );
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasVerificationService", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else if ( index.EntityStateId == 3 )
						{
							//index.TextValues.Add( "HasNoVerificationService" );
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasNoVerificationService", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						
						if ( index.CostManifestsCount > 0 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasCostManifest", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else if ( index.EntityStateId == 3 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasNoCostManifests", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.ConditionManifestsCount > 0 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasConditionManifest", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else if ( index.EntityStateId == 3 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasNoConditionManifests", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						AddReportProperty( index, index.PathwaysCount, thisReportCategoryId, "Has Pathways", "orgReport:HasPathways" );
						AddReportProperty( index, index.PathwaySetsCount, thisReportCategoryId, "Has Pathway Sets", "orgReport:HasPathwaySets" );
						AddReportProperty( index, index.TransferValueProfilesCount, thisReportCategoryId, "Has Transfer Value Profiles", "orgReport:HasTransferValueProfiles" );
						AddReportProperty( index, index.DataSetProfileCount, thisReportCategoryId, "Has Outcome Profiles", "orgReport:HasOutcomeProfiles" );
						//
						if ( string.IsNullOrWhiteSpace( index.CTID ) )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:IsReferenceOrg", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:IsRegisteredOrg", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.SubsidiariesCount > 0 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasSubsidiary", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.DepartmentsCount > 0 )
						{
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasDepartment", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						if ( index.HasIndustriesCount > 0 )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//
						if ( !string.IsNullOrWhiteSpace( index.OwnedByResults ) || !string.IsNullOrWhiteSpace( index.OfferedByResults ) )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasCredentials", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						if ( !string.IsNullOrWhiteSpace( index.AsmtsOwnedByResults ) )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasAssessments", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//
						if ( !string.IsNullOrWhiteSpace( index.LoppsOwnedByResults ) )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasLearningOpportunities", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//
						if ( !string.IsNullOrWhiteSpace( index.FrameworksOwnedByResults ) )
							if ( GetPropertyId( thisReportCategoryId, "orgReport:HasCompetencyFrameworks", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic. Last Row: {0}, OrgId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Organization_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}
		/// <summary>
		/// populate Credential JsonProperties - starting with addresses
		/// </summary>
		/// <param name="index"></param>
		public static void AddOrganizationJsonProperties( OrganizationIndex index )
		{
			if ( index.Addresses == null || !index.Addresses.Any() )
				return;
			//
			var entity = new MC.OrganizationExternalProperties();
			foreach ( var item in index.Addresses )
			{
				MC.Address adr = new MC.Address()
				{
					StreetAddress = item.StreetAddress,
					//Address2 = item.Address2,
					AddressLocality = item.AddressLocality,
					AddressRegion = item.AddressRegion,
					PostalCode = item.PostalCode,
					AddressCountry = item.AddressCountry,
					Latitude = item.Latitude,
					Longitude = item.Longitude,
					GeoCoordinates = null
				};
				entity.Addresses.Add( adr );
			}
			var json = JsonConvert.SerializeObject( entity, JsonHelper.GetJsonSettings() );
			//will custom method that just gets and updates additional property
			new OrganizationManager().UpdateJson( index.Id, json );
		}

		public static List<MC.OrganizationSummary> Organization_MapFromElastic( List<OrganizationIndex> organizations, int pageNbr, int pageSize )
		{
			var list = new List<MC.OrganizationSummary>();

			var currencies = CodesManager.GetCurrencies();
			//have to be changed
			var costTypes = CodesManager.GetEnumeration( CodesManager.PROPERTY_CATEGORY_CREDENTIAL_ATTAINMENT_COST );

			int rowNbr = ( pageNbr - 1 ) * pageSize;
			foreach ( var item in organizations )
			{
				rowNbr++;
				var index = new MC.OrganizationSummary
				{
					Id = item.Id,
					ResultNumber = rowNbr,
					Name = item.Name,
					FriendlyName = item.FriendlyName,
					SubjectWebpage = item.SubjectWebpage,
					RowId = item.RowId,
					Description = item.Description,
					CTID = item.CTID,
					//CredentialRegistryId = oi.CredentialRegistryId,
					EntityStateId = item.EntityStateId,
					Created = item.Created,
					LastUpdated = item.LastUpdated,
					LifeCycleStatus = item.LifeCycleStatusType,

				};
				if ( index.EntityTypeId > 0 )
				{
					index.EntityType = OrganizationManager.MapEntityType( index.EntityTypeId );
					index.EntityTypeLabel = OrganizationManager.MapEntityTypeLabel( index.EntityTypeId );
					index.EntityTypeSchema = "ceterms:" + index.EntityType;
				}
				if ( index.EntityStateId == 2 )
				{
					index.Name += " [reference]";
				}
                if ( item.ResourceDetail != null )
                    index.ResourceDetail = item.ResourceDetail;
                if ( item.ImageURL != null && item.ImageURL.Trim().Length > 0 )
					index.Image = item.ImageURL;
				else
					index.Image = null;

				if ( IsValidDate( item.Created ) )
					index.Created = item.Created;

				if ( IsValidDate( item.LastUpdated ) )
					index.LastUpdated = item.LastUpdated;

				index.IsACredentialingOrg = item.IsACredentialingOrg;

				//addressess                

				index.Addresses = item.Addresses.Select( x => new MC.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					StreetAddress = x.StreetAddress,
					//Address2 = x.Address2,
					AddressLocality = x.AddressLocality,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Identifier = x.Identifier,
					AddressCountry = x.AddressCountry
				} ).ToList();

				//these should be derived from the codes property
				index.AgentType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_TYPE );
				index.AgentSectorType = EntityPropertyManager.FillEnumeration( item.RowId, CodesManager.PROPERTY_CATEGORY_ORGANIZATION_SECTORTYPE );
				//index.OrganizationClaimType = EntityPropertyManager.FillEnumeration( oi.RowId, CodesManager.PROPERTY_CATEGORY_CLAIM_TYPE );
				index.ServiceType = EntityPropertyManager.FillEnumeration( index.RowId, CodesManager.PROPERTY_CATEGORY_ORG_SERVICE );
				//
				index.TotalPathways = item.PathwaysCount;
				index.TotalPathwaySets = item.PathwaySetsCount;
				index.TotalTransferValueProfiles = item.TransferValueProfilesCount;
				index.TotalDataSetProfiles = item.DataSetProfileCount;

				//
				if ( item.Industries != null && item.Industries.Count > 0 )
				{
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
				}
				index.OwnedByResults = Fill_CodeItemResults( item.OwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.OfferedByResults = Fill_CodeItemResults( item.OfferedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.AsmtsOwnedByResults = Fill_CodeItemResults( item.AsmtsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.LoppsOwnedByResults = Fill_CodeItemResults( item.LoppsOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );
				index.FrameworksOwnedByResults = Fill_CodeItemResults( item.FrameworksOwnedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.AccreditedByResults = Fill_CodeItemResults( item.AccreditedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.ApprovedByResults = Fill_CodeItemResults( item.ApprovedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.RecognizedByResults = Fill_CodeItemResults( item.RecognizedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.RegulatedByResults = Fill_CodeItemResults( item.RegulatedByResults, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, false, false );

				index.QualityAssurance = Fill_AgentRelationship( item.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "Organization" );

				index.QualityAssurancePerformed = Fill_TargetQaAssertion( item.QualityAssurancePerformed, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, string.Empty );
				var results = item.QualityAssurancePerformed.GroupBy( a => new
				{
					a.TargetEntityTypeId,
					a.TargetEntityBaseId,
					a.AssertionTypeIds
				} )
					.Select( g => new QualityAssurancePerformed
					{
						TargetEntityTypeId = g.Key.TargetEntityTypeId,
						TargetEntityBaseId = g.Key.TargetEntityBaseId,
						AssertionTypeIds = g.Key.AssertionTypeIds
					} )
					.OrderBy( a => a.TargetEntityTypeId ).ThenBy( s => s.TargetEntityBaseId ).ThenBy( s => s.AssertionTypeIds )
					.ToList();

				//results = results.OrderBy(s => s.TargetEntityTypeId).ThenBy(s => s.TargetEntityBaseId).ThenBy(s => s.AssertionTypeId).Distinct().ToList();

				index.QualityAssuranceCombinedTotal = results.Count();

				list.Add( index );
			}

			return list;
		}
		#endregion

		#region Assessment Elastic Index 
		public static List<AssessmentIndex> Assessment_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new AssessmentIndex();
			var list = new List<AssessmentIndex>();
			var result = new DataTable();
			var startTime = DateTime.Now;
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[Assessment.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new AssessmentIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );
						return list;
					} finally
					{
						//call duration
						TimeSpan duration = DateTime.Now.Subtract( startTime );
						if ( duration.TotalSeconds > 10 )
						{
							LoggingHelper.DoTrace( LoggingHelper.appTraceLevel, thisClassName + string.Format( "Assessment_SearchForElastic. Search duration: {0:N2} seconds ", duration.TotalSeconds ) );
						}
					}
				}
				int costProfilesCount = 0;
				int conditionProfilesCount = 0;
				string assessesCompetencies = string.Empty;
				try
				{
					startTime = DateTime.Now;
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

						index = new AssessmentIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.NameIndex = cntr * 1000;
						if ( index.Id == 415 )
						{
							//415,289,406,280 - had reference error in connections
						}
						index.Name = dr[ "Name" ].ToString();
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = dr[ "Description" ].ToString();
						string rowId = dr[ "RowId" ].ToString();
						index.RowId = new Guid( rowId );

						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						var codedNotation = GetRowColumn( dr, "IdentificationCode", string.Empty );
						if ( !string.IsNullOrWhiteSpace( codedNotation ) )
							index.CodedNotation.Add( codedNotation );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

						index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "Organization", string.Empty );
						index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "OrgId", 0 );

						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//add helpers
						index.PrimaryOrganizationCTID = dr[ "OwningOrganizationCtid" ].ToString();
                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.AssessmentDetail>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );

                                HandleResourceHasSupportService( index, resource.HasSupportService );
								if ( resource.ProvidesTransferValueFor != null )
								{
									index.ProvidesTransferValueFor = HandleResource( resource.ProvidesTransferValueFor );

								}
								if ( resource.ReceivesTransferValueFrom != null )
								{
									index.ReceivesTransferValueFrom = HandleResource( resource.ReceivesTransferValueFrom );

								}
							}
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Assessment_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        var date = GetRowColumn( dr, "DateEffective", string.Empty );
						if ( IsValidDate( date ) )
							index.DateEffective = ( DateTime.Parse( date ).ToString("yyyy-MM-dd") );
						else
							index.DateEffective = string.Empty;
						date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						//don't thinks this is necessary!
						//index.QARolesCount = GetRowColumn( dr, "QARolesCount", 0 );
						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = dr[ "LifeCycleStatusType" ].ToString();

						index.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
						index.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
						index.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
						index.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
						index.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
						index.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
						index.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
						index.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
						//index.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
						index.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );
						costProfilesCount = index.CostProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
						conditionProfilesCount = GetRowPossibleColumn( dr, "conditionProfilesCount", 0 );
						//
						index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
						index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
						index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
						index.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );

						//index.HasInstructionalPrograms = GetRowPossibleColumn( dr, "HasCIPCount", 0 );
						//-actual connection type (no credential info)
						index.ConnectionsList = dr[ "ConnectionsList" ].ToString();
						//connection type, plus Id, and name of credential
						index.CredentialsList = dr[ "CredentialsList" ].ToString();
						//change to use Connections
						try
						{
							//24-02-16 mp - AssessmentConnections is not in the proc, so commented out
							//string assessmentConnections = GetRowPossibleColumn( dr, "AssessmentConnections" );
							//if ( !string.IsNullOrWhiteSpace( assessmentConnections ) )
							//{
							//	Connection conn = new Connection();
							//	var xDoc = XDocument.Parse( assessmentConnections );
							//	foreach ( var child in xDoc.Root.Elements() )
							//	{
							//		conn = new Connection();
							//		conn.ConnectionType = ( string )child.Attribute( "ConnectionType" ) ?? string.Empty;
							//		conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

							//		//do something with counts for this type

							//		conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
							//		if ( conn.CredentialId > 0 )
							//		{
							//			//add credential
							//			conn.Credential = ( string )child.Attribute( "CredentialName" ) ?? string.Empty;
							//			//??????
							//			conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
							//			conn.CredentialOrganization = ( string )child.Attribute( "credOrganization" ) ?? string.Empty;
							//		}
							//		conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
							//		if ( conn.AssessmentId > 0 )
							//		{
							//			conn.Assessment = ( string )child.Attribute( "AssessmentName" ) ?? string.Empty;
							//			conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
							//			conn.AssessmentOrganization = ( string )child.Attribute( "asmtOrganization" ) ?? string.Empty;
							//		}
							//		conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
							//		if ( conn.LoppId > 0 )
							//		{
							//			conn.LearningOpportunity = ( string )child.Attribute( "LearningOpportunityName" ) ?? string.Empty;
							//			conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
							//			conn.LearningOpportunityOrganization = ( string )child.Attribute( "loppOrganization" ) ?? string.Empty;
							//		}

							//		index.Connections.Add( conn );
							//	}
							//}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment Connections id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}

						#region AssessesCompetencies
						//21-08-31 - currently the competency search is not specific for assesses or requires. So use the general competencies for now
						assessesCompetencies = dr[ "AssessesCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( assessesCompetencies ) )
						{
							try
							{
								if ( ContainsUnicodeCharacter( assessesCompetencies ) )
								{
									assessesCompetencies = Regex.Replace( assessesCompetencies, @"[^\u0000-\u007F]+", string.Empty );

								}
								assessesCompetencies = assessesCompetencies.Replace( "&", " " );
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( assessesCompetencies );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var competency = new IndexCompetency
									{
										Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
										Description = ( string )child.Attribute( "Description" ) ?? string.Empty
									};

									index.AssessesCompetencies.Add( competency );
									index.Competencies.Add( competency );
								}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment AssessesCompetencies id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}

						#endregion

						#region RequiresCompetencies

						string requiresCompetencies = dr[ "RequiresCompetencies" ].ToString();
						if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( requiresCompetencies ) )
							{
								requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							requiresCompetencies = requiresCompetencies.Replace( "&", " " );
							try
							{
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( requiresCompetencies );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var competency = new IndexCompetency
									{
										Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
										Description = ( string )child.Attribute( "Description" ) ?? string.Empty
									};

									index.RequiresCompetencies.Add( competency );
									index.Competencies.Add( competency );

								}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment RequiresCompetencies id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}

						#endregion

						#region TextValues

						var textValues = dr[ "TextValues" ].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								//index.TextValues.Add( ( string )child.Attribute( "TextValue" ) ?? string.Empty );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
								{


									//if ( textValue.Value.IndexOf( "-" ) > -1 )
									//	index.TextValues.Add( textValue.Value.Replace( "-", string.Empty ) );

									if ( categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
										index.Keyword.Add( textValue.Value );
									//else
									//	index.TextValues.Add( textValue.Value );
								}
								//source is just direct/indirect, more want the sourceEntityType
								var codeNotation = child.Attribute( "CodedNotation" );
								if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
								{
									index.CodedNotation.Add( codeNotation.Value );
									//if ( codeNotation.Value.IndexOf( "-" ) > -1 )
									//	index.TextValues.Add( codeNotation.Value.Replace( "-", string.Empty ) );
								}
							}
						}
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        //properties to add to textvalues
                        index.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
						//	index.TextValues.Add( index.AvailableOnlineAt );
						index.AvailabilityListing = GetRowPossibleColumn( dr, "AvailabilityListing", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( index.AvailabilityListing ) )
						//	index.TextValues.Add( index.AvailabilityListing );
						//string url = GetRowPossibleColumn( dr, "AssessmentExampleUrl", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	index.TextValues.Add( url );
						//url = GetRowPossibleColumn( dr, "ProcessStandards", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	index.TextValues.Add( url );
						//url = GetRowPossibleColumn( dr, "ScoringMethodExample", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	index.TextValues.Add( url );
						//url = GetRowPossibleColumn( dr, "ExternalResearch", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( url ) )
						//	index.TextValues.Add( url );
						//	,base.
						//if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
						//	index.TextValues.Add( index.CredentialRegistryId );
						//index.TextValues.Add( index.Id.ToString() );
						//index.TextValues.Add( index.CTID );
						//if ( !string.IsNullOrWhiteSpace( index.CodedNotation ) )
						//	index.TextValues.Add( index.CodedNotation );
						#endregion

						#region SubjectAreas

						var subjectAreas = dr[ "SubjectAreas" ].ToString();
						if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
						{
							var xDoc = XDocument.Parse( subjectAreas );
							foreach ( var child in xDoc.Root.Elements() )
								index.SubjectAreas.Add( child.Attribute( "Subject" ).Value );

							if ( index.SubjectAreas.Count() > 0 )
								index.HasSubjects = true;
						}

						#endregion

						//#region TransferValues
						//string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
						//if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
						//{
						//	if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
						//	{
						//		providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
						//	}
						//	providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( providesTransferValueFor );
						//	foreach ( var child in xDoc.Root.Elements() )
						//	{
						//		var competency = new IndexProperty
						//		{
						//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
						//			Id = ( int ) child.Attribute( "ResourceId" )
						//		};

						//		index.ProvidesTransferValueFor.Add( competency );
						//	}
						//}
						//string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
						//if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
						//{
						//	if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
						//	{
						//		receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
						//	}
						//	receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( receivesTransferValueFor );
						//	foreach ( var child in xDoc.Root.Elements() )
						//	{
						//		var competency = new IndexProperty
						//		{
						//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
						//			Id = ( int ) child.Attribute( "ResourceId" )
						//		};

						//		index.ReceivesTransferValueFrom.Add( competency );
						//	}
						//}
						//#endregion
						//
						index.AggregateDataProfileCount = GetRowColumn( dr, "AggregateDataProfileCount", 0 );
						index.DataSetProfileCount = GetRowColumn( dr, "DataSetProfileCount", 0 );
						if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
						{
							index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
						}

						#region AssessmentProperties - these are stored to enable use by gray button clicks

						try
						{
							var assessmentProperties = dr[ "AssessmentProperties" ].ToString();
							if ( !string.IsNullOrEmpty( assessmentProperties ) )
							{
								assessmentProperties = assessmentProperties.Replace( "&", " " );
								var xDoc = XDocument.Parse( assessmentProperties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;
									var schemaName = ( string )child.Attribute( "PropertySchemaName" );

									index.AssessmentProperties.Add( new IndexProperty
									{
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property,
										SchemaName = schemaName
									} );
									if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type )
										index.AssessmentMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE )
										index.AssessmentUseTypeIds.Add( propertyValueId );
									if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_Scoring_Method )
										index.ScoringMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
										index.DeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
										index.AudienceTypeIds.Add( propertyValueId );
									else if ( categoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
										index.AudienceLevelTypeIds.Add( propertyValueId );
									//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
									//	index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
									//if ( !string.IsNullOrWhiteSpace( property ) )
									//	index.TextValues.Add( property );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on AssessmentProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						
						#endregion

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = dr[ "Frameworks" ].ToString();
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							HandleFrameworks( index, frameworks );
							//var xDoc = new XDocument();
							//xDoc = XDocument.Parse( frameworks );
							//foreach ( var child in xDoc.Root.Elements() )
							//{
							//	var framework = new IndexReferenceFramework
							//	{
							//		CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
							//		ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkItemId" ).Value ),
							//		Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
							//		CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? string.Empty,
							//		SchemaName = ( string )child.Attribute( "SchemaName" ) ?? string.Empty,
							//		CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? string.Empty,
							//	};

							//	if ( framework.CategoryId == 11 )
							//		index.Occupations.Add( framework );
							//	if ( framework.CategoryId == 10 )
							//		index.Industries.Add( framework );
							//	if ( framework.CategoryId == 23 )
							//		index.InstructionalPrograms.Add( framework );
							//}
							if ( index.Occupations.Count > 0 )
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.InstructionalPrograms.Count > 0 )
								index.HasInstructionalPrograms = true;
						}

						#endregion

						#region QualityAssurance
						index.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );

						HandleAgentRelationshipsForEntity( dr, index );
						//handle QA asserted by a third party (versus by the owner)
						HandleDirectAgentRelationshipsForEntity( dr, index );
						#endregion

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							FormatAddressesToElastic( index, addresses );

							var xDoc = new XDocument();
						
						}
						if ( index.Addresses.Count == 0 )
						{
							if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
							{

								//prototype: if no resource addresses, and one org address, then add to index (not detail page)
								var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
								if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
								{
									FormatAddressesToElastic( index, orgAddresses, true );
								}
							}
						}
						#endregion

						#region language
						index.InLanguage = GetLanguages( dr );
						#endregion
						#region Widgets, collections
						HandleCollectionSelections( index, dr );
                        HandleMemberOfTransferValue( index, dr );
                        HandleWidgetSelections( index, dr, "AssessmentFilters" );

						#endregion
						#region custom reports
						int propertyId = 0;
						//indicator of in registry 
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
						{
							if ( GetPropertyId( 60, "asmtReport:IsInRegistry", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else if ( GetPropertyId( 60, "asmtReport:IsNotInRegistry", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
						//
						AddReportProperty( index, index.ResourceForCollection.Count, 60, "asmtReport:IsPartOfCollection" );

						AddReportProperty( index, index.AggregateDataProfileCount, 60, "Has Aggregate Data Profile", "asmtReport:HasAggregateDataProfile" );
						AddReportProperty( index, index.AggregateDataProfileCount + index.DataSetProfileCount, 60, "Has Outcome Data", "asmtReport:HasOutcomeData" );
						
						index.HasTransferValueProfilesCount = GetRowColumn( dr, "HasTransferValueProfileCount", 0 );
						AddReportProperty( index, index.HasTransferValueProfilesCount, 60, "Has Transfer Values", "asmtReport:HasTransferValues" );


						if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
							if ( GetPropertyId( 60, "asmtReport:AvailableOnline", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.AssessesCompetencies.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:AssessesCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.RequiresCompetencies.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:RequiresCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
                        //
                        AddReportProperty( index, index.RequiresCompetencies.Count + index.AssessesCompetencies.Count, 60, "asmtReport:HasCompetencies" );
                        //
                        if ( costProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasCostProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( conditionProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasConditionProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//
						var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
						AddReportProperty( index, DurationProfileCount, 60, "Has Duration Profile", "asmtReport:HasDurationProfile" );

						if ( index.CommonConditionsCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:ReferencesCommonConditions", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.CommonCostsCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:ReferencesCommonCosts", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.FinancialAidCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:FinancialAid", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ProcessProfilesCount > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasProcessProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasSubjects, 60, "Has Subjects", "asmtReport:HasSubjects" );
						if ( index.HasOccupations )
							if ( GetPropertyId( 60, "asmtReport:HasOccupations", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasIndustries )
							if ( GetPropertyId( 60, "asmtReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasInstructionalPrograms )
							if ( GetPropertyId( 60, "asmtReport:HasCIP", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ProvidesTransferValueFor.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:ProvidesTransferValueFor", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ReceivesTransferValueFrom.Count > 0 )
							if ( GetPropertyId( 60, "asmtReport:ReceivesTransferValueFrom", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

							list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Assessment_SearchForElastic. Last Row: {0}, asmtId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					TimeSpan duration = DateTime.Now.Subtract( startTime );
					//if ( duration.TotalSeconds > 10 )
					//{
					//	LoggingHelper.DoTrace( LoggingHelper.appTraceLevel, thisClassName + string.Format( "Assessment_SearchForElastic. mapping duration: {0:N2} seconds ", duration.TotalSeconds ) );
					//}
					LoggingHelper.DoTrace( 2, string.Format( "Assessment_SearchForElastic - Page: {0} Complete. Loaded {1} records.  mapping duration: {0:N2} seconds ", pageNumber, cntr, duration.TotalSeconds ) );
				}
				return list;
			}
		}

		public static List<PM.AssessmentProfile> Assessment_MapFromElastic( List<AssessmentIndex> assessments, int pageNbr, int pageSize )
		{
			var list = new List<PM.AssessmentProfile>();
			int rowNbr = ( pageNbr - 1 ) * pageSize;
			foreach ( var item in assessments )
			{
				rowNbr++;
				if ( item.Id == 0)
                {
					continue;
                }
				var index = new PM.AssessmentProfile
				{
					Id = item.Id,
					ResultNumber = rowNbr,
					Name = item.Name,
					FriendlyName = item.FriendlyName,
					Description = item.Description,
					EntityStateId = item.EntityStateId,
					RowId = item.RowId,
					SubjectWebpage = item.SubjectWebpage,
					AvailableOnlineAt = item.AvailableOnlineAt,
					CodedNotation = item.IdentificationCode,
					CTID = item.CTID,
					PrimaryOrganizationCTID = item.PrimaryOrganizationCTID,
					//CredentialRegistryId = item.CredentialRegistryId,
					DateEffective = item.DateEffective,
					Created = item.Created,
					//define LastUpdated to be EntityLastUpdated
					LastUpdated = item.LastUpdated,
					LifeCycleStatus = item.LifeCycleStatusType,
					RequiresCount = item.RequiresCount,
					RecommendsCount = item.RecommendsCount,
					RequiredForCount = item.IsRequiredForCount,
					IsRecommendedForCount = item.IsRecommendedForCount,
					IsAdvancedStandingForCount = item.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = item.AdvancedStandingFromCount,
					PreparationForCount = item.IsPreparationForCount,
					PreparationFromCount = item.PreparationFromCount,
					CompetenciesCount = item.Competencies.Count, //21-08-31 chg to use total
					//TotalCostCount = ai.TotalCostCount,
					CostProfilesCount = item.CostProfilesCount,
					NumberOfCostProfileItems = item.NumberOfCostProfileItems,
					CommonCostsCount = item.CommonCostsCount,
					CommonConditionsCount = item.CommonConditionsCount,
					FinancialAidCount = item.FinancialAidCount,
					AggregateDataProfileCount = item.AggregateDataProfileCount,
					DataSetProfileCount = item.DataSetProfileCount,
					TransferValueCount = item.HasTransferValueProfilesCount

				};

				if ( item.PrimaryOrganizationId > 0 )
					index.PrimaryOrganization = new MC.Organization() { Id = item.PrimaryOrganizationId, Name = item.PrimaryOrganizationName };
				index.PrimaryOrganizationFriendlyName = FormatFriendlyTitle( index.PrimaryOrganizationName );

				if ( index.EntityStateId == 2 )
				{
					index.Name += " [reference]";
				}
                if ( item.ResourceDetail != null )
                    index.ResourceDetail = item.ResourceDetail;
                //
                if ( index.CompetenciesCount == 0 && item.AssessesCompetencies != null && item.AssessesCompetencies.Any() )
					index.CompetenciesCount = item.AssessesCompetencies.Count();
				//addresses
				index.AvailableAt = item.Addresses.Select( x => new MC.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					StreetAddress = x.StreetAddress,
					//Address2 = x.Address2,
					AddressLocality = x.AddressLocality,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					Identifier = x.Identifier,
					AddressCountry = x.AddressCountry
				} ).ToList();

				index.EstimatedDuration = DurationProfileManager.GetAll( index.RowId );
				index.QualityAssurance = Fill_AgentRelationship( item.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "Assessment" );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( item.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );

				index.AssessmentMethodTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type ).ToList(), CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, false );
				index.AssessmentUseTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_ASSESSMENT_USE_TYPE, false, false );
				index.ScoringMethodTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_Scoring_Method ).ToList(), CodesManager.PROPERTY_CATEGORY_Scoring_Method, false, false );
				index.DeliveryMethodTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
				index.AudienceTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );
				index.AudienceLevelTypes = Fill_CodeItemResults( item.AssessmentProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );
				//
				if ( item.Industries != null && item.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( item.Occupations != null && item.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( item.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( item.InstructionalPrograms != null && item.InstructionalPrograms.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( item.InstructionalPrograms.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );
				//
				if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
				{
					//21-04-19 - decided to use a simple generic summary for all outcome data
					index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
					//hack 
					if ( index.AggregateDataProfileCount == 0 )
						index.AggregateDataProfileCount += index.DataSetProfileCount;
				}
				//
				if ( item.ProvidesTransferValueFor.Count > 0 )
					index.ProvidesTransferValueForElastic = Fill_CodeItemResults( item.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				if ( item.ReceivesTransferValueFrom.Count > 0 )
					index.ReceivesTransferValueFromElastic = Fill_CodeItemResults( item.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				index.CredentialsList = Fill_CredentialConnectionsResult( item.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.ListTitle = item.ListTitle;
				index.Subjects = item.SubjectAreas;
				list.Add( index );
			}

			return list;
		}

		#endregion

		#region LearningOpp Elastic Index 

		public static List<LearningOppIndex> LearningOpp_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows, bool doingReadingAndMapping = true )
		{
			string connectionString = DBConnectionRO();
			//bool usingGeneratedTable = true;
			var proc = "[LearningOpportunity.ElasticSearch]";
			//work around where cannot seem to contact the lopp elastic proc
			if ( UtilityManager.GetAppKeyValue( "using_LearningOpportunity_IndexBuild", false ) )
			{
				proc = "[LearningOpportunity.ElasticSearchV2]";
				LoggingHelper.DoTrace( 1, "LearningOpp_SearchForElastic using " + proc );
			}

			var index = new LearningOppIndex();
			var list = new List<LearningOppIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "LearningOpp_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
            DateTime started = DateTime.Now;
            using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( proc, c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );	//sort by lastest to catch most likely targets
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						var msg = FormatExceptions(ex );
                        LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic. Exception on adapter.SelectCommand: \r\n{0}", msg ) );
                        index = new LearningOppIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = msg;
						list.Add( index );
						return list;
					}
                    finally
                    {
                        DateTime searchProcCompleted = DateTime.Now;
                        var duration = searchProcCompleted.Subtract( started ).TotalSeconds;

                        LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic - Call to proc: page #{0}, took {1} seconds", pageNumber, duration ) );
                    }
                }
				int costProfilesCount = 0;
				int conditionProfilesCount = 0;

				try
				{

					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( "		processing record: {0}", cntr ) );

						index = new LearningOppIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );

						index.EntityTypeId = GetRowColumn( dr, "LearningEntityTypeId", 7 );
						index.EntityType = LearningOpportunityManager.MapLearningEntityType( index.EntityTypeId );
						index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = GetRowColumn( dr, "Description", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						index.RowId = new Guid( rowId );
						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.EntityId = GetRowPossibleColumn( dr, "EntityId", 0 );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.LearningOpportunityDetail>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );

								HandleResourceHasSupportService( index, resource.HasSupportService );
								if ( resource.ProvidesTransferValueFor != null )
								{
									index.ProvidesTransferValueFor = HandleResource( resource.ProvidesTransferValueFor );

								}
								if ( resource.ReceivesTransferValueFrom != null )
								{
									index.ReceivesTransferValueFrom = HandleResource( resource.ReceivesTransferValueFrom );

								}
								if ( resource.ObjectOfAction != null )
								{
									index.ObjectOfAction = HandleResource( resource.ObjectOfAction );

								}
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "LearningOpp_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );
						//==========================
						//TBD. just time initially
						//	actually can't do here, as need the API classes
						// OR an alternate may be to populate from factory and then call API mapping method in the lopp services method
						//DateTime getDetailStarted = DateTime.Now;
						//var lopp = LearningOpportunityManager.GetForDetail( index.Id, true );
						//DateTime getDetailCompleted = DateTime.Now;
						//var getDuration = getDetailCompleted.Subtract( getDetailStarted ).TotalSeconds;
						//==========================
						index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "Organization", string.Empty );
						index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "OrgId", 0 );
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//add helpers
						index.PrimaryOrganizationCTID = dr["OwningOrganizationCtid"].ToString();

						var date = GetRowColumn( dr, "DateEffective", string.Empty );
						if ( IsValidDate( date ) )
							index.DateEffective = DateTime.Parse( date ).ToString( "yyyy-MM-dd" );
						else
							index.DateEffective = string.Empty;

						index.Created = GetRowColumn( dr, "Created", System.DateTime.MinValue );
						index.LastUpdated = GetRowColumn( dr, "LastUpdated", System.DateTime.MinValue );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();

						index.IsNonCredit = GetRowColumn( dr, "IsNonCredit", false );
						index.IsNonCredit = GetRowColumn( dr, "IsNonCredit", false );
						if ( index.IsNonCredit )
						{
							index.Keyword.Add( "IsNonCredit" );
							index.Keyword.Add( "Is Non-Credit" );
						}
						//competencies. either arbitrarily get all, or if filters exist, only return matching ones
						index.CompetenciesCount = GetRowPossibleColumn( dr, "CompetenciesCount", 0 );

						//connections not condition profiles
						index.RequiresCount = GetRowColumn( dr, "RequiresCount", 0 );
						index.RecommendsCount = GetRowColumn( dr, "RecommendsCount", 0 );
						index.IsRequiredForCount = GetRowColumn( dr, "IsRequiredForCount", 0 );
						index.IsRecommendedForCount = GetRowColumn( dr, "IsRecommendedForCount", 0 );
						index.IsAdvancedStandingForCount = GetRowColumn( dr, "IsAdvancedStandingForCount", 0 );
						index.AdvancedStandingFromCount = GetRowColumn( dr, "AdvancedStandingFromCount", 0 );
						index.IsPreparationForCount = GetRowColumn( dr, "IsPreparationForCount", 0 );
						index.PreparationFromCount = GetRowColumn( dr, "PreparationFromCount", 0 );
						//index.TotalCostCount = GetRowPossibleColumn( dr, "TotalCostCount", 0M );
						index.NumberOfCostProfileItems = GetRowColumn( dr, "NumberOfCostProfileItems", 0 );
						costProfilesCount = index.CostProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
						conditionProfilesCount = GetRowPossibleColumn( dr, "conditionProfilesCount", 0 );

						index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
						index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
						index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );
						//TBD
						if ( index.FinancialAidCount > 0 )
						{
							//TODO - could get the details here, only where has outcomes
						}

						//index.ProcessProfilesCount = GetRowPossibleColumn( dr, "ProcessProfilesCount", 0 );
						//
						index.AggregateDataProfileCount = GetRowColumn( dr, "AggregateDataProfileCount", 0 );
						index.DataSetProfileCount = GetRowColumn( dr, "DataSetProfileCount", 0 );
						if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
						{
							index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
							//TODO - could get the details here, only where has outcomes
						}
						//-actual connection type (no credential info)
						index.ConnectionsList = dr["ConnectionsList"].ToString();
						//connection type, plus Id, and name of credential
						index.CredentialsList = dr["CredentialsList"].ToString();
						//change to use Connections
						string loppConnections = GetRowPossibleColumn( dr, "LoppConnections" );
						if ( !string.IsNullOrWhiteSpace( loppConnections ) )
						{
							Connection conn = new Connection();
							var xDoc = XDocument.Parse( loppConnections );
							foreach ( var child in xDoc.Root.Elements() )
							{
								conn = new Connection();
								conn.ConnectionType = ( string ) child.Attribute( "ConnectionType" ) ?? string.Empty;
								conn.ConnectionTypeId = int.Parse( child.Attribute( "ConnectionTypeId" ).Value );

								//do something with counts for this type

								conn.CredentialId = int.Parse( child.Attribute( "CredentialId" ).Value );
								if ( conn.CredentialId > 0 )
								{
									//add credential
									conn.Credential = ( string ) child.Attribute( "CredentialName" ) ?? string.Empty;
									//??????
									conn.CredentialOrgId = int.Parse( child.Attribute( "credOrgid" ).Value );
									conn.CredentialOrganization = ( string ) child.Attribute( "credOrganization" ) ?? string.Empty;
								}
								conn.AssessmentId = int.Parse( child.Attribute( "AssessmentId" ).Value );
								if ( conn.AssessmentId > 0 )
								{
									conn.Assessment = ( string ) child.Attribute( "AssessmentName" ) ?? string.Empty;
									conn.AssessmentOrganizationId = int.Parse( child.Attribute( "asmtOrgid" ).Value );
									conn.AssessmentOrganization = ( string ) child.Attribute( "asmtOrganization" ) ?? string.Empty;
								}
								conn.LoppId = int.Parse( child.Attribute( "LearningOpportunityId" ).Value );
								if ( conn.LoppId > 0 )
								{
									conn.LearningOpportunity = ( string ) child.Attribute( "LearningOpportunityName" ) ?? string.Empty;
									conn.LoppOrganizationId = int.Parse( child.Attribute( "loppOrgid" ).Value );
									conn.LearningOpportunityOrganization = ( string ) child.Attribute( "loppOrganization" ) ?? string.Empty;
								}

								index.Connections.Add( conn );
							}
						}

						#region TeachesCompetencies

						string teachesCompetencies = dr["TeachesCompetencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( teachesCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( teachesCompetencies ) )
							{
								teachesCompetencies = Regex.Replace( teachesCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							teachesCompetencies = teachesCompetencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( teachesCompetencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.TeachesCompetencies.Add( competency );
								index.Competencies.Add( competency );

							}
						}

						#endregion
						#region AssessesCompetencies
						//21-08-31 - currently the competency search is not specific for assesses or requires. So use the general competencies for now
						var assessesCompetencies = GetRowColumn( dr, "AssessesCompetencies" );
						if ( !string.IsNullOrWhiteSpace( assessesCompetencies ) )
						{
							try
							{
								if ( ContainsUnicodeCharacter( assessesCompetencies ) )
								{
									assessesCompetencies = Regex.Replace( assessesCompetencies, @"[^\u0000-\u007F]+", string.Empty );

								}
								assessesCompetencies = assessesCompetencies.Replace( "&", " " );
								var xDoc = new XDocument();
								xDoc = XDocument.Parse( assessesCompetencies );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var competency = new IndexCompetency
									{
										Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
										Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
									};

									index.AssessesCompetencies.Add( competency );
									index.Competencies.Add( competency );
								}
							}
							catch ( Exception ex )
							{
								LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on Assessment AssessesCompetencies id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
							}
						}

						#endregion

						#region RequiresCompetencies

						string requiresCompetencies = dr["RequiresCompetencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( requiresCompetencies ) )
						{
							if ( ContainsUnicodeCharacter( requiresCompetencies ) )
							{
								requiresCompetencies = Regex.Replace( requiresCompetencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							requiresCompetencies = requiresCompetencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( requiresCompetencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.RequiresCompetencies.Add( competency );
								index.Competencies.Add( competency );

							}
						}

						#endregion

						#region SubjectAreas

						var subjectAreas = dr["SubjectAreas"].ToString();
						if ( !string.IsNullOrWhiteSpace( subjectAreas ) )
						{
							var xDoc = XDocument.Parse( subjectAreas );
							foreach ( var child in xDoc.Root.Elements() )
								index.SubjectAreas.Add( child.Attribute( "Subject" ).Value );

							if ( index.SubjectAreas.Count() > 0 )
								index.HasSubjects = true;
						}

						#endregion

						#region TextValues

						var textValues = dr["TextValues"].ToString();
						if ( !string.IsNullOrWhiteSpace( textValues ) )
						{
							textValues = textValues.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( textValues );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null && !string.IsNullOrWhiteSpace( textValue.Value ) )
								{

									//if ( textValue.Value.IndexOf( "-" ) > -1 )
									//	index.TextValues.Add( textValue.Value.Replace( "-", string.Empty ) );

									if ( categoryId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
										index.Keyword.Add( textValue.Value );
									//else
									//	index.TextValues.Add( textValue.Value );
								}
								//source is just direct/indirect, more want the sourceEntityType
								var codeNotation = child.Attribute( "CodedNotation" );
								if ( codeNotation != null && !string.IsNullOrWhiteSpace( codeNotation.Value ) )
								{
									index.CodedNotation.Add( codeNotation.Value );
									//if ( codeNotation.Value.IndexOf( "-" ) > -1 )
									//	index.TextValues.Add( codeNotation.Value.Replace( "-", string.Empty ) );
								}
							}
						}
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );
						//properties to add to textvalues
						index.AvailableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );
						//AddTextValue( index, index.AvailableOnlineAt );

						string url = GetRowPossibleColumn( dr, "AvailabilityListing", string.Empty );
						//AddTextValue( index, url );
						var codedNotation = GetRowColumn( dr, "IdentificationCode", string.Empty );
						if ( !string.IsNullOrWhiteSpace( codedNotation ) )
							index.CodedNotation.Add( codedNotation );


						#endregion

						#region LoppProperties
						try
						{
							var loppProperties = dr["LoppProperties"].ToString();
							if ( !string.IsNullOrEmpty( loppProperties ) )
							{
								loppProperties = loppProperties.Replace( "&", " " );
								var xDoc = XDocument.Parse( loppProperties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;

									index.LoppProperties.Add( new IndexProperty
									{
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property
									} );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Learning_Method_Type )
										index.LearningMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
										index.DeliveryMethodTypeIds.Add( propertyValueId );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE )
										index.AudienceTypeIds.Add( propertyValueId );
									else if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL )
										index.AudienceLevelTypeIds.Add( propertyValueId );
									else if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type )
										index.AssessmentMethodTypeIds.Add( propertyValueId );
									//if ( !string.IsNullOrWhiteSpace( ( string )child.Attribute( "PropertySchemaName" ) ) )
									//	index.TextValues.Add( ( string )child.Attribute( "PropertySchemaName" ) );
									//if ( !string.IsNullOrWhiteSpace( property ) )
									//	index.TextValues.Add( property );
								}
							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on LoppProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
						#endregion

						//#region TransferValues
						//string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
						//if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
						//{
						//	if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
						//	{
						//		providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
						//	}
						//	providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( providesTransferValueFor );
						//	foreach ( var child in xDoc.Root.Elements() )
						//	{
						//		var competency = new IndexProperty
						//		{
						//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
						//			Id = ( int ) child.Attribute( "ResourceId" )
						//		};

						//		index.ProvidesTransferValueFor.Add( competency );
						//	}
						//}
						//string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
						//if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
						//{
						//	if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
						//	{
						//		receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
						//	}
						//	receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( receivesTransferValueFor );
						//	foreach ( var child in xDoc.Root.Elements() )
						//	{
						//		var competency = new IndexProperty
						//		{
						//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
						//			Id = ( int ) child.Attribute( "ResourceId" )
						//		};

						//		index.ReceivesTransferValueFrom.Add( competency );
						//	}
						//}
						//#endregion

						//string objectOfAction = dr["ObjectOfAction"].ToString();
						//if ( !string.IsNullOrWhiteSpace( objectOfAction ) )
						//{
						//	if ( ContainsUnicodeCharacter( objectOfAction ) )
						//	{
						//		objectOfAction = Regex.Replace( objectOfAction, @"[^\u0000-\u007F]+", string.Empty );
						//	}
						//	objectOfAction = objectOfAction.Replace( "&", " " );
						//	var xDoc = new XDocument();
						//	xDoc = XDocument.Parse( objectOfAction );
						//	foreach ( var child in xDoc.Root.Elements() )
						//	{
						//		var competency = new IndexProperty
						//		{
						//			Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
						//			Id = ( int ) child.Attribute( "ResourceId" )
						//		};

						//		index.ObjectOfAction.Add( competency );
						//	}
						//}

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = dr["Frameworks"].ToString();
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							HandleFrameworks( index, frameworks );
							//var xDoc = new XDocument();
							//xDoc = XDocument.Parse( frameworks );
							//foreach ( var child in xDoc.Root.Elements() )
							//{
							//	var framework = new IndexReferenceFramework
							//	{
							//		CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
							//		ReferenceFrameworkId = int.Parse( child.Attribute( "ReferenceFrameworkItemId" ).Value ),
							//		Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
							//		CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? string.Empty,
							//		SchemaName = ( string )child.Attribute( "SchemaName" ) ?? string.Empty,
							//		CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? string.Empty,
							//	};

							//	if ( framework.CategoryId == 11 )
							//		index.Occupations.Add( framework );
							//	if ( framework.CategoryId == 10 )
							//		index.Industries.Add( framework );
							//	if ( framework.CategoryId == 23 )
							//		index.InstructionalPrograms.Add( framework );
							//}
							if ( index.Occupations.Count > 0 )
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.InstructionalPrograms.Count > 0 )
								index.HasInstructionalPrograms = true;
						}


						#endregion
						//relationships 
						//may need a fall back to populate the entity cache if no data
						//or only do if a small number or maybe single implying simple reindex
						//24-03-07 mp - no need to take the hit if not present
						string agentRelationshipsForEntity = GetRowPossibleColumn( dr, "AgentRelationshipsForEntity" );
						if ( string.IsNullOrWhiteSpace( agentRelationshipsForEntity )  )
						{
							//&& ( result.Rows.Count == 1 || index.EntityStateId == 2 )
							//NOTE: may want to disable this once everything rebuilt:
							string statusMessage = string.Empty;
							if (new EntityManager().EntityCacheUpdateAgentRelationshipsForLopp( index.RowId.ToString(), ref statusMessage ))
							{
								var ec = EntityManager.EntityCacheGetByGuid( index.RowId );
								if ( ec != null && ec.Id > 1 && !string.IsNullOrWhiteSpace(ec.AgentRelationshipsForEntity ) ) 
								{
									HandleAgentRelationshipsForEntity( ec.AgentRelationshipsForEntity, index );
								}								
							}
						}
						else 
						if ( agentRelationshipsForEntity.ToLower() != "none" )
							HandleAgentRelationshipsForEntity( dr, index );


						#region QualityAssurance
						index.Org_QAAgentAndRoles = GetRowPossibleColumn( dr, "Org_QAAgentAndRoles" );


						//handle QA asserted by a third party (versus by the owner)
						HandleDirectAgentRelationshipsForEntity( dr, index );

						#endregion

						#region Addresses
						var addresses = dr[ "Addresses" ].ToString();
						if ( !string.IsNullOrWhiteSpace( addresses ) )
						{
							FormatAddressesToElastic( index, addresses );
						}

						if ( index.Addresses.Count == 0 )
						{
							if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
							{

								//prototype: if no resource addresses, and one org address, then add to index (not detail page)
								var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
								if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
								{
									FormatAddressesToElastic( index, orgAddresses, true );
								}
							}
						}
						#endregion

						#region Languages
						index.InLanguage = GetLanguages( dr );
						#endregion
						#region Widgets, collections
						HandleCollectionSelections( index, dr );
                        HandleMemberOfTransferValue( index, dr );
                        HandleWidgetSelections( index, dr, "LearningOpportunityFilters" );

						#endregion
						#region custom reports
						int propertyId = 0;
						//indicator of in registry 
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
						{
							if ( GetPropertyId( 61, "loppReport:IsInRegistry", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						else if ( GetPropertyId( 61, "loppReport:IsNotInRegistry", ref propertyId ) )
							index.ReportFilters.Add( propertyId );
						//
						if ( index.IsNonCredit )
						{
							if ( GetPropertyId( 61, "loppReport:IsNonCredit", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						}
						//
						AddReportProperty( index, index.ResourceForCollection.Count, 61, "loppReport:IsPartOfCollection" );

						AddReportProperty( index, index.AggregateDataProfileCount, 61, "Has Aggregate Data Profile", "loppReport:HasAggregateDataProfile" );
						AddReportProperty( index, index.AggregateDataProfileCount + index.DataSetProfileCount, 61, "Has Outcome Data", "loppReport:HasOutcomeData" );

						index.HasTransferValueProfilesCount = GetRowColumn( dr, "HasTransferValueProfileCount", 0 );
						AddReportProperty( index, index.HasTransferValueProfilesCount, 61, "Has Transfer Values", "loppReport:HasTransferValues" );

						
						if ( !string.IsNullOrWhiteSpace( index.AvailableOnlineAt ) )
							if ( GetPropertyId( 61, "loppReport:AvailableOnline", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.TeachesCompetencies.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:TeachesCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.RequiresCompetencies.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:RequiresCompetencies", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
                        //
                        AddReportProperty( index, index.RequiresCompetencies.Count + index.TeachesCompetencies.Count, 61, "loppReport:HasCompetencies" );
						//
                        var DurationProfileCount = GetRowPossibleColumn( dr, "HasDurationCount", 0 );
						AddReportProperty( index, DurationProfileCount, 61, "Has Duration Profile", "loppReport:HasDurationProfile" );

						if ( costProfilesCount > 0 )
							if ( GetPropertyId( 61, "loppReport:HasCostProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( conditionProfilesCount > 0 )
							if ( GetPropertyId( 61, "loppReport:HasConditionProfile", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						if ( index.CommonConditionsCount > 0 )
							if ( GetPropertyId( 61, "loppReport:ReferencesCommonConditions", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.CommonCostsCount > 0 )
							if ( GetPropertyId( 61, "loppReport:ReferencesCommonCosts", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.FinancialAidCount > 0 )
							if ( GetPropertyId( 61, "loppReport:FinancialAid", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//if ( index.ProcessProfilesCount > 0 )
						//	if ( GetPropertyId( 61, "loppReport:HasProcessProfile", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasSubjects, 61, "Has Subjects", "loppReport:HasSubjects" );
						if ( index.HasOccupations )
							if ( GetPropertyId( 61, "loppReport:HasOccupations", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasIndustries )
							if ( GetPropertyId( 61, "loppReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasInstructionalPrograms )
							if ( GetPropertyId( 61, "loppReport:HasCIP", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						//do we want to distinguish between an org address and a resource address??
						if ( index.Addresses.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:HasAddresses", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ProvidesTransferValueFor.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:ProvidesTransferValueFor", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ReceivesTransferValueFrom.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:ReceivesTransferValueFrom", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.ObjectOfAction.Count > 0 )
							if ( GetPropertyId( 61, "loppReport:ObjectOfAction", ref propertyId ) )
								index.ReportFilters.Add( propertyId );

						list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
                    var msg = FormatExceptions( ex );
                    LoggingHelper.DoTrace( 2, string.Format( "LearningOpp_SearchForElastic. Last Row: {0}, LoppId: {1} Exception: \r\n{2}", cntr, index.Id, msg ) );
				}
				finally
				{
                    DateTime completed = DateTime.Now;
                    var duration = completed.Subtract( started ).TotalSeconds;

                    LoggingHelper.DoTrace( 2, string.Format( thisClassName+ ".LearningOpp_SearchForElastic - Page: {0} Complete. Loaded {1} records, in {2} seconds", pageNumber, cntr, duration ) );
				}
				return list;
			}
		}

		public static List<PM.LearningOpportunityProfile> LearningOpp_MapFromElastic( List<LearningOppIndex> learningOpps, int pageNbr, int pageSize )
		{
			var list = new List<PM.LearningOpportunityProfile>();
			int rowNbr = ( pageNbr - 1 ) * pageSize;
			foreach ( var item in learningOpps )
			{
				rowNbr++;
				

				var index = new PM.LearningOpportunityProfile
				{
					Id = item.Id,
					EntityTypeId = item.EntityTypeId,
					ResultNumber = rowNbr,
					Name = item.Name,
					FriendlyName = item.FriendlyName,
					Description = item.Description,
					EntityStateId = item.EntityStateId,
					RowId = item.RowId,
					SubjectWebpage = item.SubjectWebpage,
					AvailableOnlineAt = item.AvailableOnlineAt,
					CodedNotation = item.IdentificationCode,
					CTID = item.CTID,
					PrimaryOrganizationCTID = item.PrimaryOrganizationCTID,
					//CredentialRegistryId = li.CredentialRegistryId,
					DateEffective = item.DateEffective,
					Created = item.Created,
					LastUpdated = item.LastUpdated,
					LifeCycleStatus = item.LifeCycleStatusType,
					RequiresCount = item.RequiresCount,
					RecommendsCount = item.RecommendsCount,
					RequiredForCount = item.IsRequiredForCount,
					IsRecommendedForCount = item.IsRecommendedForCount,
					IsAdvancedStandingForCount = item.IsAdvancedStandingForCount,
					AdvancedStandingFromCount = item.AdvancedStandingFromCount,
					IsNonCredit=item.IsNonCredit,
					PreparationForCount = item.IsPreparationForCount,
					PreparationFromCount = item.PreparationFromCount,
					CompetenciesCount = item.Competencies.Count, //21-08-31 - use total competencies
					//TotalCostCount = li.TotalCostCount,
					CostProfilesCount = item.CostProfilesCount,
					NumberOfCostProfileItems = item.NumberOfCostProfileItems,

					CommonCostsCount = item.CommonCostsCount,
					CommonConditionsCount = item.CommonConditionsCount,
					FinancialAidCount = item.FinancialAidCount,
					AggregateDataProfileCount = item.AggregateDataProfileCount,
					DataSetProfileCount = item.DataSetProfileCount,
					TransferValueCount = item.HasTransferValueProfilesCount,


				};
				//or take this hit in populate elastic
				if ( index.EntityTypeId > 0 )
				{
					index.LearningEntityType = LearningOpportunityManager.MapLearningEntityType( index.EntityTypeId );
					index.CTDLTypeLabel = LearningOpportunityManager.MapLearningEntityTypeLabel( index.EntityTypeId );
					index.LearningTypeSchema = "ceterms:" +  index.LearningEntityType;
				}
				//
				if ( item.PrimaryOrganizationId > 0 )
					index.PrimaryOrganization = new MC.Organization() { Id = item.PrimaryOrganizationId, Name = item.PrimaryOrganizationName };
				index.PrimaryOrganizationFriendlyName = FormatFriendlyTitle( index.PrimaryOrganizationName );

				if ( index.EntityStateId == 2 )
				{
					index.Name += " [reference]";
				}
				if ( item.ResourceForCollection.Count > 0)
                {
					index.InCollectionCount = item.ResourceForCollection.Count;
                }
				if ( item.ResourceDetail != null )
					index.ResourceDetail = item.ResourceDetail;
				else
				{
					LoggingHelper.DoTrace( 5, $"Lopp #{index.Id}, '{index.Name}', EntityStateId: {index.EntityStateId} missing resource detail" );
				}

				//
				if ( index.CompetenciesCount == 0 && item.TeachesCompetencies != null && item.TeachesCompetencies.Any() )
					index.CompetenciesCount = item.TeachesCompetencies.Count();

				//addressess                
				index.AvailableAt = item.Addresses.Select( x => new MC.Address
				{
					Latitude = x.Latitude,
					Longitude = x.Longitude,
					StreetAddress = x.StreetAddress,
					AddressLocality = x.AddressLocality,
					AddressRegion = x.AddressRegion,
					PostalCode = x.PostalCode,
					AddressCountry = x.AddressCountry, 
					Identifier = x.Identifier
				} ).ToList();
				//
				//not used yet. Prototype for general use (subregions)

				//get distinct region identifiers
				var regions = new List<string>();
				if ( item.RegionIdentifier != null && item.RegionIdentifier.Count > 0)
                {
					regions = item.RegionIdentifier.Select( s => s.IdentifierType ).Distinct().ToList();
						
				}

				var lwias = item.RegionIdentifier
						.Where( b => b.IdentifierType == illinoisLWIAIdentityType )
						.ToList();
				//var lwias2 = ( from record in item.RegionIdentifier
				//			   where record.IdentifierType == illinoisLWIAIdentityType
				//			   select new { record.IdentifierValueCode }
				//			)
				//		.ToList();
				if ( lwias?.Count > 0 )
				{

				}
				var edrs = item.RegionIdentifier
						.Where( b => b.IdentifierType == illinoisEDRIdentityType )
						.ToList();
				var edrs2 = ( from record in item.RegionIdentifier
							  where record.IdentifierType == illinoisEDRIdentityType
							  select new { record.IdentifierValueCode }
							)
						.ToList();
				//
				index.EstimatedDuration = DurationProfileManager.GetAll( index.RowId );
				index.Org_QAAgentAndRoles = Fill_AgentRelationship( item.Org_QAAgentAndRoles, 130, false, false, true, "Organization" );
				index.QualityAssurance = Fill_AgentRelationship( item.AgentRelationshipsForEntity, CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE, "LearningOpportunity" );
				index.CredentialsList = Fill_CredentialConnectionsResult( item.CredentialsList, CodesManager.PROPERTY_CATEGORY_CONNECTION_PROFILE_TYPE );
				index.Subjects = item.SubjectAreas;

				if ( item.Industries != null && item.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );

				if ( item.Occupations != null && item.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( item.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );

				if ( item.InstructionalPrograms != null && item.InstructionalPrograms.Count > 0 )
					index.InstructionalProgramClassification = Fill_CodeItemResults( item.InstructionalPrograms.Where( x => x.CategoryId == 23 ).ToList(), CodesManager.PROPERTY_CATEGORY_CIP, false, false );

				index.LearningMethodTypes = Fill_CodeItemResults( item.LoppProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_Learning_Method_Type ).ToList(), CodesManager.PROPERTY_CATEGORY_Learning_Method_Type, false, false );
				index.DeliveryMethodTypes = Fill_CodeItemResults( item.LoppProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE, false, false );
				index.AudienceTypes = Fill_CodeItemResults( item.LoppProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_TYPE, false, false );
				index.AudienceLevelTypes = Fill_CodeItemResults( item.LoppProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL ).ToList(), CodesManager.PROPERTY_CATEGORY_AUDIENCE_LEVEL, false, false );
				index.AssessmentMethodTypes = Fill_CodeItemResults( item.LoppProperties.Where( x => x.CategoryId == ( int )CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type ).ToList(), CodesManager.PROPERTY_CATEGORY_Assessment_Method_Type, false, false );
				//
				if ( item.ProvidesTransferValueFor.Count > 0 )
					index.ProvidesTransferValueForElastic = Fill_CodeItemResults( item.ProvidesTransferValueFor, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				if ( item.ReceivesTransferValueFrom.Count > 0 )
					index.ReceivesTransferValueFromElastic = Fill_CodeItemResults( item.ReceivesTransferValueFrom, CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE, false, false );
				//
				if ( item.ObjectOfAction.Count > 0 )
					index.ObjectOfActionElastic = Fill_CodeItemResults( item.ObjectOfAction, CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION, false, false );
				//
				if ( index.AggregateDataProfileCount > 0 || index.DataSetProfileCount > 0 )
				{
					//21-04-19 - decided to use a simple generic summary for all outcome data
					index.AggregateDataProfileSummary = string.Format( "Outcome data is available for '{0}'.", index.Name );
					//hack 
					if ( index.AggregateDataProfileCount == 0 )
						index.AggregateDataProfileCount += index.DataSetProfileCount;
				}
				//add at end as to not affect other uses
				if ( UtilityManager.GetAppKeyValue( "environment" ) != "production" )
				{
					if ( item.LWIAList?.Count > 0 )
					{
						//index.Name += " [ LWIA(s): " + String.Join( ", ", item.LWIAList ) + "]";
						//OR before descr? OR add a special pill for debug stuff
						index.Description = "[ LWIA(s): " + String.Join( ", ", item.LWIAList ) + "]. " + index.Description;
					}
				}
				//
				index.ListTitle = item.ListTitle;
				list.Add( index );
			}

			return list;
		}
		#endregion


		#region CompetencyFramework Elastic Index
		public static List<CompetencyFrameworkIndex> CompetencyFramework_SearchForElastic( string filter, int pageNumber = 1, int pageSize = 0 )
		{
			int pTotalRows = 0;
			return CompetencyFramework_SearchForElastic( filter, pageNumber, pageSize, ref pTotalRows );
		}
		public static List<CompetencyFrameworkIndex> CompetencyFramework_SearchForElastic( string filter, int pageNumber, int pageSize, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new CompetencyFrameworkIndex();
			var list = new List<CompetencyFrameworkIndex>();
			if ( pageSize < 10 )
				pageSize = 200;

			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[CompetencyFramework.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );
					}
					catch ( Exception ex )
					{
						index = new CompetencyFrameworkIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{


					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " CompetencyFramework loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new CompetencyFrameworkIndex();
						index.EntityTypeId = 10;
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.SourceUrl = GetRowColumn( dr, "SourceUrl", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//add helpers
						//20-06-11 - need to ensure have data for owner, creator, publisher
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );

						//used for autocomplete and phrase prefix queries
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );
						//24-04-17 mp - the graph can be quite large and it is not being used, so stopping adding to elastic
						index.CompetencyFrameworkGraph = string.Empty;//GetRowPossibleColumn( dr, "CompetencyFrameworkGraph", string.Empty );

						index.TotalCompetencies = GetRowPossibleColumn( dr, "TotalCompetencies", 0 );
						var competenciesStore = GetRowPossibleColumn( dr, "CompetenciesStore", string.Empty );
						if ( !string.IsNullOrWhiteSpace( competenciesStore ) )
						{
							//20-07-02 - changed to just store competencies in CompetencyFrameworkGraph
							//populate competencies - max of??
							index.Competencies = LoadCompetencies( index.Name, competenciesStore );
						}
						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion
						index.ReferencedByAssessments = GetRowPossibleColumn( dr, "ReferencedByAssessments", 0 );
						index.ReferencedByCredentials = GetRowPossibleColumn( dr, "ReferencedByCredentials", 0 );
						index.ReferencedByLearningOpportunities = GetRowPossibleColumn( dr, "ReferencedByLearningOpportunities", 0 );

						#region Custom Reports
						int propertyId = 0;
						//TODO
						//indicator of in registry 
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
						{
							if ( GetPropertyId( 69, "cfReport:IsInRegistry", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						} else if ( GetPropertyId( 69, "cfReport:IsNotInRegistry", ref propertyId ) )
							index.ReportFilters.Add( propertyId );

						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "CompetencyFramework_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "CompetencyFramework_SearchForElastic - Complete found {0} records", cntr ) );
				}


				return list;
			}
		}

		public static List<IndexCompetency> LoadCompetencies( string competencyFramework, string competencyList )
		{
			var list = new List<IndexCompetency>();
			int maxEntries = 30;
			int cntr = 0;
			try
			{

				var input = JsonConvert.DeserializeObject<List<IndexCompetency>>( competencyList );

				foreach ( var item in input )
				{
					cntr++;
					list.Add( item );
					if ( cntr >= maxEntries )
						break;
				}

			} catch ( Exception ex )
			{
				//may not do anything
				LoggingHelper.DoTrace( 1, string.Format( "ElasticManager.LoadCompetencies failed for: {0}.  ", competencyFramework ) + ex.Message );
			}

			return list;
		}

		public static List<IndexCompetency> LoadCompetenciesFromGraph( string competencyFramework, ref int propertyId )
		{
			var list = new List<IndexCompetency>();
			Dictionary<string, object> dictionary = JsonHelper.JsonToDictionary( competencyFramework );

			//parse graph in to list of objects
			JArray graphList = JArray.Parse( competencyFramework );
			int cntr = 0;
			foreach ( var item in graphList )
			{
				cntr++;
				//note older frameworks will not be in the priority order
				var main = item.ToString();
				if ( cntr == 1 || main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
				{
					//HACK
					if ( main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
					{
						//input = JsonConvert.DeserializeObject<InputEntity>( main );
					}
				}
				else
				{
					//should just have competencies, but should check for bnodes
					var child = item.ToString();
					if ( child.IndexOf( "_:" ) > -1 )
					{
						//shouldn't happen
					}
					else if ( child.IndexOf( "ceasn:Competency" ) > -1 )
					{
						//competencies.Add( JsonConvert.DeserializeObject<InputCompetency>( child ) );
					}
					else
					{
						//unexpected
					}
				}
			}


			return list;

		}

		public static List<PM.CompetencyFrameworkSummary> CompetencyFramework_MapFromElastic( List<CompetencyFrameworkIndex> CompetencyFrameworks, int pageNbr, int pageSize )
		{
			var list = new List<PM.CompetencyFrameworkSummary>();
			int rowNbr = ( pageNbr - 1 ) * pageSize;
			foreach ( var oi in CompetencyFrameworks )
			{
				rowNbr++;
				var index = new PM.CompetencyFrameworkSummary
				{
					Id = oi.Id,
					ResultNumber = rowNbr,
					Name = oi.Name,
					FriendlyName = oi.FriendlyName,
					RowId = oi.RowId,
					Description = oi.Description,
					CTID = oi.CTID,
					Source = oi.SourceUrl,
					OrganizationCTID = oi.OwnerOrganizationCTID,
					OrganizationId = oi.PrimaryOrganizationId,
					//FrameworkUri = oi.FrameworkUri,
					//CredentialRegistryId = oi.CredentialRegistryId,
					EntityStateId = oi.EntityStateId,
					//DateEffective = ci.DateEffective,
					Created = oi.Created,
					LastUpdated = oi.LastUpdated,
				};
				index.PrimaryOrganizationCTID = oi.OwnerOrganizationCTID;
				if ( oi.PrimaryOrganizationId > 0)
				{
					index.PrimaryOrganization = OrganizationManager.GetForSummary( oi.PrimaryOrganizationId );
				} else if ( !string.IsNullOrWhiteSpace( oi.PrimaryOrganizationCTID))
				{
					index.PrimaryOrganization = OrganizationManager.GetMinimumSummaryByCtid( oi.PrimaryOrganizationCTID );
				}

				//NOTE: these are not used in the search results yet
				index.ReferencedByAssessments = oi.ReferencedByAssessments;
				index.ReferencedByCredentials = oi.ReferencedByCredentials;
				index.ReferencedByLearningOpportunities = oi.ReferencedByLearningOpportunities;
				index.TotalCompetencies = oi.TotalCompetencies;
				foreach ( var item in oi.Competencies )
				{
					index.Competencies.Add( item );
				}
				//TBD for format
				index.CompetencyFrameworkGraph = oi.CompetencyFrameworkGraph;
				//split out properties

				if ( IsValidDate( oi.Created ) )
					index.Created = oi.Created;

				if ( IsValidDate( oi.LastUpdated ) )
					index.LastUpdated = oi.LastUpdated;



				list.Add( index );
			}

			return list;
		}
		private void UnpackGraph( string graph )
		{
			JArray graphList = JArray.Parse( graph );
			//Dictionary<string,<object>> input = null;// RA.Models.JsonV2.CompetencyFramework;
			Dictionary<string, object> input = new Dictionary<string, object>();
			int cntr = 0;
			foreach ( var item in graphList )
			{
				cntr++;
				//note older frameworks will not be in the priority order
				var main = item.ToString();
				if ( cntr == 1 || main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
				{
					//HACK

					if ( main.IndexOf( "ceasn:CompetencyFramework" ) > -1 )
					{
						input = JsonConvert.DeserializeObject<Dictionary<string, object>>( main );
					}
				}
				else
				{
					//Error converting value "https://credentialengineregistry.org/resources/ce-949fcaba-45ed-44d9-88bf-43677277eb84" to type 'System.Collections.Generic.List`1[System.String]'. Path 'ceasn:isPartOf', line 11, position 108.
					//not set up to handle issues
					//comp = JsonConvert.DeserializeObject<InputCompetency>( item.ToString() );
					//competencies.Add( comp );

					//should just have competencies, but should check for bnodes
					var child = item.ToString();
					if ( child.IndexOf( "_:" ) > -1 )
					{
						//bnodes.Add( JsonConvert.DeserializeObject<BNode>( child ) );
						//ceasn:Competency
					}
					else if ( child.IndexOf( "ceasn:Competency" ) > -1 )
					{
						var comp = JsonConvert.DeserializeObject<Dictionary<string, object>>( child );
						//now what
					}
					else
					{
						//unexpected
					}
				}
			}

		}
		#endregion

		#region Pathway Elastic Index
		public static List<PathwayIndex> Pathway_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new PathwayIndex();
			var list = new List<PathwayIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Pathway.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new PathwayIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Pathway loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new PathwayIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY;
						index.EntityType = "Pathway";
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//add helpers
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
                        //
						var properties = GetRowColumn( dr, "Properties", string.Empty );
                        if ( !string.IsNullOrWhiteSpace( properties ) )
                        {
                            var pcp = JsonConvert.DeserializeObject<MC.PathwayJSONProperties>( properties );
                            if ( pcp != null )
                            {
                                index.AllowUseOfPathwayDisplay = pcp.AllowUseOfPathwayDisplay;
                            }
                        }

						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.Pathway>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Pathway_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						//
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );
						//index.PathwayGraph = GetRowPossibleColumn( dr, "PathwayGraph", string.Empty );
						//if ( !string.IsNullOrWhiteSpace( index.PathwayGraph ) )
						//{
						//	//populate competencies
						//	index.Competencies = new List<IndexCompetency>();
						//}
						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						
						HandleAgentRelationshipsForEntity( dr, index );
						//
						if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
						{
							var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
							if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
							{
								FormatAddressesToElastic( index, orgAddresses, true );
							}
						}
						HandleCollectionSelections( index, dr );

						#region Reference Frameworks - industries, occupations and classifications
						string frameworks = GetRowColumn( dr, "Frameworks" );
						if ( !string.IsNullOrWhiteSpace( frameworks ) )
						{
							HandleFrameworks( index, frameworks );

							if ( index.Occupations.Count > 0 )
								index.HasOccupations = true;
							if ( index.Industries.Count > 0 )
								index.HasIndustries = true;
							if ( index.InstructionalPrograms.Count > 0 )
								index.HasInstructionalPrograms = true;
						}


						#endregion
						#region Subjects
						var subjects = GetRowColumn( dr, "TextValues", string.Empty );
						if ( !string.IsNullOrWhiteSpace( subjects ) )
						{
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( subjects );
							foreach ( var child in xDoc.Root.Elements() )
							{
								//var cs = new IndexSubject();
								var textValue = child.Attribute( "TextValue" );
								if ( textValue != null )
								{
									var text = textValue.Value;
									int outputId = 0;
									var categoryId = child.Attribute( "CategoryId" );
									if ( categoryId != null )
										if ( Int32.TryParse( categoryId.Value, out outputId ) )
										{
											if ( outputId == 34 )
												index.SubjectAreas.Add( text );
											else if ( outputId == CodesManager.PROPERTY_CATEGORY_KEYWORD )
												index.Keyword.Add( text );
										}
								}
							}
							//if ( index.SubjectAreas.Count() > 0 )
							//	index.HasSubjects = true;
						}
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        #endregion
                        #region Custom Reports
                        int propertyId = 0;
						if ( index.HasOccupations )
							if ( GetPropertyId( 70, "pathwayReport:HasOccupations", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						if ( index.HasIndustries )
							if ( GetPropertyId( 70, "pathwayReport:HasIndustries", ref propertyId ) )
								index.ReportFilters.Add( propertyId );
						AddReportProperty( index, index.HasSubjects, 70, "Has Subjects", "pathwayReport:HasSubjects" );
						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Pathway_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Pathway_SearchForElastic - Complete loaded {0} records", cntr ) );
				}


				return list;
			}
		}


		public static List<MC.CommonSearchSummary> Pathway_MapFromElastic( List<PathwayIndex> Pathways, int pageNbr = 1, int pageSize = 50)
		{
			var list = new List<MC.CommonSearchSummary>();
			int rowNbr = ( pageNbr - 1 ) * pageSize;

			foreach ( var item in Pathways )
			{
				rowNbr++;

				var index = new MC.CommonSearchSummary
				{
					Id = item.Id,
					ResultNumber = rowNbr,

					Name = item.Name,
					FriendlyName = item.FriendlyName,
					RowId = item.RowId,
					Description = item.Description,
					CTID = item.CTID,
					SubjectWebpage = item.SubjectWebpage,
					PrimaryOrganizationCTID = item.PrimaryOrganizationCTID,
					PrimaryOrganizationId = item.PrimaryOrganizationId,
					PrimaryOrganizationName = item.PrimaryOrganizationName,
					//FrameworkUri = oi.FrameworkUri,
					//CredentialRegistryId = item.CredentialRegistryId,
					EntityStateId = item.EntityStateId,
					//DateEffective = ci.DateEffective,
					Created = item.Created,
					LastUpdated = item.LastUpdated,
				};

				if ( string.IsNullOrWhiteSpace( index.FriendlyName ) )
					index.FriendlyName = FormatFriendlyTitle( index.Name );
				index.PrimaryOrganizationFriendlyName = FormatFriendlyTitle( index.PrimaryOrganizationName );

				//TBD for format
				//index. = oi.PathwayGraph;

				if ( IsValidDate( item.Created ) )
					index.Created = item.Created;

				if ( IsValidDate( item.LastUpdated ) )
					index.LastUpdated = item.LastUpdated;
				//
				if ( item.Occupations != null && item.Occupations.Count > 0 )
					index.OccupationResults = Fill_CodeItemResults( item.Occupations.Where( x => x.CategoryId == 11 ).ToList(), CodesManager.PROPERTY_CATEGORY_SOC, false, false );
				if ( item.Industries != null && item.Industries.Count > 0 )
					index.IndustryResults = Fill_CodeItemResults( item.Industries.Where( x => x.CategoryId == 10 ).ToList(), CodesManager.PROPERTY_CATEGORY_NAICS, false, false );
				//
				index.Subjects = item.SubjectAreas;
				index.Keyword = item.Keyword;
				list.Add( index );
			}

			return list;
		}
        #endregion

        #region PathwaySet Elastic Index
        public static List<PathwayIndex> PathwaySet_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
        {
            string connectionString = DBConnectionRO();
			var index = new PathwayIndex();
			var list = new List<PathwayIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "GetAllForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[PathwaySet.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
                        pTotalRows = Int32.Parse( rows );
                    }
					catch ( Exception ex )
					{
						index = new PathwayIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " PathwaySet loading record: {0}", cntr ) );

						index = new PathwayIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_PATHWAY_SET;
						index.EntityType = "PathwaySet";
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );

						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//add helpers
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						//pathways
						string pathways = GetRowColumn( dr, "HasPathways", string.Empty );
						if ( !string.IsNullOrWhiteSpace( pathways ) )
						{
							index.Pathways = new List<IndexEntityReference>();
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( pathways );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var eref = new IndexEntityReference
								{
									EntityType="Pathway", 
									EntityTypeId=8,
									EntityBaseId = int.Parse( child.Attribute( "PathwayId" ).Value ),
									EntityName = ( string )child.Attribute( "Pathway" ) ?? string.Empty
								};
								index.Pathways.Add( eref );
							}//
						}

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.PathwaySet>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "PathwaySet_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        //relationships
                        HandleAgentRelationshipsForEntity( dr, index );
						if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
						{

							var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
							if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
							{
								FormatAddressesToElastic( index, orgAddresses, true );
							}
						}
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        #region Custom Reports
                        //int propertyId = 0;
                        //TODO
                        //indicator of in registry
                        //if ( !string.IsNullOrWhiteSpace( index.CredentialRegistryId ) )
                        //{
                        //	if ( GetPropertyId( 69, "cfReport:IsInRegistry", ref propertyId ) )
                        //		index.ReportFilters.Add( propertyId );
                        //}
                        //else if ( GetPropertyId( 69, "cfReport:IsNotInRegistry", ref propertyId ) )
                        //	index.ReportFilters.Add( propertyId );

                        #endregion

                        list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "PathwaySet_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "PathwaySet_SearchForElastic - Complete loaded {0} records", cntr ) );
				}


				return list;
			}
		}


		public static List<MC.PathwaySetSummary> PathwaySet_MapFromElastic( List<GenericIndex> PathwaySets )
		{
			var list = new List<MC.PathwaySetSummary>();

			foreach ( var oi in PathwaySets )
			{
				var index = new MC.PathwaySetSummary
				{
					Id = oi.Id,
					Name = oi.Name,
					FriendlyName = oi.FriendlyName,
					RowId = oi.RowId,
					Description = oi.Description,
					CTID = oi.CTID,
					SubjectWebpage = oi.SubjectWebpage,
					//OrganizationCTID = oi.OwnerOrganizationCTID,
					//OrganizationId = oi.OwnerOrganizationId,
					//OrganizationName = oi.PrimaryOrganizationName,
					//FrameworkUri = oi.FrameworkUri,
					//CredentialRegistryId = oi.CredentialRegistryId,
					EntityStateId = oi.EntityStateId,
					//DateEffective = ci.DateEffective,
					Created = oi.Created,
					LastUpdated = oi.LastUpdated,
				};


				//TBD for format
				//index. = oi.PathwaySetGraph;

				if ( IsValidDate( oi.Created ) )
					index.Created = oi.Created;

				if ( IsValidDate( oi.LastUpdated ) )
					index.LastUpdated = oi.LastUpdated;



				list.Add( index );
			}

			return list;
		}
		#endregion

		#region Common Index Entities 
		#region Collection Search
		public static List<ManyInOneIndex> Collection_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Collection_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Collection.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Collection loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_COLLECTION;
						index.EntityType = "Collection";
						index.Id = GetRowColumn( dr, "Id", 0 );
						//Need EntityId as the PK for elastic
						index.EntityId = GetRowColumn( dr, "EntityId", 0);
						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						//needs to be a list. Just store first one
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
						if ( !string.IsNullOrWhiteSpace( index.SubjectWebpage ) )
						{
							//do a split
							string[] swps = index.SubjectWebpage.Split( '|' );
							foreach ( var item in swps )
							{
								index.SubjectWebpage = item.Trim();
								break;
							}
						}

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//
						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();
						//add helpers
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//
						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						#region Properties
						try
						{
							var properties = GetRowColumn( dr, "CollectionProperties", string.Empty );
							if ( !string.IsNullOrEmpty( properties ) )
							{
								properties = properties.Replace( "&", " " );
								var xDoc = XDocument.Parse( properties );
								foreach ( var child in xDoc.Root.Elements() )
								{
									var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
									var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
									var property = child.Attribute( "Property" ).Value;
									var schemaName = ( string ) child.Attribute( "PropertySchemaName" );

									index.Properties.Add( new IndexProperty
									{
										CategoryId = categoryId,
										Id = propertyValueId,
										Name = property,
										SchemaName = schemaName
									} );
									if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_COLLECTION_CATEGORY )
										index.CollectionCategoryTypeIds.Add( propertyValueId );


								}
							}

							//get first 'n' collection members
							var collectionGraph = GetRowColumn( dr, "CollectionGraph", string.Empty );
							if (!string.IsNullOrEmpty( collectionGraph ) )
							{

							}
						}
						catch ( Exception ex )
						{
							LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on CollectionProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
						}
                        #endregion
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        //counts


                        //temp - included list of for and from credentials - will like use the counts and ajax gets
                        //var credentialsFor = GetRowColumn( dr, "CredentialsFor", string.Empty );
                        //var credentialsFrom = GetRowColumn( dr, "CredentialsFrom", string.Empty );

                        if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
						{
							//not sure we need addresses?
							var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
							if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
							{
								FormatAddressesToElastic( index, orgAddresses, true );
							}
						}
						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

                        #endregion
                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.Collection>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );

                                HandleResourceHasSupportService( index, resource.HasSupportService );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Collection_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        #region Custom Reports
                        var reportCategoryId = 71;
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Collection_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Collection_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}

		//NOT USED SEE GENERAL
		//public static List<ManyInOneIndex> Collection_MapFromElastic( List<ManyInOneIndex> Collections, int pageNbr, int pageSize )
		//{
		//	var list = new List<ManyInOneIndex>();

		//	return list;
		//}
		#endregion

		/// <summary>
		/// ConceptScheme
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="pageSize"></param>
		/// <param name="pageNumber"></param>
		/// <param name="pTotalRows"></param>
		/// <returns></returns>
		public static List<ManyInOneIndex> ConceptScheme_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( BaseFactory.appMethodEntryTraceLevel, "ConceptScheme_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[ConceptScheme_Search]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " ConceptScheme loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = GetRowColumn( dr, "EntityTypeId", 11 );
						index.EntityType = "Concept Scheme";
						if ( index.EntityTypeId == CodesManager.ENTITY_TYPE_PROGRESSION_MODEL)
						{
							index.EntityType = "Progression Model";
						}
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );

						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						//index.Source = GetRowColumn( dr, "Source", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						/* there is not owning org, etc?		*/
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
				

						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
                        //define LastUpdated to be EntityLastUpdated
                        //date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
                        //if ( IsValidDate( date ) )
                        //	index.LastUpdated = DateTime.Parse( date );
                        //

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.ConceptScheme>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );

                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "ConceptScheme_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

                        #region AgentRelationshipsForEntity
                        //do we need these
                        //HandleAgentRelationshipsForEntity( dr, index );

                        #endregion

                        #region Custom Reports
                        var reportCategoryId = 95;//??
												  //TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "ConceptScheme_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "ConceptScheme_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}
        /// <summary>
        /// progression model
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pTotalRows"></param>
        /// <returns></returns>
        public static List<ManyInOneIndex> ProgressionModel_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
        {
            string connectionString = DBConnectionRO();

            var index = new ManyInOneIndex();
            var list = new List<ManyInOneIndex>();
            var result = new DataTable();
            LoggingHelper.DoTrace( BaseFactory.appMethodEntryTraceLevel, "ProgressionModel_SearchForElastic - Starting. filter\r\n " + filter );
            int cntr = 0;
            using (SqlConnection c = new SqlConnection( connectionString ))
            {
                c.Open();

                using (SqlCommand command = new SqlCommand( "[ProgressionModel_Search]", c ))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    command.CommandTimeout = 300;

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    try
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter())
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill( result );
                        }
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                        pTotalRows = Int32.Parse( rows );

                    }
                    catch (Exception ex)
                    {
                        index = new ManyInOneIndex();
                        index.Name = "EXCEPTION ENCOUNTERED";
                        index.Description = ex.Message;
                        list.Add( index );

                        return list;
                    }
                }
                try
                {
                    foreach (DataRow dr in result.Rows)
                    {
                        cntr++;
                        if (cntr % 100 == 0)
                            LoggingHelper.DoTrace( 2, string.Format( " ProgressionModel loading record: {0}", cntr ) );
                        if (cntr == 450)
                        {

                        }

                        index = new ManyInOneIndex();
                        index.EntityTypeId = CodesManager.ENTITY_TYPE_PROGRESSION_MODEL;
                        index.EntityType = "Progression Model";
                        index.Id = GetRowColumn( dr, "Id", 0 );
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
                        //index.NameIndex = cntr * 1000;
                        index.Name = GetRowColumn( dr, "Name", "missing" );
                        //index.Source = GetRowColumn( dr, "Source", string.Empty );

                        string rowId = GetRowColumn( dr, "RowId" );
                        if (IsValidGuid( rowId ))
                            index.RowId = new Guid( rowId );

                        index.Description = GetRowColumn( dr, "Description", string.Empty );
                        index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        /* there is not owning org, etc?		*/
                        index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
                        index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
                        index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
                        //used for autocomplete and phrase prefix queries
                        index.PrimaryOrganizationName = index.PrimaryOrganizationName;
                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if (index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1)
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }


                        string date = GetRowColumn( dr, "Created", string.Empty );
                        if (IsValidDate( date ))
                            index.Created = DateTime.Parse( date );
                        date = GetRowColumn( dr, "LastUpdated", string.Empty );
                        if (IsValidDate( date ))
                            index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						//counts

						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.ProgressionModel>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "ProgressionModel_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						#region AgentRelationshipsForEntity
						//do we need these
						//HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						#region Custom Reports
						var reportCategoryId = 95;//??
                                                  //TODO


                        #endregion

                        list.Add( index );
                    }
                }
                catch (Exception ex)
                {
                    LoggingHelper.DoTrace( 2, string.Format( "ProgressionModel_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                }
                finally
                {
                    LoggingHelper.DoTrace( 2, string.Format( "ProgressionModel_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
                }


                return list;
            }
        }
		#region CredentialingAction Elastic Index 
		public static List<ManyInOneIndex> CredentialingAction_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "CredentialingAction_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[CredentialingAction.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );
						return list;
					}
				}
				int costProfilesCount = 0;
				int conditionProfilesCount = 0;
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

						index = new ManyInOneIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityTypeId = CodesManager.ENTITY_TYPE_CREDENTIALING_ACTION;
						//or actual short URI?
						index.EntityType = dr["ActionType"].ToString();
						index.CTDLType = GetRowColumn( dr, "ActionType" );
						//

						//Need EntityId as the PK for elastic
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );
						index.Name = dr["Name"].ToString();
						//if no name, how to construct one?
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = dr["Description"].ToString();
						string rowId = dr["RowId"].ToString();
						index.RowId = new Guid( rowId );

						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

						index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "PrimaryOrganizationName", string.Empty );
						index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "PrimaryOrganizationId", 0 );
						index.PrimaryOrganizationCTID = dr["PrimaryOrganizationCtid"].ToString();
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}


						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.CredentialingAction>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "CredentialingAction_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						//don't really need these dates?
						var date = GetRowColumn( dr, "StartDate", string.Empty );
						if ( IsValidDate( date ) )
							index.StartDate = ( DateTime.Parse( date ).ToString( "yyyy-MM-dd" ) );
						else
							index.StartDate = string.Empty;
						//
						date = GetRowColumn( dr, "EndDate", string.Empty );
						if ( IsValidDate( date ) )
							index.EndDate = date;
						else
							index.EndDate = string.Empty;

						date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated => no should be registry last updated which is the base lastupdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						//don't thinks this is necessary!
						//	NO - should be adding filters where concepts exist.
						//	BUT can it be made general (to reuse for different status types) - maybe replace life cycle with a generic internal status property
						//index.ActionStatusTypeId = GetRowColumn( dr, "ActionStatusTypeId", 0 );
						//index.ActionStatusType = dr["ActionStatusType"].ToString();
						//Huh what is this. May be a general property to use for instrument and other credentials
						index.AboutCredentialList = GetRowColumn( dr, "Instrument", string.Empty );

						#region AgentRelationshipsForEntity
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion
						#region TextValues

						//properties to add to textvalues
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );


						//

						//if ( index.CommonConditionsCount > 0 )
						//	if ( GetPropertyId( supportServicePropertyCategory, "supportSrvReport:ReferencesCommonConditions", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );


						list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "CredentialingAction_SearchForElastic. Last Row: {0}, asmtId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
					var index2 = new ManyInOneIndex();
					index2.Name = "EXCEPTION ENCOUNTERED";
					index2.Description = ex.Message;
					list.Add( index2 );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "CredentialingAction_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}
				return list;
			}
		}
		#endregion
		#region OutcomeData Elastic Index 
		public static List<ManyInOneIndex> OutcomeData_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();
			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "OutcomeData_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();
				using ( SqlCommand command = new SqlCommand( "[DataSetProfile.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );
						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

						index = new ManyInOneIndex();
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityTypeId = CodesManager.ENTITY_TYPE_DATASET_PROFILE;
						index.EntityType = "DataSetProfile";
						//Need EntityId as the PK for elastic
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );
						index.Name = dr[ "Name" ].ToString();
						Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
						index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

						index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
						index.FriendlyName = FormatFriendlyTitle( index.Name );
						index.Description = dr[ "Description" ].ToString();

						if ( string.IsNullOrWhiteSpace( index.Name ) )
						{
							if ( index.Description?.Length < 250 )
							{
								index.Name = index.Description;
							}
							if ( !string.IsNullOrWhiteSpace( index.Description ) )
							{
								index.Name = index.Description?.Length < PortionOfDescriptionToUseForName ? index.Description : index.Description.Substring( 0, PortionOfDescriptionToUseForName ) + " ...";
							}
							else
							{
								index.Name = "Outcome Data";
							}
						}
						string rowId = dr[ "RowId" ].ToString();
						index.RowId = new Guid( rowId );

						index.SubjectWebpage = GetRowColumn( dr, "Source", string.Empty );

						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "DataProviderName", string.Empty );
						index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "DataProviderId", 0 );
						index.PrimaryOrganizationCTID = dr[ "DataProviderCTID" ].ToString();

						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						index.AboutCredentialList = GetRowColumn( dr, "AboutCredentials", string.Empty );
						index.AboutLoppList = GetRowColumn( dr, "AboutLearningOpportunities", string.Empty );

						var dataSetTimePeriod = GetRowColumn( dr, "DataSetTimePeriodJson", string.Empty );
						//try
						//{
						//	//24-04-01 mp - commenting out as causes exception and not in use

						//	if ( !string.IsNullOrWhiteSpace( dataSetTimePeriod ) )
						//	{
						//		var dataSetTimePeriodList = JsonConvert.DeserializeObject<List<DataSetTimeFrame>>( dataSetTimePeriod );
						//		//TODO - what to extact for use in elastic queries?
						//		//may just add as a blob and unpack in search results. First consider what might be in ResourceDetail and maybe use use the latter? 
						//		index.DataSetTimePeriods = JObject.FromObject( dataSetTimePeriodList );
						//	}
						//}
						//catch (Exception ex) 
						//{
						//}
						//

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.DataSetProfile>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "OutcomeData_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

						//don't really need these dates?
						var date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated => no should be registry last updated which is the base lastupdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						#region AgentRelationshipsForEntity
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion
						#region TextValues
						
						//properties to add to textvalues
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );
						#endregion



						#region Widgets, collections
						//???
						HandleWidgetSelections( index, dr, "OutcomeDataFilters" );

						#endregion
						#region custom reports
						int propertyId = 0;
						//indicator of in registry 
						//how to make this more generic.
						//Start with reusing asmt?
						var OutcomeDataPropertyCategory = 60; //for now

						//always in registry, no blank nodes at this time!
						//                 if ( !string.IsNullOrWhiteSpace( index.CTID ) )
						//                 {
						////perhaps schema name can just be generic for elastic!
						//                     if ( GetPropertyId( OutcomeDataPropertyCategory, "asmtReport:IsInRegistry", ref propertyId ) )
						//                         index.ReportFilters.Add( propertyId );
						//                 }
						//                 else if ( GetPropertyId( OutcomeDataPropertyCategory, "asmtReport:IsNotInRegistry", ref propertyId ) )
						//                     index.ReportFilters.Add( propertyId );
						//


						//if ( index.Addresses.Count > 0 )
						//	if ( GetPropertyId( OutcomeDataPropertyCategory, "supportSrvReport:HasAddresses", ref propertyId ) )
						//		index.ReportFilters.Add( propertyId );

						list.Add( index );
					}
					#endregion

				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "OutcomeData_SearchForElastic. Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "OutcomeData_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}
				return list;
			}
		}



		#endregion

		#region Occupation/Job/Task/WorkRole Elastic Index
		public static List<ManyInOneIndex> Occupation_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Occupation_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Occupation.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Occupation loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_OCCUPATIONS_PROFILE;
						index.EntityType = "OccupationProfile";
						index.Id = GetRowColumn( dr, "Id", 0 );
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						/* there is not owning org, etc?	*/
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "PrimaryOrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "PrimaryOrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "PrimaryOrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
					
						
						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						index.LifeCycleStatusTypeId = GetRowPossibleColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = GetRowPossibleColumn( dr, "LifeCycleStatusType", string.Empty );
						//counts


                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.Occupation>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Occupation_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

                        #region AgentRelationshipsForEntity
                        HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						#region TransferValues
						string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
						if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
						{
							if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
							{
								providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
							}
							providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( providesTransferValueFor );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexProperty
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Id = ( int ) child.Attribute( "ResourceId" )
								};

								index.ProvidesTransferValueFor.Add( competency );
							}
						}
						string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
						if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
						{
							if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
							{
								receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
							}
							receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( receivesTransferValueFor );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexProperty
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Id = ( int ) child.Attribute( "ResourceId" )
								};

								index.ReceivesTransferValueFrom.Add( competency );
							}
						}
						#endregion

						string competencies = dr["Competencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( competencies ) )
						{
							if ( ContainsUnicodeCharacter( competencies ) )
							{
								competencies = Regex.Replace( competencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							competencies = competencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( competencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.Competencies.Add( competency );

							}
						}

						#region Custom Reports
						AddReportProperty( index, index.Competencies.Count(), 58, "occupationReport:HasCompetencies" );
						//var reportCategoryId = 95;//??
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Occupation_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Occupation_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}

		public static List<ManyInOneIndex> Job_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Job_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Job.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Job loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_JOB_PROFILE;
						index.EntityType = "JobProfile";
						index.Id = GetRowColumn( dr, "Id", 0 );
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        /* there is not owning org, etc?	*/
                        index.PrimaryOrganizationCTID = GetRowColumn( dr, "PrimaryOrganizationCTID", string.Empty );
                        index.PrimaryOrganizationId = GetRowColumn( dr, "PrimaryOrganizationId", 0 );
                        index.PrimaryOrganizationName = GetRowColumn( dr, "PrimaryOrganizationName", string.Empty );
                        //used for autocomplete and phrase prefix queries
                        index.PrimaryOrganizationName = index.PrimaryOrganizationName;
                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }
                        string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						
						index.LifeCycleStatusTypeId = GetRowPossibleColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType =  GetRowPossibleColumn( dr, "LifeCycleStatusType", string.Empty );
						//counts

						try
						{
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.Job>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );

                                HandleResourceHasSupportService( index, resource.HasSupportService );

                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Job_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

                        #region AgentRelationshipsForEntity
                        //do we need these - yes for offered by 
                        HandleAgentRelationshipsForEntity( dr, index );

						#endregion
						#region TransferValues
						string providesTransferValueFor = dr["ProvidesTransferValueFor"].ToString();
						if ( !string.IsNullOrWhiteSpace( providesTransferValueFor ) )
						{
							if ( ContainsUnicodeCharacter( providesTransferValueFor ) )
							{
								providesTransferValueFor = Regex.Replace( providesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
							}
							providesTransferValueFor = providesTransferValueFor.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( providesTransferValueFor );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexProperty
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Id = ( int ) child.Attribute( "ResourceId" )
								};

								index.ProvidesTransferValueFor.Add( competency );
							}
						}
						string receivesTransferValueFor = dr["ReceivesTransferValueFrom"].ToString();
						if ( !string.IsNullOrWhiteSpace( receivesTransferValueFor ) )
						{
							if ( ContainsUnicodeCharacter( receivesTransferValueFor ) )
							{
								receivesTransferValueFor = Regex.Replace( receivesTransferValueFor, @"[^\u0000-\u007F]+", string.Empty );
							}
							receivesTransferValueFor = receivesTransferValueFor.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( receivesTransferValueFor );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexProperty
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Id = ( int ) child.Attribute( "ResourceId" )
								};

								index.ReceivesTransferValueFrom.Add( competency );
							}
						}
						#endregion

						string competencies = dr["Competencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( competencies ) )
						{
							if ( ContainsUnicodeCharacter( competencies ) )
							{
								competencies = Regex.Replace( competencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							competencies = competencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( competencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.Competencies.Add( competency );

							}
						}

						#region Custom Reports
						AddReportProperty( index, index.Competencies.Count(), 58, "jobReport:HasCompetencies" );
						//??var reportCategoryId = ??;
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Job_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Job_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}
		public static List<ManyInOneIndex> Task_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Task_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Task.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Task loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_TASK_PROFILE;
						index.EntityType = "TaskProfile";
						index.Id = GetRowColumn( dr, "Id", 0 );
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );

						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
				

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );

						if ( string.IsNullOrWhiteSpace( index.Name ) )
						{
							if ( !string.IsNullOrWhiteSpace( index.Description ) )
							{
								index.Name = index.Description?.Length < PortionOfDescriptionToUseForName ? index.Description : index.Description.Substring( 0, PortionOfDescriptionToUseForName ) + " ...";
							}
							else
							{
								index.Name = "Task";
							}
						}

						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        /* there is not owning org, etc?	*/
                        index.PrimaryOrganizationCTID = GetRowColumn( dr, "PrimaryOrganizationCTID", string.Empty );
                        index.PrimaryOrganizationId = GetRowColumn( dr, "PrimaryOrganizationId", 0 );
                        index.PrimaryOrganizationName = GetRowColumn( dr, "PrimaryOrganizationName", string.Empty );
                        //used for autocomplete and phrase prefix queries
                        index.PrimaryOrganizationName = index.PrimaryOrganizationName;
                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }

                        string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						index.LifeCycleStatusTypeId = GetRowPossibleColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = GetRowPossibleColumn( dr, "LifeCycleStatusType", string.Empty );
						//counts

						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.Task>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Task_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        #region AgentRelationshipsForEntity
                        //do we need these
                        HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						string competencies = dr["Competencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( competencies ) )
						{
							if ( ContainsUnicodeCharacter( competencies ) )
							{
								competencies = Regex.Replace( competencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							competencies = competencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( competencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.Competencies.Add( competency );

							}
						}

						#region Custom Reports
						AddReportProperty( index, index.Competencies.Count(), 58, "taskReport:HasCompetencies" );
						//??var reportCategoryId = ??;
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Task_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Task_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}

		public static List<ManyInOneIndex> WorkRole_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "WorkRole_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[WorkRole.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " WorkRole loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_WORKROLE_PROFILE;
						index.EntityType = "WorkRoleProfile";
						index.Id = GetRowColumn( dr, "Id", 0 );
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
					

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        /* there is not owning org, etc?	*/
                        index.PrimaryOrganizationCTID = GetRowColumn( dr, "PrimaryOrganizationCTID", string.Empty );
                        index.PrimaryOrganizationId = GetRowColumn( dr, "PrimaryOrganizationId", 0 );
                        index.PrimaryOrganizationName = GetRowColumn( dr, "PrimaryOrganizationName", string.Empty );
                        //used for autocomplete and phrase prefix queries
                        index.PrimaryOrganizationName = index.PrimaryOrganizationName;
                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }
                        string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
                        //define LastUpdated to be EntityLastUpdated
                        //date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
                        //if ( IsValidDate( date ) )
                        //	index.LastUpdated = DateTime.Parse( date );

                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.WorkRole>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "WorkRole_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						index.LifeCycleStatusTypeId = GetRowPossibleColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = GetRowPossibleColumn( dr, "LifeCycleStatusType", string.Empty );
						//counts

						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						string competencies = dr["Competencies"].ToString();
						if ( !string.IsNullOrWhiteSpace( competencies ) )
						{
							if ( ContainsUnicodeCharacter( competencies ) )
							{
								competencies = Regex.Replace( competencies, @"[^\u0000-\u007F]+", string.Empty );
							}
							competencies = competencies.Replace( "&", " " );
							var xDoc = new XDocument();
							xDoc = XDocument.Parse( competencies );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var competency = new IndexCompetency
								{
									Name = ( string ) child.Attribute( "Name" ) ?? string.Empty,
									Description = ( string ) child.Attribute( "Description" ) ?? string.Empty
								};

								index.Competencies.Add( competency );

							}
						}

						#region Custom Reports
						AddReportProperty( index, index.Competencies.Count(), 58, "workroleReport:HasCompetencies" );
						//??var reportCategoryId = ??;
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "WorkRole_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "WorkRole_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}
		#endregion

		#region TransferValue/Transfer Intermediary Elastic Index
		public static List<ManyInOneIndex> TransferValue_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "TransferValue_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[TransferValue.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " TransferValue loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_TRANSFER_VALUE_PROFILE;
						index.EntityType = "TransferValueProfile";
						index.Id = GetRowColumn( dr, "Id", 0 );
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.TransferValueProfile>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "TransferValue_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						//add helpers
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//
						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
						index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();
						//index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );

						date = GetRowColumn( dr, "StartDate", string.Empty );
						if ( IsValidDate( date ) )
							index.StartDate = date;
						else
							index.StartDate = null;

						//expirationDate
						date = GetRowColumn( dr, "EndDate", string.Empty );
						if ( IsValidDate( date ) )
							index.EndDate = date;
						else
							index.EndDate = null;

						//TransferIntermediariesFor
						var transferIntermediariesFor = GetRowColumn( dr, "TransferIntermediariesFor" );
						if ( !string.IsNullOrWhiteSpace( transferIntermediariesFor ) )
						{
							var xDoc = XDocument.Parse( transferIntermediariesFor );
							foreach ( var child in xDoc.Root.Elements() )
							{
								var id = int.Parse( child.Attribute( "TransferIntermediaryId" ).Value );
								if ( id > 0 )
									index.TransferIntermediariesFor.Add( id );
							}//
						}

						#region transfer value from/for
						//
						var transferValueFromJson = GetRowColumn( dr, "TransferValueFromJson" );
						if ( !string.IsNullOrWhiteSpace( transferValueFromJson ) )
						{
							var transferValueFrom = JsonConvert.DeserializeObject<List<MC.TopLevelObject>>( transferValueFromJson );
							index.TransferValueFrom = new List<IndexEntityReference>();
							foreach ( var item in transferValueFrom )
							{
								index.TransferValueFrom.Add( new IndexEntityReference()
								{
									EntityName = item.Name,
									EntityBaseId = item.Id,
									EntityTypeId = item.EntityTypeId,
									EntityType = item.EntityType,
									EntityStateId = item.EntityStateId,
									OrganizationName = item.OrganizationName,
									OrganizationId = item.PrimaryOrganizationId,
								} );
							}
						}

						var transferValueForJson = GetRowColumn( dr, "TransferValueForJson" );
						if ( !string.IsNullOrWhiteSpace( transferValueForJson ) )
						{
							var transferValueFor = JsonConvert.DeserializeObject<List<MC.TopLevelObject>>( transferValueForJson );
							index.TransferValueFor = new List<IndexEntityReference>();
							foreach (var item in transferValueFor )
							{
								index.TransferValueFor.Add( new IndexEntityReference()
								{
									EntityName = item.Name,
									EntityBaseId = item.Id,
									EntityTypeId = item.EntityTypeId,
									EntityType = item.EntityType,			//might be safer to use resources for the link? No would need a CTID (or GUID?)
									EntityStateId = item.EntityStateId,
									OrganizationName = item.OrganizationName,
									OrganizationId = item.PrimaryOrganizationId,
								} );
							}
						}

						//counts
						index.TransferValueForCredentialsCount = GetRowColumn( dr, "TransferValueForCredentialsCount", 0 );
						index.TransferValueFromCredentialsCount = GetRowColumn( dr, "TransferValueFromCredentialsCount", 0 );

						index.TransferValueForAssessmentsCount = GetRowColumn( dr, "TransferValueForAssessmentsCount", 0 );
						index.TransferValueFromAssessmentsCount = GetRowColumn( dr, "TransferValueFromAssessmentsCount", 0 );

						index.TransferValueForLoppsCount = GetRowColumn( dr, "TransferValueForLoppsCount", 0 );
						index.TransferValueFromLoppsCount = GetRowColumn( dr, "TransferValueFromLoppsCount", 0 );
						#endregion
						//
						index.TransferValueHasDevProcessCount = GetRowColumn( dr, "TransferValueHasDevProcessCount", 0 );
						


						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );

						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						#region Custom Reports
						var reportCategoryId = 71;
						//TODO

						AddReportProperty( index, index.TransferValueForCredentialsCount, reportCategoryId, "Has Transfer Value For Credentials", "tvpReport:HasTransferValueForCredentials" );
						AddReportProperty( index, index.TransferValueFromCredentialsCount, reportCategoryId, "Has Transfer Value From Credentials", "tvpReport:HasTransferValueFromCredentials" );
						//
						AddReportProperty( index, index.TransferValueForAssessmentsCount, reportCategoryId, "Has Transfer Value For Assessments", "tvpReport:HasTransferValueForAssessments" ); 
						AddReportProperty( index, index.TransferValueFromAssessmentsCount, reportCategoryId, "Has Transfer Value From Assessments", "tvpReport:HasTransferValueFromAssessments" );
						//
						AddReportProperty( index, index.TransferValueForLoppsCount, reportCategoryId, "Has Transfer Value For Learning Opportunities", "tvpReport:HasTransferValueForLopps" ); 
						AddReportProperty( index, index.TransferValueFromLoppsCount, reportCategoryId, "Has Transfer Value From Learning Opportunities", "tvpReport:HasTransferValueFromLopps" );
						//
						AddReportProperty( index, index.TransferValueHasDevProcessCount, reportCategoryId, "Has Transfer Value Development Process Profiles", "tvpReport:TransferValueHasDevProcess" ); 
						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "TransferValue_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "TransferValue_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}

        public static List<ManyInOneIndex> TransferIntermediary_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
        {
            string connectionString = DBConnectionRO();

            var index = new ManyInOneIndex();
            var list = new List<ManyInOneIndex>();
            var result = new DataTable();
            LoggingHelper.DoTrace( 2, "TransferIntermediary_SearchForElastic - Starting. filter\r\n " + filter );
            int cntr = 0;
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();

                using ( SqlCommand command = new SqlCommand( "[TransferIntermediary.ElasticSearch]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    command.CommandTimeout = 300;

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    try
                    {
                        using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill( result );
                        }
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                        pTotalRows = Int32.Parse( rows );

                    }
                    catch ( Exception ex )
                    {
                        index = new ManyInOneIndex();
                        index.Name = "EXCEPTION ENCOUNTERED";
                        index.Description = ex.Message;
                        list.Add( index );

                        return list;
                    }
                }
                try
                {
                    foreach ( DataRow dr in result.Rows )
                    {
                        cntr++;
                        if ( cntr % 100 == 0 )
                            LoggingHelper.DoTrace( 2, string.Format( " TransferIntermediary loading record: {0}", cntr ) );
                        if ( cntr == 450 )
                        {

                        }

                        index = new ManyInOneIndex();
                        index.EntityTypeId = 28;
                        index.EntityType = "TransferIntermediary";
                        index.Id = GetRowColumn( dr, "Id", 0 );
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );

                        index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
                        //index.NameIndex = cntr * 1000;
                        index.Name = GetRowColumn( dr, "Name", "missing" );
                        index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

                        string rowId = GetRowColumn( dr, "RowId" );
                        if ( IsValidGuid( rowId ) )
                            index.RowId = new Guid( rowId );

                        index.Description = GetRowColumn( dr, "Description", string.Empty );
                        index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );

						//add helpers
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "OrganizationCTID", string.Empty );
                        index.PrimaryOrganizationId = GetRowColumn( dr, "OrganizationId", 0 );
                        index.PrimaryOrganizationName = GetRowColumn( dr, "OrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						//

						string date = GetRowColumn( dr, "Created", string.Empty );
                        if ( IsValidDate( date ) )
                            index.Created = DateTime.Parse( date );
                        date = GetRowColumn( dr, "LastUpdated", string.Empty );
                        if ( IsValidDate( date ) )
                            index.LastUpdated = DateTime.Parse( date );
                        //define LastUpdated to be EntityLastUpdated
                        //date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
                        //if ( IsValidDate( date ) )
                        //	index.LastUpdated = DateTime.Parse( date );
                        //IntermediaryForJson
                        var intermediaryForJson = GetRowColumn( dr, "IntermediaryForJson" );
                        if ( !string.IsNullOrWhiteSpace( intermediaryForJson ) )
                        {
                            //var xDoc = XDocument.Parse( intermediaryForJson );
                            // foreach ( var child in xDoc.Root.Elements() )
                            // {
                                // var id = int.Parse( child.Attribute( "TransferIntermediaryId" ).Value );
                                // if ( id > 0 )
                                    // index.TransferIntermediariesFor.Add( id );
                            // }//
                        }
						//counts

						index.HasTransferValueProfiles = GetRowColumn( dr, "HasTransferValueProfiles", 0 );
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );

						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.TransferIntermediary>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "TransferIntermediary_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

                        #endregion

                        #region Custom Reports
                        var reportCategoryId = 72;
                        //TODO

                        //AddReportProperty( index, index.TransferValueForCredentialsCount, reportCategoryId, "Has Transfer Value For Credentials", "tvpReport:HasTransferValueForCredentials" );
                        #endregion

                        list.Add( index );
                    }
                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 2, string.Format( "TransferIntermediary_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                }
                finally
                {
                    LoggingHelper.DoTrace( 2, string.Format( "TransferIntermediary_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
                }


                return list;
            }
        }
        #endregion

        #region SupportService Elastic Index 
        public static List<ManyInOneIndex> SupportService_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
        {
            string connectionString = DBConnectionRO();
            var index = new ManyInOneIndex();
            var list = new List<ManyInOneIndex>();
            var result = new DataTable();
			var thisEntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
			LoggingHelper.DoTrace( 2, "SupportService_SearchForElastic - Starting. filter\r\n " + filter );
            int cntr = 0;
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();
                using ( SqlCommand command = new SqlCommand( "[SupportService.ElasticSearch]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    command.CommandTimeout = 300;

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    try
                    {
                        using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill( result );
                        }
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                        pTotalRows = Int32.Parse( rows );

                    }
                    catch ( Exception ex )
                    {
                        index = new ManyInOneIndex();
                        index.Name = "EXCEPTION ENCOUNTERED";
                        index.Description = ex.Message;
                        list.Add( index );
                        return list;
                    }
                }
                int costProfilesCount = 0;
                int conditionProfilesCount = 0;
                try
                {
                    foreach ( DataRow dr in result.Rows )
                    {
                        cntr++;
                        if ( cntr % 100 == 0 )
                            LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

                        index = new ManyInOneIndex();
                        index.Id = GetRowColumn( dr, "Id", 0 );
                        index.EntityTypeId = CodesManager.ENTITY_TYPE_SUPPORT_SERVICE;
                        index.EntityType = "SupportService";
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );
                        index.Name = dr["Name"].ToString();
                        Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
                        index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

                        index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
                        index.FriendlyName = FormatFriendlyTitle( index.Name );
                        index.Description = dr["Description"].ToString();
                        string rowId = dr["RowId"].ToString();
                        index.RowId = new Guid( rowId );

                        index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

                        index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        //index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

                        index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "OrganizationName", string.Empty );
                        index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "OrganizationId", 0 );

                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }
                        //add helpers
                        index.PrimaryOrganizationCTID = dr["OrganizationCTID"].ToString();
						//index.PrimaryOrganizationId = index.PrimaryOrganizationId;
						//index.PrimaryOrganizationName = index.PrimaryOrganizationName;

                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.SupportService>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "SupportService_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        //don't really need these dates?
                        var date = GetRowColumn( dr, "DateEffective", string.Empty );
                        if ( IsValidDate( date ) )
                            index.DateEffective = ( DateTime.Parse( date ).ToString( "yyyy-MM-dd" ) );
                        else
                            index.DateEffective = string.Empty;

						//expirationDate
						date = GetRowColumn( dr, "ExpirationDate", string.Empty );
						if ( IsValidDate( date ) )
							index.ExpirationDate = date;
						else
							index.ExpirationDate = string.Empty;

						date = GetRowColumn( dr, "Created", string.Empty );
                        if ( IsValidDate( date ) )
                            index.Created = DateTime.Parse( date );
                        date = GetRowColumn( dr, "LastUpdated", string.Empty );
                        if ( IsValidDate( date ) )
                            index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated => no should be registry last updated which is the base lastupdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );

						//don't thinks this is necessary!
						//index.QARolesCount = GetRowColumn( dr, "QARolesCount", 0 );
						index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
                        index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();
                        //not clear whether need a count for supportServiceCondition profiles?
                        //index.SupportServiceConditionCount = GetRowColumn( dr, "supportServiceConditionCount", 0 );
						conditionProfilesCount = GetRowPossibleColumn( dr, "SupportServiceConditionCount", 0 );

                        costProfilesCount = index.CostProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );
                        
                        //not sure of value for some of these counts, maybe fin aid
                        index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
                        index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
                        index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );

                        #region AgentRelationshipsForEntity
                        HandleAgentRelationshipsForEntity( dr, index );

                        #endregion
                        #region TextValues
                        var keywords = dr["Keyword"].ToString();
                        if ( !string.IsNullOrWhiteSpace( keywords ) )
                        {
                            index.Keyword = SplitDelimitedStringToList( keywords, '|' );
                        }
                        //properties to add to textvalues
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        //these are not searchable, so skip. actually need AvailableOnlineAt for a report
                        var availableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );
                        //index.AvailabilityListing = GetRowPossibleColumn( dr, "AvailabilityListing", string.Empty );
                        #endregion

                        #region SupportServiceProperties - these are stored to enable use by gray button clicks
                        try
                        {
                            var properties = dr["SupportServiceProperties"].ToString();
                            if ( !string.IsNullOrEmpty( properties ) )
                            {
                                properties = properties.Replace( "&", " " );
                                var xDoc = XDocument.Parse( properties );
                                foreach ( var child in xDoc.Root.Elements() )
                                {
                                    var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
                                    var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
                                    var property = child.Attribute( "Property" ).Value;
                                    var schemaName = ( string ) child.Attribute( "PropertySchemaName" );

                                    index.Properties.Add( new IndexProperty
                                    {
                                        CategoryId = categoryId,
                                        Id = propertyValueId,
                                        Name = property,
                                        SchemaName = schemaName
                                    } );
                                    if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_ACCOMMODATION )
                                        index.AccommodationTypeIds.Add( propertyValueId );
                                    if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_CATEGORY )
                                        index.SupportServiceCategoryIds.Add( propertyValueId );
                                    if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
                                        index.DeliveryMethodTypeIds.Add( propertyValueId );
                                    
                                }
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on SupportServiceProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

                        #endregion

                        #region Addresses
                        var addresses = dr["Addresses"].ToString();
                        if ( !string.IsNullOrWhiteSpace( addresses ) )
                        {
                            FormatAddressesToElastic( index, addresses );

                            var xDoc = new XDocument();

                        }
                        if ( index.Addresses.Count == 0 )
                        {
                            if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
                            {
								//TBD, or maybe specific config for support service
                                //prototype: if no resource addresses, and one org address, then add to index (not detail page)
                                //var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
                                //if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
                                //{
                                //    FormatAddressesToElastic( index, orgAddresses, true );
                                //}
                            }
                        }
                        #endregion

                        #region language
                        index.InLanguage = GetLanguages( dr );
                        #endregion
                        #region Widgets, collections
						//???
                        HandleWidgetSelections( index, dr, "SupportServiceFilters" );

                        #endregion
                        #region custom reports
                        int propertyId = 0;
						//indicator of in registry 
						//how to make this more generic.
						//Start with reusing asmt?
						//if being generic, then can't use the namespace!
						//var supportServicePropertyCategory = CodesManager.PROPERTY_CATEGORY_SUPPORT_SERVICE_REPORT_ITEM;

						//always in registry, no blank nodes at this time!
       //                 if ( !string.IsNullOrWhiteSpace( index.CTID ) )
       //                 {
							////perhaps schema name can just be generic for elastic!
       //                     if ( GetPropertyIdByEntityType( thisEntityTypeId, "asmtReport:IsInRegistry", ref propertyId ) )
       //                         index.ReportFilters.Add( propertyId );
       //                 }
       //                 else if ( GetPropertyIdByEntityType( thisEntityTypeId, "asmtReport:IsNotInRegistry", ref propertyId ) )
       //                     index.ReportFilters.Add( propertyId );
                        //


                        if ( !string.IsNullOrWhiteSpace( availableOnlineAt ) )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:AvailableOnline", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        
                        //
                        if ( costProfilesCount > 0 )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:HasCostProfile", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( conditionProfilesCount > 0 )		//if being generic, then can't use the namespace!
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:HasConditionProfile", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        //

                        if ( index.CommonConditionsCount > 0 )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:ReferencesCommonConditions", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.CommonCostsCount > 0 )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:ReferencesCommonCosts", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.FinancialAidCount > 0 )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:FinancialAid", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.HasOccupations )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:HasOccupations", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.Addresses.Count > 0 )
                            if ( GetPropertyIdByEntityType( thisEntityTypeId, "supportSrvReport:HasAddresses", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );

                        list.Add( index );
                    }
                    #endregion

                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 2, string.Format( "SupportService_SearchForElastic. Last Row: {0}, asmtId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                }
                finally
                {
                    LoggingHelper.DoTrace( 2, string.Format( "SupportService_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
                }
                return list;
            }
        }
        #endregion

        #region ScheduledOffering Elastic Index 
        public static List<ManyInOneIndex> ScheduledOffering_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
        {
            string connectionString = DBConnectionRO();
            var index = new ManyInOneIndex();
            var list = new List<ManyInOneIndex>();
            var result = new DataTable();
			var thisEntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
            LoggingHelper.DoTrace( 2, "ScheduledOffering_SearchForElastic - Starting. filter\r\n " + filter );
            int cntr = 0;
            using ( SqlConnection c = new SqlConnection( connectionString ) )
            {
                c.Open();
                using ( SqlCommand command = new SqlCommand( "[ScheduledOffering.ElasticSearch]", c ) )
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
                    command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
                    command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
                    command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
                    command.CommandTimeout = 300;

                    SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
                    totalRows.Direction = ParameterDirection.Output;
                    command.Parameters.Add( totalRows );

                    try
                    {
                        using ( SqlDataAdapter adapter = new SqlDataAdapter() )
                        {
                            adapter.SelectCommand = command;
                            adapter.Fill( result );
                        }
                        string rows = command.Parameters[command.Parameters.Count - 1].Value.ToString();
                        pTotalRows = Int32.Parse( rows );

                    }
                    catch ( Exception ex )
                    {
                        index = new ManyInOneIndex();
                        index.Name = "EXCEPTION ENCOUNTERED";
                        index.Description = ex.Message;
                        list.Add( index );
                        return list;
                    }
                }
                int costProfilesCount = 0;
                int conditionProfilesCount = 0;
                try
                {
                    foreach ( DataRow dr in result.Rows )
                    {
                        cntr++;
                        if ( cntr % 100 == 0 )
                            LoggingHelper.DoTrace( 2, string.Format( " loading record: {0}", cntr ) );

                        index = new ManyInOneIndex();
                        index.Id = GetRowColumn( dr, "Id", 0 );
                        index.EntityTypeId = CodesManager.ENTITY_TYPE_SCHEDULED_OFFERING;
                        index.EntityType = "ScheduledOffering";
                        //Need EntityId as the PK for elastic
                        index.EntityId = GetRowColumn( dr, "EntityId", 0 );
                        index.Name = dr["Name"].ToString();
                        Regex rgx = new Regex( "[^a-zA-Z0-9 -]" );
                        index.NameAlphanumericOnly = rgx.Replace( index.Name, string.Empty ).Replace( " ", string.Empty ).Replace( "-", string.Empty );

                        index.EntityStateId = GetRowPossibleColumn( dr, "EntityStateId", 0 );
                        index.FriendlyName = FormatFriendlyTitle( index.Name );
                        index.Description = dr["Description"].ToString();
                        string rowId = dr["RowId"].ToString();
                        index.RowId = new Guid( rowId );

                        index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );

                        index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
                        //index.CredentialRegistryId = GetRowPossibleColumn( dr, "CredentialRegistryId", string.Empty );

                        index.PrimaryOrganizationName = GetRowPossibleColumn( dr, "OrganizationName", string.Empty );
                        index.PrimaryOrganizationId = GetRowPossibleColumn( dr, "OrganizationId", 0 );

                        index.NameOrganizationKey = index.Name;
                        index.ListTitle = index.Name;
                        if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
                        {
                            index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
                            //ListTitle is not used anymore
                            index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
                        }
                        //add helpers
                        index.PrimaryOrganizationCTID = dr["OrganizationCTID"].ToString();
                        //index.PrimaryOrganizationId = index.OwnerOrganizationId;
                        //index.PrimaryOrganizationName = index.PrimaryOrganizationName;
                        try
                        {
                            var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
                            if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
                            {
                                var resource = JsonConvert.DeserializeObject<APIM.ScheduledOffering>( resourceDetail );
                                index.ResourceDetail = JObject.FromObject( resource );

                                HandleResourceHasSupportService( index, resource.HasSupportService );
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "ScheduledOffering_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
                        //don't really need these dates?
                        var date = GetRowColumn( dr, "DateEffective", string.Empty );
                        if ( IsValidDate( date ) )
                            index.DateEffective = ( DateTime.Parse( date ).ToString( "yyyy-MM-dd" ) );
                        else
                            index.DateEffective = string.Empty;

                        //expirationDate
                        date = GetRowColumn( dr, "Created", string.Empty );
                        if ( IsValidDate( date ) )
                            index.Created = DateTime.Parse( date );
                        date = GetRowColumn( dr, "LastUpdated", string.Empty );
                        if ( IsValidDate( date ) )
                            index.LastUpdated = DateTime.Parse( date );
                        //define LastUpdated to be EntityLastUpdated => no should be registry last updated which is the base lastupdated
                        //date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
                        //if ( IsValidDate( date ) )
                        //	index.LastUpdated = DateTime.Parse( date );

                        //don't thinks this is necessary!
                        //index.QARolesCount = GetRowColumn( dr, "QARolesCount", 0 );
                        index.LifeCycleStatusTypeId = GetRowColumn( dr, "LifeCycleStatusTypeId", 0 );
                        index.LifeCycleStatusType = dr["LifeCycleStatusType"].ToString();
                        //not clear whether need a count for ScheduledOfferingCondition profiles?
                        //index.ScheduledOfferingConditionCount = GetRowColumn( dr, "ScheduledOfferingConditionCount", 0 );
                        conditionProfilesCount = GetRowPossibleColumn( dr, "ScheduledOfferingConditionCount", 0 );

                        costProfilesCount = index.CostProfilesCount = GetRowPossibleColumn( dr, "CostProfilesCount", 0 );

                        //not sure of value for some of these counts, maybe fin aid
                        index.CommonConditionsCount = GetRowPossibleColumn( dr, "CommonConditionsCount", 0 );
                        index.CommonCostsCount = GetRowPossibleColumn( dr, "CommonCostsCount", 0 );
                        index.FinancialAidCount = GetRowPossibleColumn( dr, "FinancialAidCount", 0 );


                        #region TextValues
                        var keywords = dr["Keyword"].ToString();
                        if ( !string.IsNullOrWhiteSpace( keywords ) )
                        {
                            index.Keyword = SplitDelimitedStringToList( keywords, '|' );
                        }
                        //properties to add to textvalues
                        if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                            index.TextValues.Add( index.CTID );
                        //these are not searchable, so skip. actually need AvailableOnlineAt for a report
                        var availableOnlineAt = GetRowPossibleColumn( dr, "AvailableOnlineAt", string.Empty );
                        //index.AvailabilityListing = GetRowPossibleColumn( dr, "AvailabilityListing", string.Empty );
                        #endregion

                        #region ScheduledOfferingProperties - these are stored to enable use by gray button clicks
                        try
                        {
                            var properties = dr["ScheduledOfferingProperties"].ToString();
                            if ( !string.IsNullOrEmpty( properties ) )
                            {
                                properties = properties.Replace( "&", " " );
                                var xDoc = XDocument.Parse( properties );
                                foreach ( var child in xDoc.Root.Elements() )
                                {
                                    var categoryId = int.Parse( child.Attribute( "CategoryId" ).Value );
                                    var propertyValueId = int.Parse( child.Attribute( "PropertyValueId" ).Value );
                                    var property = child.Attribute( "Property" ).Value;
                                    var schemaName = ( string ) child.Attribute( "PropertySchemaName" );

                                    index.Properties.Add( new IndexProperty
                                    {
                                        CategoryId = categoryId,
                                        Id = propertyValueId,
                                        Name = property,
                                        SchemaName = schemaName
                                    } );
                                    if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_ACCOMMODATION )
                                        index.AccommodationTypeIds.Add( propertyValueId );
                                    if ( categoryId == ( int ) CodesManager.PROPERTY_CATEGORY_DELIVERY_TYPE )
                                        index.DeliveryMethodTypeIds.Add( propertyValueId );

                                }
                            }
                        }
                        catch ( Exception ex )
                        {
                            LoggingHelper.DoTrace( 2, string.Format( " # {0}. Exception on ScheduledOfferingProperties id: {1}; \r\n{2}", cntr, index.Id, ex.Message ) );
                        }

                        #endregion

                        #region Addresses
                        var addresses = dr["Addresses"].ToString();
                        if ( !string.IsNullOrWhiteSpace( addresses ) )
                        {
                            FormatAddressesToElastic( index, addresses );

                            var xDoc = new XDocument();

                        }
                        if ( index.Addresses.Count == 0 )
                        {
                            if ( UtilityManager.GetAppKeyValue( "ifNoResourceAddressThenAddOrgAddresses", false ) )
                            {
                                //TBD, or maybe specific config for support service
                                //prototype: if no resource addresses, and one org address, then add to index (not detail page)
                                //var orgAddresses = GetRowPossibleColumn( dr, "OrgAddresses" );
                                //if ( !string.IsNullOrWhiteSpace( orgAddresses ) )
                                //{
                                //    FormatAddressesToElastic( index, orgAddresses, true );
                                //}
                            }
                        }
                        #endregion
                        #region AgentRelationshipsForEntity
                        HandleAgentRelationshipsForEntity( dr, index );

                        #endregion
                        #region language
                        index.InLanguage = GetLanguages( dr );
                        #endregion
                        #region Widgets, collections
                        //???
                        HandleWidgetSelections( index, dr, "ScheduledOfferingFilters" );

                        #endregion
                        #region custom reports
                        int propertyId = 0;
                        //indicator of in registry 
                        //how to make this more generic.
                        //Start with reusing asmt?
                        var ScheduledOfferingPropertyCategory = 60; //for now

                        //always in registry, no blank nodes at this time!
                        //                 if ( !string.IsNullOrWhiteSpace( index.CTID ) )
                        //                 {
                        ////perhaps schema name can just be generic for elastic!
                        //                     if ( GetPropertyId( ScheduledOfferingPropertyCategory, "asmtReport:IsInRegistry", ref propertyId ) )
                        //                         index.ReportFilters.Add( propertyId );
                        //                 }
                        //                 else if ( GetPropertyId( ScheduledOfferingPropertyCategory, "asmtReport:IsNotInRegistry", ref propertyId ) )
                        //                     index.ReportFilters.Add( propertyId );
                        //


                        if ( !string.IsNullOrWhiteSpace( availableOnlineAt ) )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:AvailableOnline", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );

                        //
                        if ( costProfilesCount > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:HasCostProfile", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( conditionProfilesCount > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:HasConditionProfile", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        //

                        if ( index.CommonConditionsCount > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:ReferencesCommonConditions", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.CommonCostsCount > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:ReferencesCommonCosts", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.FinancialAidCount > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:FinancialAid", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.HasOccupations )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:HasOccupations", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );
                        if ( index.Addresses.Count > 0 )
                            if ( GetPropertyId( ScheduledOfferingPropertyCategory, "schedofferingReport:HasAddresses", ref propertyId ) )
                                index.ReportFilters.Add( propertyId );

                        list.Add( index );
                    }
                    #endregion

                }
                catch ( Exception ex )
                {
                    LoggingHelper.DoTrace( 2, string.Format( "ScheduledOffering_SearchForElastic. Last Row: {0}, asmtId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                }
                finally
                {
                    LoggingHelper.DoTrace( 2, string.Format( "ScheduledOffering_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
                }
                return list;
            }
        }



		#endregion

		public static List<ManyInOneIndex> Rubric_SearchForElastic( string filter, int pageSize, int pageNumber, ref int pTotalRows )
		{
			string connectionString = DBConnectionRO();

			var index = new ManyInOneIndex();
			var list = new List<ManyInOneIndex>();
			var result = new DataTable();
			LoggingHelper.DoTrace( 2, "Rubric_SearchForElastic - Starting. filter\r\n " + filter );
			int cntr = 0;
			using ( SqlConnection c = new SqlConnection( connectionString ) )
			{
				c.Open();

				using ( SqlCommand command = new SqlCommand( "[Rubric.ElasticSearch]", c ) )
				{
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add( new SqlParameter( "@Filter", filter ) );
					command.Parameters.Add( new SqlParameter( "@SortOrder", DefaultSearchOrder ) );
					command.Parameters.Add( new SqlParameter( "@StartPageIndex", pageNumber ) );
					command.Parameters.Add( new SqlParameter( "@PageSize", pageSize ) );
					command.CommandTimeout = 300;

					SqlParameter totalRows = new SqlParameter( "@TotalRows", 0 );
					totalRows.Direction = ParameterDirection.Output;
					command.Parameters.Add( totalRows );

					try
					{
						using ( SqlDataAdapter adapter = new SqlDataAdapter() )
						{
							adapter.SelectCommand = command;
							adapter.Fill( result );
						}
						string rows = command.Parameters[ command.Parameters.Count - 1 ].Value.ToString();
						pTotalRows = Int32.Parse( rows );

					}
					catch ( Exception ex )
					{
						index = new ManyInOneIndex();
						index.Name = "EXCEPTION ENCOUNTERED";
						index.Description = ex.Message;
						list.Add( index );

						return list;
					}
				}
				try
				{
					foreach ( DataRow dr in result.Rows )
					{
						cntr++;
						if ( cntr % 100 == 0 )
							LoggingHelper.DoTrace( 2, string.Format( " Rubric loading record: {0}", cntr ) );
						if ( cntr == 450 )
						{

						}

						index = new ManyInOneIndex();
						index.EntityTypeId = CodesManager.ENTITY_TYPE_RUBRIC;
						index.EntityType = "Rubric";
						index.Id = GetRowColumn( dr, "Id", 0 );
						//Need EntityId as the PK for elastic
						index.EntityId = GetRowColumn( dr, "EntityId", 0 );

						index.EntityStateId = GetRowColumn( dr, "EntityStateId", 1 );
						//index.NameIndex = cntr * 1000;
						index.Name = GetRowColumn( dr, "Name", "missing" );


						string rowId = GetRowColumn( dr, "RowId" );
						if ( IsValidGuid( rowId ) )
							index.RowId = new Guid( rowId );

						index.Description = GetRowColumn( dr, "Description", string.Empty );
						index.SubjectWebpage = GetRowColumn( dr, "SubjectWebpage", string.Empty );
						index.CTID = GetRowPossibleColumn( dr, "CTID", string.Empty );
						/* there is not owning org, etc?	*/
						index.PrimaryOrganizationCTID = GetRowColumn( dr, "PrimaryOrganizationCTID", string.Empty );
						index.PrimaryOrganizationId = GetRowColumn( dr, "PrimaryOrganizationId", 0 );
						index.PrimaryOrganizationName = GetRowColumn( dr, "PrimaryOrganizationName", string.Empty );
						//used for autocomplete and phrase prefix queries
						index.PrimaryOrganizationName = index.PrimaryOrganizationName;
						index.NameOrganizationKey = index.Name;
						index.ListTitle = index.Name;
						if ( index.PrimaryOrganizationName.Length > 0 && index.Name.IndexOf( index.PrimaryOrganizationName ) == -1 )
						{
							index.NameOrganizationKey = index.Name + " " + index.PrimaryOrganizationName;
							//ListTitle is not used anymore
							index.ListTitle = index.Name + " (" + index.PrimaryOrganizationName + ")";
						}
						string date = GetRowColumn( dr, "Created", string.Empty );
						if ( IsValidDate( date ) )
							index.Created = DateTime.Parse( date );
						date = GetRowColumn( dr, "LastUpdated", string.Empty );
						if ( IsValidDate( date ) )
							index.LastUpdated = DateTime.Parse( date );
						//define LastUpdated to be EntityLastUpdated
						//date = GetRowColumn( dr, "EntityLastUpdated", string.Empty );
						//if ( IsValidDate( date ) )
						//	index.LastUpdated = DateTime.Parse( date );
						//

						try
						{
							var resourceDetail = GetRowColumn( dr, "ResourceDetail", string.Empty );
							if ( !string.IsNullOrWhiteSpace( resourceDetail ) )
							{
								var resource = JsonConvert.DeserializeObject<APIM.Rubric>( resourceDetail );
								index.ResourceDetail = JObject.FromObject( resource );
							}
						}
                        catch ( Exception ex )
                        {
                            LoggingHelper.LogError( ex, string.Format( "Rubric_SearchForElastic re: ResourceDetail Last Row: {0}, recordId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
                        }
						if ( !string.IsNullOrWhiteSpace( index.CTID ) )
							index.TextValues.Add( index.CTID );
						//counts

						#region AgentRelationshipsForEntity
						//do we need these
						HandleAgentRelationshipsForEntity( dr, index );

						#endregion

						#region Custom Reports
						//??var reportCategoryId = ??;
						//TODO


						#endregion

						list.Add( index );
					}
				}
				catch ( Exception ex )
				{
					LoggingHelper.DoTrace( 2, string.Format( "Rubric_SearchForElastic. Last Row: {0}, CFId: {1} Exception: \r\n{2}", cntr, index.Id, ex.Message ) );
				}
				finally
				{
					LoggingHelper.DoTrace( 2, string.Format( "Rubric_SearchForElastic - Page: {0} Complete. Loaded {1} records", pageNumber, cntr ) );
				}


				return list;
			}
		}



		#endregion
		#region Helper methods
		public static List<MC.TopLevelObject> GetAsTopLevelObject( IIndex index, string input )
		{
			var output = new List<MC.TopLevelObject>();
			try
			{
				var list = JsonConvert.DeserializeObject<List<MC.TopLevelObject>>( input );
				var cntr = 0;
				foreach ( var item in list )
				{
					cntr++;
					//probably want clean it up, or use a lighter object in the index
					output.Add( item );
					
				}
			}
			catch
			{
			}

			return output;
		}



		#endregion
		#region Helper methods
		public static void AddTextValue( IIndex index, string input, bool addingToPremium = false )
        {
            if ( string.IsNullOrWhiteSpace( input ) )
                return;
            //check if exists

            if ( addingToPremium )
            {
                if ( index.PremiumValues.FindIndex( a => a == input ) < 0 )
                    index.PremiumValues.Add( input.Trim() );
            }
            else
            {
                if ( index.TextValues.FindIndex( a => a == input ) < 0 )
                    index.TextValues.Add( input.Trim() );
            }
        }
        public static void AddReportProperty( IIndex index, int count, int reportCategoryId, string reportSchemaname )
        {
            int propertyId = 0;
            if ( count > 0 )
                if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
                {
                    index.ReportFilters.Add( propertyId );
                }
                else
                {
                    //log as probably have a spelling error
                    LoggingHelper.DoTrace( 1, string.Format( "$$$$ElasticManager.AddReportProperty - did not find property for: reportCategoryId: {0}, reportSchemaname: {1}", reportCategoryId, reportSchemaname ) );
                }
        }
        public static void AddReportProperty( IIndex index, int count, int reportCategoryId, string reportTextName, string reportSchemaname )
        {
            int propertyId = 0;
            if ( count > 0 )
                if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
                {
                    index.ReportFilters.Add( propertyId );
                    //AddTextValue( index, reportTextName );
                }
                else
                {
                    //log as probably have a spelling error
                    LoggingHelper.DoTrace( 1, string.Format( "$$$$ElasticManager.AddReportProperty - did not find property for: reportCategoryId: {0}, reportSchemaname: {1}", reportCategoryId, reportSchemaname ) );
                }
        }
        public static void AddReportProperty( IIndex index, bool hasData, int reportCategoryId, string reportSchemaname )
        {
            int propertyId = 0;
            if ( hasData )
                if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
                {
                    index.ReportFilters.Add( propertyId );
                }
                else
                {
                    //log as probably have a spelling error
                    LoggingHelper.DoTrace( 1, string.Format( "$$$$ElasticManager.AddReportProperty (usng hasData) - did not find property for: reportCategoryId: {0}, reportSchemaname: {1}", reportCategoryId, reportSchemaname ) );
                }
        }
        public static void AddReportProperty( IIndex index, bool hasData, int reportCategoryId, string reportTextName, string reportSchemaname )
        {
            int propertyId = 0;
            if ( hasData )
                if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
                {
                    index.ReportFilters.Add( propertyId );
                    //AddTextValue( index, reportTextName );
                }
                else
                {
                    //log as probably have a spelling error
                    LoggingHelper.DoTrace( 1, string.Format( "$$$$ElasticManager.AddReportProperty (usng hasData) - did not find property for: reportCategoryId: {0}, reportSchemaname: {1}", reportCategoryId, reportSchemaname ) );
                }
        }
        public static void AddReportProperty( IIndex index, string input, int reportCategoryId, string reportTextName, string reportSchemaname )
        {
            int propertyId = 0;
            if ( !string.IsNullOrWhiteSpace( input ) )
                if ( GetPropertyId( reportCategoryId, reportSchemaname, ref propertyId ) )
                {
                    index.ReportFilters.Add( propertyId );
                    //AddTextValue( index, reportTextName );
                }
                else
                {
                    //log as probably have a spelling error
                    LoggingHelper.DoTrace( 1, string.Format( "$$$$ElasticManager.AddReportProperty(string input) - did not find property for: reportCategoryId: {0}, reportSchemaname: {1}", reportCategoryId, reportSchemaname ) );
                }
        }

        public static bool ContainsUnicodeCharacter( string input )
        {
            const int MaxAnsiCode = 255;

            return input.Any( c => c > MaxAnsiCode );
        }
        private static List<int> GetIntegerList( DataRow dr, string propertyName )
        {
            string delimitedList = GetRowPossibleColumn( dr, propertyName );
            List<int> list = new List<int>();
            if ( !string.IsNullOrWhiteSpace( delimitedList ) )
            {
                foreach ( var index in delimitedList.Split( '|' ) )
                {
                    list.Add( int.Parse( index ) );
                }
            }
            return list;
        }
        private static List<string> GetLanguages( DataRow dr )
        {
            string languages = GetRowPossibleColumn( dr, "Languages" );
            List<string> list = new List<string>();
            if ( !string.IsNullOrWhiteSpace( languages ) )
            {
                foreach ( var language in languages.Split( '|' ) )
                {
                    if ( !string.IsNullOrWhiteSpace( language ) )
                    {
                        if ( language.IndexOf( "(" ) > 1 )
                            list.Add( language.Substring( 0, language.IndexOf( "(" ) ).Trim() );
                        else
                            list.Add( language );
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Look up property directly in code table
        /// OR - create a cache list of codes to save many lookups
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="schemaName"></param>
        /// <param name="propertyId"></param>
        /// <returns></returns>
        public static bool GetPropertyId( int categoryId, string schemaName, ref int propertyId )
        {
            CodeItem index = CodesManager.GetEntityStatisticBySchema( categoryId, schemaName );
            if ( index != null && index.Id > 0 )
            {
                propertyId = index.Id;
                return true;
            }
            else
            {
                LoggingHelper.DoTrace( 5, string.Format( "WARNING: ElasticManager.GetPropertyId. A property was not found for: categoryId: {0}, schemaName: {1}, ", categoryId, schemaName ) );
            }
            return false;
        }

		/// <summary>
		/// NEW - no longer using CategoryId for new classes in EntityStatistic
		/// </summary>
		/// <param name="entityTypeId"></param>
		/// <param name="schemaName"></param>
		/// <param name="propertyId"></param>
		/// <returns></returns>
		public static bool GetPropertyIdByEntityType( int entityTypeId, string schemaName, ref int propertyId )
		{
			CodeItem index = CodesManager.GetEntityStatisticBySchema( entityTypeId, schemaName );
			if ( index != null && index.Id > 0 )
			{
				propertyId = index.Id;
				return true;
			}
			else
			{
				LoggingHelper.DoTrace( 5, string.Format( "WARNING: ElasticManager.GetPropertyId. A property was not found for: categoryId: {0}, schemaName: {1}, ", entityTypeId, schemaName ) );
			}
			return false;
		}
		#endregion
		public static void HandleFrameworks( IIndex index, string frameworks )
		{
			var xDoc = new XDocument();
			xDoc = XDocument.Parse( frameworks );
			foreach ( var child in xDoc.Root.Elements() )
			{
				var framework = new IndexReferenceFramework
				{
					CategoryId = int.Parse( child.Attribute( "CategoryId" ).Value ),
					ReferenceFrameworkItemId = int.Parse( child.Attribute( "ReferenceFrameworkItemId" ).Value ),
					Name = ( string )child.Attribute( "Name" ) ?? string.Empty,
					CodeGroup = ( string )child.Attribute( "CodeGroup" ) ?? string.Empty,
					SchemaName = ( string )child.Attribute( "SchemaName" ) ?? string.Empty,
					CodedNotation = ( string )child.Attribute( "CodedNotation" ) ?? string.Empty,
				};


				if ( framework.CategoryId == 11 )
				{
					if ( index.Occupation.Contains( framework.CodedNotation ) )
					{
						//skip, WHY? risks?
					}
					else
					{
						index.Occupations.Add( framework );
						//should have a dups check
						index.Occupation.Add( framework.Name );
						index.Occupation.Add( framework.CodedNotation );
						//if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//	AddTextValue( index, "occupation " + framework.Name );
					}
				}
				else if ( framework.CategoryId == 10 )
				{
					if ( index.Industry.Contains( framework.CodedNotation ) )
					{
						//skip, risks?
					}
					else
					{
						index.Industries.Add( framework );
						//should have a dups check
						index.Industry.Add( framework.Name );
						index.Industry.Add( framework.CodedNotation );
						//if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//	AddTextValue( index, "industry " + framework.Name );
					}
				}
				else if ( framework.CategoryId == 23 )
				{
					if ( index.InstructionalProgram.Contains( framework.CodedNotation ) )
					{
						//skip, risks?
					}
					else
					{
						index.InstructionalPrograms.Add( framework );
						//should have a dups check
						index.InstructionalProgram.Add( framework.Name );
						index.InstructionalProgram.Add( framework.CodedNotation );
						//if ( UtilityManager.GetAppKeyValue( "includingFrameworksInTextValueIndex", false ) )
						//	AddTextValue( index, "program " + framework.Name );
					}
				}

			}//
		}

        #region Address related
        public static void FormatAddressesToElastic( IIndex index, string addresses, bool skipIfMoreThanOne = false )
		{
			var xDoc = new XDocument();
			//index.RegionIdentifier = new List<MC.IdentifierValue>();
			if ( string.IsNullOrWhiteSpace( addresses ) )
				return;

			try
			{
				xDoc = XDocument.Parse( addresses );
				if ( xDoc.Root.Elements() .Count() > 1 && skipIfMoreThanOne )
                {
					//or take the first one? - small risk???
					//return;
                }
				foreach ( var child in xDoc.Root.Elements() )
				{
					string region = ( string )child.Attribute( "Region" ) ?? string.Empty;
					string city = ( string )child.Attribute( "City" ) ?? string.Empty;
					string country = ( string )child.Attribute( "Country" ) ?? string.Empty;
					//check for identifier
					string identifierJson = ( string ) child.Attribute( "IdentifierJson" ) ?? string.Empty;
					List<MC.IdentifierValue> identifier = null;
					if ( !string.IsNullOrWhiteSpace( identifierJson ) )
					{
						identifier = JsonConvert.DeserializeObject<List<MC.IdentifierValue>>( identifierJson );
						//actually should eliminate duplicates
						foreach(var item in identifier)
                        {
                            //TODO - need to alter with use by other states
                            if ( (item.IdentifierTypeName??string.Empty).ToLower() == "lwia" && string.IsNullOrWhiteSpace( item.IdentifierType ) )
                            {
                                item.IdentifierTypeName = "LWIA";
                                if ( region.ToLower() == "illinois" || region.ToLower() == "il" )
                                    item.IdentifierType = illinoisLWIAIdentityType;
                                else
                                    item.IdentifierType = LWIAIdentityType;
                                //??
                                //item.IdentifierValueCode = "LWIA" + item.IdentifierValueCode;
                            } else if ( ( item.IdentifierTypeName ?? string.Empty ) == "EDR" && string.IsNullOrWhiteSpace( item.IdentifierType ) )
                            {
                                item.IdentifierType = illinoisEDRIdentityType;
                                item.IdentifierValueCode = item.IdentifierValueCode;
                            }
                            else if ( (region.ToLower() == "illinois" || region.ToLower() == "il" ) && string.IsNullOrWhiteSpace( item.IdentifierTypeName )
                                && item.IdentifierValueCode.Length < 3)
                            {
                                //risky
                                item.IdentifierType = illinoisLWIAIdentityType;
                                item.IdentifierTypeName = "LWIA";
                            }

                            var exists = index.RegionIdentifier.FirstOrDefault( s => s.IdentifierTypeName == item.IdentifierTypeName && s.IdentifierValueCode == item.IdentifierValueCode );
							if ( exists == null || exists?.IdentifierTypeName == string.Empty )
							{
								index.RegionIdentifier.Add( item );

								if ( item.IdentifierType == illinoisLWIAIdentityType )
									index.LWIAList.Add( item.IdentifierValueCode );
								if ( item.IdentifierType == illinoisEDRIdentityType )
									index.EDRList.Add( item.IdentifierValueCode );
							}
						}
						//index.RegionIdentifier.AddRange(identifier);
					}

					index.Addresses.Add( new Address
					{
						Latitude = double.Parse( ( string )child.Attribute( "Latitude" ) ),
						Longitude = double.Parse( ( string )child.Attribute( "Longitude" ) ),
						StreetAddress = ( string )child.Attribute( "Address1" ) ?? string.Empty,
						AddressLocality = ( string )child.Attribute( "City" ) ?? string.Empty,
						AddressRegion = ( string )child.Attribute( "Region" ) ?? string.Empty,
						PostalCode = ( string )child.Attribute( "PostalCode" ) ?? string.Empty,
						PostOfficeBoxNumber = ( string ) child.Attribute( "PostOfficeBoxNumber" ) ?? string.Empty,
						AddressCountry = ( string )child.Attribute( "Country" ) ?? string.Empty,
						Identifier = identifier,
					} );
					//Hmm, lat/lng might be useful
					AddLocation( index, city, region, country );

                    //AddLocation( index, city, "city" );
                    //AddLocation( index, region, "region" );
                    //AddLocation( index, country, "country" );


                    if ( skipIfMoreThanOne )
                        break;
                }
			}catch (Exception ex)
			{
				LoggingHelper.LogError( ex, string.Format("HandleAddresses. Name: {0} ({1})", index.Name, index.Id) );
			}
		}
		public static void HandleAddressesFromJson( IIndex index, string jsonProperties )
		{
			if ( string.IsNullOrWhiteSpace( jsonProperties ) )
				return;

			var a = JsonConvert.DeserializeObject<MC.CredentialExternalProperties>( jsonProperties );
			if ( a != null && a.Addresses != null && a.Addresses.Any() )
			{
				foreach ( var item in a.Addresses )
				{
					string region = item.AddressRegion ?? string.Empty;
					string city = item.AddressLocality ?? string.Empty;
					string country = item.AddressCountry ?? string.Empty;
					string identifier = item.IdentifierJson ?? string.Empty;
					if ( !string.IsNullOrWhiteSpace( identifier )) 
					{
						List<Entity_IdentifierValue> evIdentifier = JsonConvert.DeserializeObject<List<Entity_IdentifierValue>>( identifier );
						//need to convert to Identifier
					}

					index.Addresses.Add( new Address
					{
						Latitude = item.Latitude,
						Longitude = item.Longitude,
						StreetAddress = item.StreetAddress,
						//Address2 = item.Address2 ?? string.Empty,
						AddressLocality = city,
						AddressRegion = region,
						PostalCode = item.PostalCode,
						AddressCountry = item.AddressCountry ?? string.Empty
					} );
					AddLocation( index, city, region, country );

					//AddLocation( index, city, "city" );
					//AddLocation( index, region, "region" );
					//AddLocation( index, country, "country" );

				}
			}
		}
		//public static void AddLocation( IIndex index, string input )
		//{
		//	if ( string.IsNullOrWhiteSpace( input ) )
		//		return;
		//	//check if exists

		//	if ( index.Locations.FindIndex( a => a == input ) < 0 )
		//		index.Locations.Add( input.Trim() );
		//}
		public static void AddLocation( IIndex index, string city, string region, string country )
		{
			AddCity( index, city );
			AddRegion( index, region );
			AddCountry( index, country );
		}
		public static void AddCity( IIndex index, string input )
		{
			if ( string.IsNullOrWhiteSpace( input ) )
				return;
			//check if exists
			if ( index.Cities.FindIndex( a => a == input ) < 0 )
				index.Cities.Add( input.Trim() );
		}
		public static void AddRegion( IIndex index, string input )
		{
			if ( string.IsNullOrWhiteSpace( input ) )
				return;
			//check if exists
			if ( index.Regions.FindIndex( a => a == input ) < 0 )
			{
				index.Regions.Add( input.Trim() );
				index.Keyword.Add( "region:" + input );
			}
		}
		public static void AddCountry( IIndex index, string input )
		{
			if ( string.IsNullOrWhiteSpace( input ) )
				return;
			//check if exists
			if ( index.Countries.FindIndex( a => a == input ) < 0 )
				index.Countries.Add( input.Trim() );
		}
        #endregion

        public static void HandleWidgetSelections( IIndex index, DataRow dr, string widgetSection )
		{
			string resourceForWidget = GetRowPossibleColumn( dr, "ResourceForWidget" );
			if ( !string.IsNullOrWhiteSpace( resourceForWidget ) )
			{
				var xDoc = new XDocument();
				xDoc = XDocument.Parse( resourceForWidget );
				foreach ( var child in xDoc.Root.Elements() )
				{
					var widgetId = int.Parse( child.Attribute( "WidgetId" ).Value );
					//future 
					var section = ( string ) child.Attribute( "WidgetSection" ) ?? string.Empty;
					if ( widgetId > 0 && section == widgetSection )
						index.ResourceForWidget.Add( widgetId );
				}//
			}
		}
		public static void HandleCollectionSelections( IIndex index, DataRow dr )
		{
			string list = GetRowPossibleColumn( dr, "CollectionMembers" );
			if ( !string.IsNullOrWhiteSpace( list ) )
			{
				var xDoc = XDocument.Parse( list );
				foreach ( var child in xDoc.Root.Elements() )
				{
					var collectionId = int.Parse( child.Attribute( "CollectionId" ).Value );
					if ( collectionId > 0  )
						index.ResourceForCollection.Add( collectionId );
					var name = ( string ) child.Attribute( "Collection" ) ?? string.Empty;
					if ( !string.IsNullOrWhiteSpace(name) )
						index.Collection.Add( name );
				}//
			}
		}
		//
		[Obsolete]
        public static void HandleMemberOfTransferValue( IIndex index, DataRow dr )
        {
            //string list = GetRowPossibleColumn( dr, "TransferValueReference" );
            //if ( !string.IsNullOrWhiteSpace( list ) )
            //{
            //    var xDoc = XDocument.Parse( list );
            //    foreach ( var child in xDoc.Root.Elements() )
            //    {
            //        var memberId = int.Parse( child.Attribute( "TransferValueProfileId" ).Value );
            //        if ( memberId > 0 )
            //            index.ResourceInTransferValue.Add( memberId );
            //    }//
            //}
        }
        //
        public static void HandleResourceHasSupportService( IIndex index, AJAXSettings hasSupportService )
        {
			if ( hasSupportService == null || (bool)!hasSupportService.Values?.Any() )
				return;
            //TODO should add actual Id to AjaxSettings, otherwise need to extract from the URL
            List<APIM.Outline> outlines = JsonConvert.DeserializeObject<List<APIM.Outline>>( hasSupportService.Values.ToString() ) ;

			foreach (var item in outlines )
			{
				//NOTE: Meta_Id was added, so check for it first
				var id = item.Meta_Id ?? 0;
				if ( id == 0 )
					id = ExtractIdFromURL( item.URL );
                if ( id > 0 )
                    index.ResourceHasSupportService.Add( id );
            }
        }

		public static List<IndexProperty> HandleResource( AJAXSettings input )
		{
			List<IndexProperty> output = new List<IndexProperty>();
			if ( input == null || ( bool ) !input.Values?.Any() )
				return output;
			string transferValues = input.Values.ToString();

			foreach ( var item in input.Values )
			{
				APIM.Outline inp = JsonConvert.DeserializeObject<APIM.Outline>( item.ToString() );
				var competency = new IndexProperty
				{
					Name = inp.Label,
					Id = inp.Meta_Id??0
				};
				output.Add( competency );
			}
			return output;
		}
		//
	
		/// <summary>
		/// Check for data related to has transfer values
		/// </summary>
		/// <param name="index"></param>
		/// <param name="hasTransferValues"></param>
		/// <param name="transferValueType">1=provides; 2=receives</param>
		public static void HandleResourceProvidesTransferValues( IIndex index, AJAXSettings hasTransferValues, int transferValueType  )
		{
			if ( hasTransferValues == null || ( bool ) !hasTransferValues.Values?.Any() )
				return;
			//TODO should add actual Id to AjaxSettings, otherwise need to extract from the URL
			List<APIM.Outline> outlines = JsonConvert.DeserializeObject<List<APIM.Outline>>( hasTransferValues.Values.ToString() );

			foreach ( var item in outlines )
			{
				var id = item.Meta_Id ?? 0;
				if ( id == 0 )
					id = ExtractIdFromURL( item.URL );
				if ( id > 0 )
				{
					if ( transferValueType == 1 )
						index.ResourceProvidesTransferValues.Add( id );
					else
						index.ResourceReceivesTransferValues.Add( id );
				}
			}
		}
		//
		public static void HandleAgentRelationshipsForEntity( DataRow dr, BaseIndex index)
		{
			//we may be able to remove this property
			//string agentRelations = GetRowPossibleColumn( dr, "AgentRelationships" );
			//if ( !string.IsNullOrWhiteSpace( agentRelations ) )
			//{
			//	var xDoc = new XDocument();
			//	xDoc = XDocument.Parse( agentRelations );
			//	foreach ( var child in xDoc.Root.Elements() )
			//		index.AgentRelationships.Add( int.Parse( ( string )child.Attribute( "RelationshipTypeId" ) ) );
			//}
			//includes publishedBy
			string agentRelationshipsForEntity = GetRowPossibleColumn( dr, "AgentRelationshipsForEntity" );
			if ( !string.IsNullOrWhiteSpace( agentRelationshipsForEntity ) )
			{
				HandleAgentRelationshipsForEntity( agentRelationshipsForEntity, index );
			}
		}

		public static void HandleAgentRelationshipsForEntity( string agentRelationshipsForEntity, BaseIndex index )
		{

			if ( !string.IsNullOrWhiteSpace( agentRelationshipsForEntity ) )
			{
				if ( ContainsUnicodeCharacter( agentRelationshipsForEntity ) )
				{
					agentRelationshipsForEntity = Regex.Replace( agentRelationshipsForEntity, @"[^\u0000-\u007F]+", string.Empty );
				}
				agentRelationshipsForEntity = agentRelationshipsForEntity.Replace( "&", " " );
				var xDoc = XDocument.Parse( agentRelationshipsForEntity );
				foreach ( var child in xDoc.Root.Elements() )
				{
					string agentName = ( string ) child.Attribute( "AgentName" ) ?? string.Empty;
					string relationshipTypeIds = ( string ) child.Attribute( "RelationshipTypeIds" ) ?? string.Empty;
					string relationships = ( string ) child.Attribute( "Relationships" ) ?? string.Empty;
					//dropping this property as is not being used. From View: [Entity.AgentRelationshipIdCSV]
					string agentContextRoles = ( string ) child.Attribute( "AgentContextRoles" ) ?? string.Empty;

					if ( !string.IsNullOrWhiteSpace( agentName ) && !string.IsNullOrWhiteSpace( relationshipTypeIds ) )
					{
						var relationshipIds = new List<int>();
						foreach ( var s in child.Attribute( "RelationshipTypeIds" ).Value.Split( new char[] { ',' } ) )
						{
							relationshipIds.Add( int.Parse( s.Trim() ) );
						}
						var relationshipsList = new List<string>();
						foreach ( var s in child.Attribute( "Relationships" ).Value.Split( new char[] { ',' } ) )
						{
							if ( !string.IsNullOrWhiteSpace( s ) )
								relationshipsList.Add( s.Trim() );
						}
						var agentRelationshipsList = new List<string>();
						foreach ( var s in child.Attribute( "AgentContextRoles" ).Value.Split( new char[] { ',' } ) )
						{
							if ( !string.IsNullOrWhiteSpace( s ) )
								agentRelationshipsList.Add( s.Trim() );
						}
						index.AgentRelationshipsForEntity.Add( new AgentRelationshipForEntity
						{
							OrgId = int.Parse( child.Attribute( "OrgId" ).Value ),
							//todo get/split list of ids
							//RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
							RelationshipTypeIds = relationshipIds,
							AgentUrl = ( string ) child.Attribute( "AgentUrl" ) ?? string.Empty,
							AgentName = ( string ) child.Attribute( "AgentName" ) ?? string.Empty,
							EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),
							Relationships = relationshipsList,
							AgentRelationships = agentRelationshipsList
						} );
						//add phrase. ex Accredited by microsoft. Should not be doing owned or offered by 
						if ( !string.IsNullOrWhiteSpace( relationships ) && !string.IsNullOrWhiteSpace( agentName ) && relationships.ToLower().IndexOf( "owned" ) == -1 && relationships.ToLower().IndexOf( "offered" ) == -1 )
							index.QualityAssurancePhrase.Add( string.Format( "{0} {1}", relationships, agentName ) );
					}
				}
			}
		}

		public static void HandleDataProvidersForEntity( DataRow dr, BaseIndex index )
		{

			//
			string providers = GetRowPossibleColumn( dr, "DataSetProfileProviders" );
			if ( !string.IsNullOrWhiteSpace( providers ) )
			{
				if ( ContainsUnicodeCharacter( providers ) )
				{
					providers = Regex.Replace( providers, @"[^\u0000-\u007F]+", string.Empty );
				}
				providers = providers.Replace( "&", " " );
				var xDoc = XDocument.Parse( providers );
				foreach ( var child in xDoc.Root.Elements() )
				{
					string agentName = ( string )child.Attribute( "DataProviderName" ) ?? string.Empty;

					if ( !string.IsNullOrWhiteSpace( agentName )  )
					{
						//for now only one relationship/purpose
						//var relationshipIds = new List<int>();
						//foreach ( var s in child.Attribute( "RelationshipTypeIds" ).Value.Split( new char[] { ',' } ) )
						//{
						//	relationshipIds.Add( int.Parse( s.Trim() ) );
						//}

						index.OutcomeProvidersForEntity.Add( new AgentRelationshipForEntity
						{
							OrgId = int.Parse( child.Attribute( "OrgId" ).Value ),
							AgentName = ( string )child.Attribute( "DataProviderName" ) ?? string.Empty,
							AgentUrl = ( string )child.Attribute( "SubjectWebpage" ) ?? string.Empty,
							EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value ),							 
							
						} );
					}
				}
			}
		}

		/// <summary>
		/// Add QA relationships from third party, not direct
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="index"></param>
		public static void HandleDirectAgentRelationshipsForEntity( DataRow dr, BaseIndex index )
		{


			string agentRelationshipsForEntity = GetRowPossibleColumn( dr, "ThirdPartyQualityAssuranceReceived" );
			if ( string.IsNullOrWhiteSpace( agentRelationshipsForEntity ) )
				return;


			
			if ( ContainsUnicodeCharacter( agentRelationshipsForEntity ) )
			{
				agentRelationshipsForEntity = Regex.Replace( agentRelationshipsForEntity, @"[^\u0000-\u007F]+", string.Empty );
			}
			agentRelationshipsForEntity = agentRelationshipsForEntity.Replace( "&", " " );
			var xDoc = XDocument.Parse( agentRelationshipsForEntity );
			foreach ( var child in xDoc.Root.Elements() )
			{
				string agentName = ( string )child.Attribute( "AgentName" ) ?? string.Empty;
				int orgId = int.Parse( child.Attribute( "OrgId" ).Value );
				string relationshipTypeIds = ( string )child.Attribute( "RelationshipTypeIds" ) ?? string.Empty;
				//string relationships = ( string )child.Attribute( "Relationships" ) ?? string.Empty;
				//string agentContextRoles = ( string )child.Attribute( "AgentContextRoles" ) ?? string.Empty;

				if ( !string.IsNullOrWhiteSpace( agentName ) && !string.IsNullOrWhiteSpace( relationshipTypeIds ) )
				{


					//check if already exist
					var exists = index.AgentRelationshipsForEntity.Select( s => s.OrgId == orgId ).ToList();
					if ( exists == null || exists.Count == 0 )
					{
						var relationshipIds = new List<int>();
						foreach ( var s in child.Attribute( "RelationshipTypeIds" ).Value.Split( new char[] { ',' } ) )
						{
							relationshipIds.Add( int.Parse( s.Trim() ) );
						}

						index.AgentRelationshipsForEntity.Add( new AgentRelationshipForEntity
						{
							OrgId = int.Parse( child.Attribute( "OrgId" ).Value ),
							IsDirectAssertion = true,
							//todo get/split list of ids
							//RelationshipTypeId = int.Parse( child.Attribute( "RelationshipTypeId" ).Value ),
							RelationshipTypeIds = relationshipIds,
							AgentName = ( string )child.Attribute( "AgentName" ) ?? string.Empty,
							EntityStateId = int.Parse( child.Attribute( "EntityStateId" ).Value )
						} );
						//add phrase. ex Accredited by microsoft. Should not be doing owned or offered by 
						//if ( !string.IsNullOrWhiteSpace( relationships ) && !string.IsNullOrWhiteSpace( agentName ) && relationships.ToLower().IndexOf( "owned" ) == -1 && relationships.ToLower().IndexOf( "offered" ) == -1 )
						//	index.QualityAssurancePhrase.Add( string.Format( "{0} {1}", relationships, agentName ) );
					}
				}
			}
			
		}
	}
}