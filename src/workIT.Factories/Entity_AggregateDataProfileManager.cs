﻿using System;
using System.Collections.Generic;
using System.Linq;

using workIT.Models;
using workIT.Models.Common;
using ThisEntity = workIT.Models.Common.AggregateDataProfile;
using DBEntity = workIT.Data.Tables.Entity_AggregateDataProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using CM = workIT.Models.Common;
using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;
using Newtonsoft.Json;
//Entity_AggregateDataProfileManager
namespace workIT.Factories
{
	public class Entity_AggregateDataProfileManager : BaseFactory
	{
		static string thisClassName = "AggregateDataProfileManager";


		#region persistance ==================

		public bool SaveList( List<ThisEntity> list, Entity parent, ref SaveStatus status )
		{
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( "Error - the parent entity was not found." );
				return false;
			}

			//TODO - this check is wrong but currently does allow for the Indiana prototyping
			//		- if data is updated in the publisher (i.e. no ADP) the ADP will not be deleted here.
			//		- maybe need some hack in a property to help with this??

			//get any current
			var currentRecords = GetAll( parent );

			//may need to delete all here, as cannot easily/dependably look up the profile
			DeleteAll( parent.EntityUid, ref status );

			if ( currentRecords.Count != list.Count )
				status.UpdateElasticIndex = true;
			//
			if ( list == null || list.Count == 0 )
			{
				return true;
			}

			//now be still do a delete all until implementing a balance line
			//could set date and delete all before this date!
			DateTime updateDate = DateTime.Now;
			if ( IsValidDate( status.EnvelopeUpdatedDate ) )
			{
				updateDate = status.LocalUpdatedDate;
			}

			
			bool isAllValid = true;
			//if ( list == null || list.Count == 0 )
			//{
			//	//if ( currentRecords != null && currentRecords.Any() )
			//	//{
			//	//	//no input, and existing conditions, delete all
			//	//	//DeleteAll( parent.EntityUid, ref status );
			//	//}
			//	return true;
			//}
			//may not need this if the new list version works
			//else if ( list.Count == 1 && currentRecords.Count == 1 )
			//{
			//	//One of each, just do update of one
			//	var entity = list[ 0 ];
			//	entity.Id = currentRecords[ 0 ].Id;
			//	Save( entity, parent, updateDate, ref status );
			//}
			//else
			//{

				foreach ( ThisEntity item in list )
				{
					Save( item, parent, updateDate, ref status );
				}
			//delete any entities with last updated less than updateDate
			//DeleteAll( parent, ref status, updateDate );
			//}

			return isAllValid;
		}

		private bool Save( ThisEntity item, Entity parent, DateTime updateDate, ref SaveStatus status )
		{
			bool isValid = true;

			item.EntityId = parent.Id;

			using ( var context = new EntityContext() )
			{
				if ( !ValidateProfile( item, ref status ) )
				{
					return false;
				}

				//should always be add if always resetting the entity
				if ( item.Id > 0 )
				{
					DBEntity p = context.Entity_AggregateDataProfile
							.FirstOrDefault( s => s.Id == item.Id );
					if ( p != null && p.Id > 0 )
					{
						item.RowId = p.RowId;
						item.EntityId = p.EntityId;
						MapToDB( item, p );

						if ( HasStateChanged( context ) )
						{
							p.LastUpdated = System.DateTime.Now;
							context.SaveChanges();
						}
						//regardless, check parts
						isValid = UpdateParts( item, updateDate, ref status );
					}
					else
					{
						//error should have been found
						isValid = false;
						status.AddWarning( string.Format( "Error: the requested record was not found: recordId: {0}", item.Id ) );
					}
				}
				else
				{
					int newId = Add( item, updateDate, ref status );
					if ( newId == 0 || status.HasErrors )
						isValid = false;
				}
			}
			return isValid;
		}


		/// <summary>
		/// add a ConditionProfile
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		private int Add( ThisEntity entity, DateTime updateDate, ref SaveStatus status )
		{
			DBEntity efEntity = new DBEntity();
			using ( var context = new EntityContext() )
			{
				try
				{
					MapToDB( entity, efEntity );

					efEntity.EntityId = entity.EntityId;
					if ( IsValidGuid( entity.RowId ) )
						efEntity.RowId = entity.RowId;
					else
						efEntity.RowId = Guid.NewGuid();
					efEntity.Created = efEntity.LastUpdated = updateDate;

					context.Entity_AggregateDataProfile.Add( efEntity );

					// submit the change to database
					int count = context.SaveChanges();
					if ( count > 0 )
					{
						entity.Id = efEntity.Id;
						entity.RowId = efEntity.RowId;

						UpdateParts( entity, updateDate, ref status );

						return efEntity.Id;
					}
					else
					{
						//?no info on error
						status.AddWarning( "Error - the profile was not saved. " );
						string message = string.Format( "{0}.Add() Failed", "Attempted to add a AggregateDataProfile. The process appeared to not work, but was not an exception, so we have no message, or no clue. AggregateDataProfile. EntityId: {1}", thisClassName, entity.EntityId );
						EmailManager.NotifyAdmin( thisClassName + ".Add() Failed", message );
					}
				}
				catch ( System.Data.Entity.Validation.DbEntityValidationException dbex )
				{
					string message = HandleDBValidationError( dbex, "AggregateDataProfileManager.Add()", string.Format( "EntityId: 0   ", entity.EntityId ) );
					status.AddWarning( message );

				}
				catch ( Exception ex )
				{
					LoggingHelper.LogError( ex, thisClassName + string.Format( ".Add(), EntityId: {0}", entity.EntityId ) );
				}
			}

			return efEntity.Id;
		}
		public bool DeleteAll( Guid parentUid, ref SaveStatus status, DateTime? lastUpdated = null )
		{
			bool isValid = true;
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
				return false;
			}
			try
			{
				using ( var context = new EntityContext() )
				{

					var results = context.Entity_AggregateDataProfile.Where( s => s.EntityId == parent.Id && ( lastUpdated == null || s.LastUpdated < lastUpdated ) ).ToList();
					if ( results == null || results.Count == 0 )
						return true;

					foreach ( var item in results )
					{
						//21-03-31 mp - just removing the profile will not remove its entity and the latter's children!
						//21-04-22 mp - we have a trigger for this
						//string statusMessage = "";
						//delete the entity in order to remove Entity.DatasetProfile - but the datasetProfile doesn't get removed!
						//new EntityManager().Delete( item.RowId, string.Format( "AggregateDataProfile: {0} for EntityType: {1} ({2})", item.Id, parent.EntityTypeId, parent.EntityBaseId ), ref statusMessage );

						context.Entity_AggregateDataProfile.Remove( item );
						var count = context.SaveChanges();
						if ( count > 0 )
						{

						}
					}
				}

			}
			catch ( Exception ex )
			{
				var msg = BaseFactory.FormatExceptions( ex );
				LoggingHelper.DoTrace( 1, string.Format( thisClassName + ".DeleteAll. ParentType: {0}, baseId: {1}, exception: {2}", parent.EntityType, parent.EntityBaseId, msg ) );
			}
			return isValid;
		}

	
		public bool UpdateParts( ThisEntity entity, DateTime updateDate, ref SaveStatus status )
		{
			bool isAllValid = true;
			Entity relatedEntity = EntityManager.GetEntity( entity.RowId );
			if ( relatedEntity == null || relatedEntity.Id == 0 )
			{
				status.AddError( "Error - the related Entity was not found." );
				return false;
			}

			//JurisdictionProfile 
			Entity_JurisdictionProfileManager jpm = new Entity_JurisdictionProfileManager();
			//do deletes 
			jpm.DeleteAll( relatedEntity, ref status );
			if ( !jpm.SaveList( entity.Jurisdiction, entity.RowId, Entity_JurisdictionProfileManager.JURISDICTION_PURPOSE_SCOPE, ref status ) )
				isAllValid = false;
			//datasetProfiles
			if ( !new DataSetProfileManager().SaveList( entity.RelevantDataSet, relatedEntity, ref status ) )
				isAllValid = false;


			//
			return isAllValid;
		}
	
		/// <summary>
		/// Delete a Condition Profile, and related Entity
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="statusMessage"></param>
		/// <returns></returns>
		//public bool Delete( int profileId, ref string statusMessage )
		//{
		//	bool isValid = false;
		//	if ( profileId == 0 )
		//	{
		//		statusMessage = "Error - missing an identifier for the ConditionProfile";
		//		return false;
		//	}
		//	using (var context = new EntityContext())
		//	{
		//		DBEntity efEntity = context.AggregateDataProfile
		//					.SingleOrDefault( s => s.Id == profileId );

		//		if (efEntity != null && efEntity.Id > 0)
		//		{
		//			Guid rowId = efEntity.RowId;
		//			context.AggregateDataProfile.Remove(efEntity);
		//			int count = context.SaveChanges();
		//			if (count > 0)
		//			{
		//				isValid = true;
		//				//16-10-19 mp - create 'After Delete' triggers to delete the Entity
		//				//new EntityManager().Delete(rowId, ref statusMessage);
		//			}
		//		}
		//		else
		//		{
		//			statusMessage = "Error - delete was not possible, as record was not found.";
		//		}
		//	}

		//	return isValid;
		//}

		private bool ValidateProfile( ThisEntity item, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			bool isNameRequired = true;

			
			return status.WasSectionValid;
		}
		#endregion

		#region == Retrieval =======================

		public static List<ThisEntity> GetAll( Entity parent, bool includingParts = true )
		{
			ThisEntity entity = new ThisEntity();
			List<ThisEntity> list = new List<ThisEntity>();
			//Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return list;
			}

			try
			{
				using ( var context = new EntityContext() )
				{
					//context.Configuration.LazyLoadingEnabled = false;

					List<DBEntity> results = context.Entity_AggregateDataProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						foreach ( DBEntity item in results )
						{
							entity = new ThisEntity();
							//??do we need all data? It will be replaced. The main issue will be references to lopps, asmts, etc. 
							MapFromDB( item, entity );
							list.Add( entity );
						}
					}
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetAll" );
			}
			return list;
		}//


		private static void MapToDB( ThisEntity input, DBEntity output )
		{

			//want output ensure fields input create are not wiped
			if ( output.Id < 1 )
			{

				//output.EntityId = input.;
			}
			else
			{

			}

			output.Id = input.Id;

			output.Name = GetData( input.Name );
			output.Description = GetData( input.Description );
			output.DemographicInformation = GetData( input.DemographicInformation );
			//
			output.NumberAwarded = input.NumberAwarded;
			output.LowEarnings = input.LowEarnings;
			output.MedianEarnings = input.MedianEarnings;
			output.HighEarnings = input.HighEarnings;
			output.PostReceiptMonths = input.PostReceiptMonths;
			output.Source = input.Source;

			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = DateTime.Parse( input.DateEffective );
			else
				output.DateEffective = null;
			if ( input.JobsObtained != null && input.JobsObtained.Any() )
			{
				output.JobsObtainedJson = JsonConvert.SerializeObject( input.JobsObtained );
			}
			else
				output.JobsObtainedJson = null;

			//
			output.Currency = input.Currency;


		}

		public static void MapFromDB( DBEntity input, ThisEntity output	)
		{
			output.Id = input.Id;
			output.RowId = input.RowId;
			output.Name = input.Name == null ? "" : input.Name;
			output.Description = input.Description == null ? "" : input.Description;
			output.DemographicInformation = input.DemographicInformation == null ? "" : input.DemographicInformation;
			if ( IsValidDate( input.DateEffective ) )
				output.DateEffective = ( ( DateTime )input.DateEffective ).ToString("yyyy-MM-dd");
			else
				output.DateEffective = "";
			//
			output.NumberAwarded = ( input.NumberAwarded ?? 0 );
			output.LowEarnings = ( input.LowEarnings ?? 0 );
			output.MedianEarnings = ( input.MedianEarnings ?? 0 );
			output.HighEarnings = ( input.HighEarnings ?? 0 );
			output.PostReceiptMonths = ( input.PostReceiptMonths ?? 0 );
			output.Source = GetUrlData( input.Source );

			output.Currency = input.Currency;
			Views.Codes_Currency code = CodesManager.GetCurrencyItem( output.Currency );
			if ( code != null && code.NumericCode > 0 )
			{
				output.Currency = code.Currency;
				output.CurrencySymbol = code.HtmlCodes;
			}

			if ( IsValidDate( input.Created ) )
				output.Created = ( DateTime )input.Created;
			if ( IsValidDate( input.LastUpdated ) )
				output.LastUpdated = ( DateTime )input.LastUpdated;

			if ( !string.IsNullOrEmpty( input.JobsObtainedJson ) )
			{
				var jobsObtained = JsonConvert.DeserializeObject<List<QuantitativeValue>>( input.JobsObtainedJson );
				if ( jobsObtained != null )
				{
					output.JobsObtained = jobsObtained;
				}
			}

			//=====
			//var relatedEntity = EntityManager.GetEntity( output.RowId, false );
			//if ( relatedEntity != null && relatedEntity.Id > 0 )
			//	output.EntityLastUpdated = relatedEntity.LastUpdated;

			//components

			//
			output.Jurisdiction = Entity_JurisdictionProfileManager.Jurisdiction_GetAll( output.RowId );
			//get datasetprofiles
			output.RelevantDataSet = DataSetProfileManager.GetAll( output.RowId, true );
		
			//==========


			
		} //
		/// <summary>
		/// Format a summary of the HoldersProfile for use in search and gray boxes
		/// </summary>
		/// <param name="parentUid"></param>
		/// <returns></returns>
		public static string GetSummary( Guid parentUid, string entityName )
		{
			var list = new List<ThisEntity>();
			var entity = new ThisEntity();

			Entity parent = EntityManager.GetEntity( parentUid );
			LoggingHelper.DoTrace( 7, string.Format( thisClassName + ".GetAll: parentUid:{0} entityId:{1}, e.EntityTypeId:{2}", parentUid, parent.Id, parent.EntityTypeId ) );
			var summary = "";
			var lineBreak = "";
			try
			{
				using ( var context = new EntityContext() )
				{
					var results = context.Entity_AggregateDataProfile
							.Where( s => s.EntityId == parent.Id )
							.OrderBy( s => s.Created )
							.ToList();

					if ( results != null && results.Count > 0 )
					{
						//21-04-19 - decided to use a simple generic summary for all outcome data
						summary = string.Format( "Outcome data is available for '{0}'.", entityName );
						/*
						foreach ( var item in results )
						{
							entity = new ThisEntity();
							if ( item != null  )
							{
								var itemSummary = "";
								if ( !string.IsNullOrWhiteSpace( item.Name ) )
								{
									summary += item.Name;
									itemSummary = item.Name;
								}
								else if ( !string.IsNullOrWhiteSpace( item.Description ) )
								{
									summary += item.Description.Length < 200 ? item.Description : item.Description.Substring( 0, 200 ) + "  ... " ;
									itemSummary = item.Description.Length < 200 ? item.Description : item.Description.Substring( 0, 200 );
								}
								else
								{

								}
								if ( string.IsNullOrWhiteSpace( itemSummary ) )
									itemSummary = string.Format("Outcome data for '{0}'", entityName);
								//if ( item.job > 0 )
								//	summary += string.Format( " Number awarded: {0};", item.JobsObtainedJson );
								if ( !string.IsNullOrEmpty( item.JobsObtainedJson ) )
								{
									var jo = SummarizeJobsObtained( item.JobsObtainedJson, itemSummary );
									if ( !string.IsNullOrWhiteSpace( jo ) )
										summary += " " + jo + "; ";
								}
								if ( item.NumberAwarded > 0 )
									summary += string.Format( " Number awarded: {0}; ", ((int)item.NumberAwarded).ToString( "#,##0" ) );
								if ( item.MedianEarnings > 0 )
								{
									if ( itemSummary.IndexOf("Median Earnings") == -1)
										summary += string.Format( " Median Earnings: {0}; ", ( ( int )item.MedianEarnings ).ToString( "$#,##0" ) );
									else
										summary += string.Format( " {0}; ", ( ( int )item.MedianEarnings ).ToString( "$#,##0" ) );
								}
								summary += lineBreak;
							}
							lineBreak = "<br>";
						}
						*/
					}
					return summary;
				}
			}
			catch ( Exception ex )
			{
				LoggingHelper.LogError( ex, thisClassName + ".GetSummary" );
			}
			return summary;
		}
		private static string SummarizeJobsObtained( string jobsObtainedJson, string itemDescription)
		{
			string summary = "";
			var jobsObtained = JsonConvert.DeserializeObject<List<QuantitativeValue>>( jobsObtainedJson );
			if ( jobsObtained != null )
			{
				//just handle one at this time
				var jo = jobsObtained[ 0 ];
				var desc = jo.Description ?? "";
				if ( desc == itemDescription || itemDescription.IndexOf( desc ) != -1 )
				{
					//skip
					desc = "";
				}
				if ( !string.IsNullOrWhiteSpace( jo.UnitText ) )
				{
					var code = CodesManager.GetCurrencyItem( jo.UnitText );
					if ( code != null && code.NumericCode > 0 )
					{
						//not sure can do anything here? Also not applicable to jobs obtained
						//currencySymbol = code.HtmlCodes;
					}
				}
				if ( jo.Percentage != 0 )
				{
					summary += string.Format( " {0}", jo.Percentage );
					if( desc.IndexOf("%") != 0)
					{
						summary += "% "; 
					}
				}
				if ( jo.Value != 0 )
					summary += string.Format( " {0} ", jo.Value.ToString( "#,##0" ) );
				if ( jo.MinValue != 0 && jo.MaxValue != 0 )
					summary += string.Format( " {0} to {1} ", jo.MinValue.ToString( "#,##0" ), jo.MaxValue.ToString( "#,##0" ) );

				//skip description if the same or similar to jo desc?
				if (desc == itemDescription || itemDescription.IndexOf(desc) != -1)
				{
					//skip
				} else 
					summary += " " + desc;

			}

			return summary;
		}
		#endregion


		#region  validations


		#endregion
	}
}
