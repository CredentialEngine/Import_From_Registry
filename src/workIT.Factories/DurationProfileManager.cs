using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;
using ThisResource = workIT.Models.ProfileModels.DurationProfile;
using DBResource = workIT.Data.Tables.Entity_DurationProfile;
using EntityContext = workIT.Data.Tables.workITEntities;
using ViewContext = workIT.Data.Views.workITViews;

using workIT.Utilities;
using WM = workIT.Models.ProfileModels;

using EM = workIT.Data.Tables;
using Views = workIT.Data.Views;


namespace workIT.Factories
{
	public class DurationProfileManager : BaseFactory
	{
		string thisClassName = "DurationProfileManager. ";
		#region persistance ==================
		public bool SaveList( List<ThisResource> list, Guid parentUid, ref SaveStatus status )
		{
            if ( !IsValidGuid( parentUid ) )
            {
                status.AddError( string.Format( "A valid parent identifier was not provided to the {0}.Add method.", thisClassName ) );
                return false;
            }

            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the parent entity was not found." );
                return false;
            }
            DeleteAll( parent, ref status );

            if ( list == null || list.Count == 0 )
				return true;

			bool isAllValid = true;
			foreach ( ThisResource item in list )
			{
				Save( item, parent, ref status );
			}

			return isAllValid;
		}

		public bool SaveRenewalFrequency( WM.DurationItem entity, Guid parentUid, ref SaveStatus status )
		{
			bool isValid = true;
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the parent entity was not found." );
                return false;
            }

            WM.DurationProfile item = new ThisResource();
			if (entity != null && entity.HasValue)
			{
				item.ExactDuration = entity;
                item.DurationProfileTypeId = 3;

                return Save( item, parent, ref status );
			}
			return isValid;
		}

		public bool Save( WM.DurationProfile profile, Entity parent, ref SaveStatus status )
		{
			bool isValid = true;

			int count = 0;
			//Entity parent = EntityManager.GetEntity( parentUid );
			//if ( parent == null || parent.Id == 0 )
			//{
			//	status.AddError( thisClassName + "Error - the parent entity was not found " + parentUid );
			//	return false;
			//}
			profile.EntityId = parent.Id;
			EM.Entity_DurationProfile efEntity = new EM.Entity_DurationProfile();
			//entityId will be in the passed entity
			//will it?????
			using ( var context = new EntityContext() )
			{
				//check add/updates first
				
				bool isEmpty = false;

				if ( ValidateDurationProfile( profile, ref isEmpty, ref status ) == false )
				{
					return false;
				}
				if ( isEmpty )
				{
					//status.AddWarning( thisClassName + "Error - no data was entered." );
					return false;
				}

				if ( profile.Id == 0 )
				{
					//add
					efEntity = new EM.Entity_DurationProfile();
					
					MapToDB( profile, efEntity );
					efEntity.EntityId = profile.EntityId;
					efEntity.Created = efEntity.LastUpdated = DateTime.Now;

					context.Entity_DurationProfile.Add( efEntity );
					count = context.SaveChanges();
					//update profile record so doesn't get deleted
					profile.Id = efEntity.Id;

					if ( count == 0 )
					{
						status.AddError( " Unable to add Duration Profile" ) ;
						isValid = false;
					}
				}
				else
				{
					efEntity = context.Entity_DurationProfile.SingleOrDefault( s => s.Id == profile.Id );
					if ( efEntity != null && efEntity.Id > 0 )
					{
						//update
						MapToDB( profile, efEntity );
						//has changed?
						if ( HasStateChanged( context ) )
						{
							//note: testing - the latter may be true if the child has changed - but shouldn't as the mapping only updates the parent
							efEntity.LastUpdated = System.DateTime.Now;
							count = context.SaveChanges();
						}
					}
					else
					{
						//??? shouldn't happen unless deleted somehow
						status.AddError( " Unable to update Duration Profile - the profile was not found." );
					}
				}
			}

			return isValid;
		}

		public bool Delete( int recordId, ref string statusMessage )
		{
			bool isOK = true;
			using ( var context = new EntityContext() )
			{
				EM.Entity_DurationProfile p = context.Entity_DurationProfile.FirstOrDefault( s => s.Id == recordId );
				if ( p != null && p.Id > 0 )
				{
					context.Entity_DurationProfile.Remove( p );
					int count = context.SaveChanges();
				}
				else
				{
					statusMessage = string.Format( "DurationProflie record was not found: {0}", recordId );
					isOK = false;
				}
			}
			return isOK;

		}
        /// <summary>
        /// Delete all properties for parent (in preparation for import)
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteAll( Entity parent, ref SaveStatus status )
        {
            bool isValid = true;
            //Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                status.AddError( thisClassName + ". Error - the provided target parent entity was not provided." );
                return false;
            }
            using ( var context = new EntityContext() )
            {
                context.Entity_DurationProfile.RemoveRange( context.Entity_DurationProfile.Where( s => s.EntityId == parent.Id ) );
                int count = context.SaveChanges();
                if ( count > 0 )
                {
                    isValid = true;
                }
                else
                {
                    //if doing a delete on spec, may not have been any properties
                }
            }

            return isValid;
        }
        private void MapToDB( ThisResource input, DBResource output )
		{
			decimal totalMinutes = 0;
			output.Id = input.Id;
			//make sure EntityId is not wiped out. Also can't actually chg
			//if ( (to.EntityId ?? 0) == 0)
			//	to.EntityId = from.EntityId;

			if ( output.Id == 0 )
			{
				//to.ParentUid = from.ParentUid;
				//to.ParentTypeId = from.ParentTypeId;
			}
			
			output.DurationComment = input.Description;
			
			bool hasExactDuration = false;
			bool hasRangeDuration = false;
			if ( HasDurationItems( input.ExactDuration ) )
				hasExactDuration = true;
			if ( HasDurationItems( input.MinimumDuration ) || HasDurationItems( input.MaximumDuration ) )
				hasRangeDuration = true;
			string durationOnly = string.Empty;
			output.TypeId = 0;
			//validations should be done before here
			if ( hasExactDuration )
			{
				//if ( hasRangeDuration )
				//{
				//	//inconsistent, take exact for now
				//	ConsoleMessageHelper.SetConsoleErrorMessage( "Error - you must either enter just an exact duration or a 'from - to' duration, not both. For now, the exact duration was used", string.Empty, false );
				//}
				

				output.FromYears = input.ExactDuration.Years;
				output.FromMonths = input.ExactDuration.Months;
				output.FromWeeks = input.ExactDuration.Weeks;
				output.FromDays = input.ExactDuration.Days;
				output.FromHours = input.ExactDuration.Hours;

				output.FromMinutes  = input.ExactDuration.Minutes;
				output.FromDuration = AsSchemaDuration( input.ExactDuration, ref totalMinutes );
                //to.AverageMinutes = totalMinutes;

                output.TypeId = input.DurationProfileTypeId == 3 ? input.DurationProfileTypeId : 1;
                //reset any to max duration values
                output.ToYears = null;
				output.ToMonths = null;
				output.ToWeeks = null;
				output.ToDays = null;
				output.ToHours = null;
				output.ToMinutes = null;
				output.ToDuration = string.Empty;
				DurationSummary( "", input.ExactDuration, ref durationOnly );
				output.DurationSummary = durationOnly;
			}
			else if ( hasRangeDuration )
			{
				output.FromYears = input.MinimumDuration.Years;
				output.FromMonths = input.MinimumDuration.Months;
				output.FromWeeks = input.MinimumDuration.Weeks;
				output.FromDays = input.MinimumDuration.Days;
				output.FromHours = input.MinimumDuration.Hours;
				output.FromMinutes = input.MinimumDuration.Minutes;
				output.FromDuration = AsSchemaDuration( input.MinimumDuration, ref totalMinutes );
               // int fromMin = totalMinutes;
				DurationSummary( "", input.MinimumDuration, ref durationOnly );
				output.DurationSummary = durationOnly;
				output.ToYears = input.MaximumDuration.Years;
				output.ToMonths = input.MaximumDuration.Months;
				output.ToWeeks = input.MaximumDuration.Weeks;
				output.ToDays = input.MaximumDuration.Days;
				output.ToHours = input.MaximumDuration.Hours;
				output.ToMinutes = input.MaximumDuration.Minutes;
				output.ToDuration = AsSchemaDuration( input.MaximumDuration, ref totalMinutes );
              //  to.AverageMinutes = ( fromMin + totalMinutes ) / 2;
                output.TypeId = 2;
				DurationSummary( "", input.MaximumDuration, ref durationOnly );
				output.DurationSummary += " to " + durationOnly;
			}

			if ( HasDurationItems( input.TimeRequiredImport ) )
			{
				output.TimeRequired = input.TimeRequiredImport.DurationISO8601;
				//should be able to have one property
				if ( input.TimeRequiredImport.Days > 0)
				{
					output.TimeAmount = input.TimeRequiredImport.Days;
					output.TimeUnit = input.TimeRequiredImport.Days> 1 ? "Days" : "Day";
				}
				else if ( input.TimeRequiredImport.Hours > 0 && input.TimeRequiredImport.Minutes > 0 )
				{
					//should not be allowing an hours with decimal with minutes
					output.TimeAmount = input.TimeRequiredImport.Hours * 60 + input.TimeRequiredImport.Minutes;
					output.TimeUnit = output.TimeAmount > 1 ? "Minutes" : "Minute";
				}
				else if ( input.TimeRequiredImport.Hours > 0  )
				{
					output.TimeAmount = input.TimeRequiredImport.Hours;
					output.TimeUnit = output.TimeAmount > 1 ? "Hours" : "Hour";
				}
				if ( input.TimeRequiredImport.Minutes > 0 )
				{
					output.TimeAmount = input.TimeRequiredImport.Minutes;
					output.TimeUnit = output.TimeAmount > 1 ? "Minutes" : "Minute";
				}
				
			}


		}

		private static void MapFromDB( DBResource input, ThisResource output )
		{
			WM.DurationItem duration = new WM.DurationItem();
			decimal totalMinutes = 0;
			string durationOnly = string.Empty;
			output.Id = input.Id;
			output.EntityId = input.EntityId ?? 0;

			output.Description = input.DurationComment;
			output.Created = input.Created != null ? ( DateTime ) input.Created : DateTime.Now;
		
			output.Created = input.LastUpdated != null ? ( DateTime ) input.LastUpdated : DateTime.Now;


			duration = new WM.DurationItem
			{
				Years = input.FromYears == null ? 0 : ( decimal ) input.FromYears,
				Months = input.FromMonths == null ? 0 : ( decimal ) input.FromMonths,
				Weeks = input.FromWeeks == null ? 0 : ( decimal ) input.FromWeeks,
				Days = input.FromDays == null ? 0 : ( decimal ) input.FromDays,
				Hours = input.FromHours == null ? 0 : ( decimal ) input.FromHours,
				Minutes = input.FromMinutes == null ? 0 : ( decimal ) input.FromMinutes
			};

			if ( HasToDurations( input ) )
			{
				//format as from and to
				output.MinimumDuration = duration;
				output.MinimumDurationISO8601 = AsSchemaDuration( duration, ref totalMinutes );
				output.ProfileSummary = DurationSummary( output.Description, duration, ref durationOnly );
				if ( !string.IsNullOrWhiteSpace( durationOnly ) )
					output.DurationSummary = durationOnly;

				duration = new WM.DurationItem();
				duration.Years = input.ToYears == null ? 0 : ( decimal ) input.ToYears;
				duration.Months = input.ToMonths == null ? 0 : ( decimal ) input.ToMonths;
				duration.Weeks = input.ToWeeks == null ? 0 : ( decimal ) input.ToWeeks;
				duration.Days = input.ToDays == null ? 0 : ( decimal ) input.ToDays;
				duration.Hours = input.ToHours == null ? 0 : ( decimal ) input.ToHours;
				duration.Minutes = input.ToMinutes == null ? 0 : ( decimal ) input.ToMinutes;

				output.MaximumDuration = duration;
				output.MaximumDurationISO8601 = AsSchemaDuration( duration, ref totalMinutes );

				output.ProfileSummary += DurationSummary( " to ", duration, ref durationOnly );
				if ( !string.IsNullOrWhiteSpace( durationOnly ) )
					output.DurationSummary += " to " + durationOnly;

			}
			else
			{
				output.ExactDuration = duration;
				output.ExactDurationISO8601 = AsSchemaDuration( duration, ref totalMinutes );
				output.ProfileSummary = DurationSummary( output.Description, duration, ref durationOnly );
				if ( !string.IsNullOrWhiteSpace( durationOnly ) )
					output.DurationSummary = durationOnly;
			}

			output.TimeRequired = input.TimeRequired;
			if (!string.IsNullOrWhiteSpace( output.TimeRequired ) )
			{
				//actually maybe no reason do separate?
				output.TimeUnit = input.TimeUnit;
				output.TimeAmount = input.TimeAmount ?? 0;
			}


			if ( string.IsNullOrWhiteSpace( output.ProfileName ) )
				output.ProfileName = output.ProfileSummary;
			
		}

		/// <summary>
		/// used for renewal, where there is only exactDuration
		/// </summary>
		/// <param name="from"></param>
		/// <param name="duration"></param>
        private static void MapFromDB( DBResource from, WM.DurationItem duration )
        {
            //WM.DurationItem duration = new WM.DurationItem();
            //duration = new WM.DurationItem();
            duration.Years = from.FromYears == null ? 0 : ( decimal ) from.FromYears;
            duration.Months = from.FromMonths == null ? 0 : ( decimal ) from.FromMonths;
            duration.Weeks = from.FromWeeks == null ? 0 : ( decimal ) from.FromWeeks;
            duration.Days = from.FromDays == null ? 0 : ( decimal ) from.FromDays;
            duration.Hours = from.FromHours == null ? 0 : ( decimal ) from.FromHours;
            duration.Minutes = from.FromMinutes == null ? 0 : ( decimal ) from.FromMinutes;
        }
        #endregion

        #region  retrieval ==================

        /// <summary>
        /// Retrieve and fill duration profiles for parent entity
        /// </summary>
        /// <param name="parentUid"></param>
        public static List<ThisResource> GetAll( Guid parentUid, int typeId = 0 )
		{
			ThisResource row = new ThisResource();
			WM.DurationItem duration = new WM.DurationItem();
			List<ThisResource> profiles = new List<ThisResource>();
			Entity parent = EntityManager.GetEntity( parentUid );
			if ( parent == null || parent.Id == 0 )
			{
				return profiles;
			}

			using ( var context = new EntityContext() )
			{
				List<EM.Entity_DurationProfile> results = context.Entity_DurationProfile
						.Where( s => s.EntityId == parent.Id 
                        && ( ( typeId == 0 && s.TypeId < 3 ) || s.TypeId == typeId ))
                        .OrderBy( s => s.Id )
						.ToList();

				if ( results != null && results.Count > 0 )
				{
					foreach ( EM.Entity_DurationProfile item in results )
					{
						row = new ThisResource();
						MapFromDB( item, row );
						profiles.Add( row );
					}
				}
				return profiles;
			}

		}//
		/// <summary>
		/// Get a single DurationProfile by integer Id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
        /// 
        public static WM.DurationItem GetRenewalDuration( Guid parentUid )
        {
            
            WM.DurationItem duration = new WM.DurationItem();
            ThisResource profile = new ThisResource();
            Entity parent = EntityManager.GetEntity( parentUid );
            if ( parent == null || parent.Id == 0 )
            {
                return duration;
            }

            using ( var context = new EntityContext() )
            {
                List<EM.Entity_DurationProfile> results = context.Entity_DurationProfile
                        .Where( s => s.EntityId == parent.Id
                        && ( s.TypeId == 3 ) )
                        .ToList();

                if ( results != null && results.Count > 0 )
                {
                    foreach ( EM.Entity_DurationProfile item in results )
                    {
                       
                        MapFromDB( item, duration );
                        break;
                    }
                }
                return duration;
            }
        }
		public static ThisResource Get( int id )
		{
			ThisResource entity = new ThisResource();
			using ( var context = new EntityContext() )
			{
				EM.Entity_DurationProfile item = context.Entity_DurationProfile
							.SingleOrDefault( s => s.Id == id );

				if ( item != null && item.Id > 0 )
				{
					MapFromDB( item, entity );
				}
			}

			return entity;
		}


		public bool ValidateDurationProfile( ThisResource profile, ref bool isEmpty, ref SaveStatus status )
		{
			status.HasSectionErrors = false;
			bool hasConditions = false;
			isEmpty = false;
			//string message = string.Empty;
			if ( string.IsNullOrWhiteSpace( profile.Description ) == false )
			{
				hasConditions = true;
			}
			bool hasExactDuration = false;
			bool hasRangeDuration = false;
			if ( HasDurationItems( profile.ExactDuration ) )
				hasExactDuration = true;
			if ( HasDurationItems( profile.MinimumDuration ) || HasDurationItems( profile.MaximumDuration ) )
				hasRangeDuration = true;

			//validations should be done before here
			if ( hasExactDuration )
			{
				if ( hasRangeDuration )
				{
					//inconsistent, take exact for now
					status.AddWarning( "Error - you must either enter an Exact duration or a Minimum/Maximum range duration but not both. For now, the exact duration was used" );
				}

			}
			else if ( hasRangeDuration == false && hasConditions == false )
			{
				//nothing, 
				status.AddWarning( "Error - you must enter either an Exact duration or a Minimum/Maximum range duration (but not both). <br/>");
				isEmpty = true;
			}
			//if ( !string.IsNullOrWhiteSpace( profile.ProfileName ) && profile.ProfileName.Length > 200 )
			//{
			//	//nothing, 
			//	status.AddError( "Error - the profile name is too long, the maximum length is 200 characters.<br/>" );
			//	isValid = false;
			//	isEmpty = false;
			//}
			if ( !string.IsNullOrWhiteSpace( profile.Description ) && profile.Description.Length > 999 )
			{
				//nothing, 
				status.AddWarning( "Error - the description is too long, the maximum length is 1000 characters.<br/>" );
				isEmpty = false;
			}
			return status.WasSectionValid;
		}
		public static bool HasDurationItems( WM.DurationItem item )
		{
			bool result = false;
			if ( item == null )
				return false;
			//if (!string.IsNullOrWhiteSpace(item.DurationISO8601) && item.DurationISO8601.Substring(0,1) == "P" )
			//{
			//	//eventually need to handle expanding the ISO8601 duration
			//	return true;
			//}

			if ( item.Years > 0
				|| item.Months > 0
				|| item.Weeks > 0
				|| item.Days > 0
				|| item.Hours > 0
				|| item.Minutes > 0
				)
				result = true;

			return result;
		}

		private static bool HasToDurations( DBResource item )
		{
			bool result = false;
			if ( item.ToYears.HasValue
				|| item.ToMonths.HasValue
				|| item.ToWeeks.HasValue
				|| item.ToDays.HasValue
				|| item.ToHours.HasValue
				|| item.ToMinutes.HasValue
				)
				result = true;

			return result;
		}

		private static string DurationSummary( string conditions, WM.DurationItem entity, ref string durationOnly )
		{
			string duration = string.Empty;
			string prefix = string.Empty;
			string comma = string.Empty;
			if ( string.IsNullOrWhiteSpace( conditions ) )
				prefix = "Duration: ";
			else
				prefix = conditions + " ";

			if ( entity.Years > 0 )
			{
				duration += SetLabel( entity.Years, "Year" );
				comma = ", ";
			}
			if ( entity.Months > 0 )
			{
				duration += comma + SetLabel( entity.Months, "Month" );
				comma = ", ";
			}
			if ( entity.Weeks > 0 )
			{
				duration += comma + SetLabel( entity.Weeks, "Week" );
				comma = ", ";
			}
			if ( entity.Days > 0 )
			{
				duration += comma + SetLabel( entity.Days, "Day" );
				comma = ", ";
			}
			if ( entity.Hours > 0 )
			{
				duration += comma + SetLabel( entity.Hours, "Hour" );
				comma = ", ";
			}
			if ( entity.Minutes > 0 )
			{
				duration += comma + SetLabel( entity.Minutes, "Minute" );
				comma = ", ";
			}

			//TODO could replace last comma with And
			int lastPos = duration.LastIndexOf( "," );
			if ( lastPos > 0 )
			{
				duration = duration.Substring( 0, lastPos ) + " and " + duration.Substring( lastPos + 1 );
			}
			durationOnly = duration;
			return prefix + duration;
		}
		static string SetLabel( decimal value, string unit )
		{
			string label = string.Empty;
			if ( value > 1 )
				label = string.Format( "{0} {1}s", ( ( decimal ) value ).ToString( "G29" ), unit );
			else
				label = string.Format( "{0} {1}", ( ( decimal ) value ).ToString( "G29" ), unit );

			return label;
		}
		public static string AsSchemaDuration( WM.DurationItem entity, ref decimal totalMinutes )
		{
			string duration = "P";
			totalMinutes = 0;
			//just check if there an input duration
			if (!string.IsNullOrWhiteSpace( entity.DurationISO8601 ) )
			{
				return entity.DurationISO8601;
			}
			if ( entity.Years > 0 )
			{
				duration += entity.Years.ToString() + "Y";
				totalMinutes += entity.Years * 365 * 24 * 3600;
			}
			if ( entity.Months > 0 )
			{
				duration += entity.Months.ToString() + "M";
				totalMinutes += entity.Months * 30 * 24 * 3600;
			}

			if ( entity.Weeks > 0 )
			{
				duration += entity.Weeks.ToString() + "W";
				totalMinutes += entity.Weeks * 5 * 24 * 3600;
			}

			if ( entity.Days > 0 )
			{
				duration += entity.Days.ToString() + "D";
				totalMinutes += entity.Days * 24 * 3600;
			}

			if ( entity.Hours > 0 || entity.Minutes > 0 )
				duration += "T";

			if ( entity.Hours > 0 )
			{
				duration += entity.Hours.ToString() + "H";
				totalMinutes += entity.Hours * 3600;
			}

			if ( entity.Minutes > 0 )
			{
				duration += entity.Minutes.ToString() + "M";
				totalMinutes += entity.Minutes;
			}
			
			return duration;
		}
		//private string AsSchemaDuration( int years, int mths, int weeks, int days = 0, int hours = 0, int minutes = 0 )
		//{
		//	string duration = "P";

		//	if ( years > 0 )
		//		duration += years.ToString() + "Y";
		//	if ( mths > 0 )
		//		duration += mths.ToString() + "M";
		//	if ( weeks > 0 )
		//		duration += weeks.ToString() + "W";
		//	if ( days > 0 )
		//		duration += days.ToString() + "D";
		//	if ( hours > 0 || minutes > 0 )
		//		duration += "T";

		//	if ( hours > 0 )
		//		duration += hours.ToString() + "H";
		//	if ( minutes > 0 )
		//		duration += minutes.ToString() + "M";
		//	return duration;
		//}
		#endregion
	}
}
