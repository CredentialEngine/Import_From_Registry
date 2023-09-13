using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Caching;
using System.Reflection;
using System.Threading;

using Newtonsoft.Json.Linq;

using workIT.Models.Elastic.Tools;
using workIT.Models.API;
using System.Globalization;

namespace workIT.Services
{
	public class ElasticServicesV2
	{
		private static string QueueCacheKey = "Elastic.Tools.Queue";
		private static int MaxQueueItemsInProgress = 3;
		//

		public static string GetQueueItemCacheName( string ctid )
		{
			return QueueCacheKey + ":" + ctid;
		}
		//

		public static List<QueueItem> GetQueue()
		{
			return MemoryCache.Default.Where( m => m.Key.Contains( QueueCacheKey ) ).Select( m => ( QueueItem ) m.Value ).ToList();
		}
		//

		public static void AddOrReplaceQueueItem( string broadType, string ctid, bool triggerProcessing = true )
		{
			AddOrReplaceQueueItem( new QueueItem()
			{
				BroadType = broadType,
				CTID = ctid,
				Added = DateTime.Now,
				Status = QueueItem.StatusType.Waiting
			}, triggerProcessing );
		}
		//

		public static void AddOrReplaceQueueItem( QueueItem item, bool triggerProcessing = true )
		{
			if( item != null )
			{
				//Ensure the item's data is set
				item.Added = item.Added == DateTime.MinValue ? DateTime.Now : item.Added;

				//Remove any existing instances of the item, and add the new one
				RemoveQueueItem( item.CTID );
				MemoryCache.Default.Add( GetQueueItemCacheName( item.CTID ), item, new DateTimeOffset( DateTime.Now.AddDays( 3 ) ) );

				//Trigger processing
				if ( triggerProcessing )
				{
					ThreadPool.QueueUserWorkItem( ( data ) => { ProcessQueue(); } );
				}
			}
		}
		//

		public static void RemoveQueueItem( string ctid )
		{
			MemoryCache.Default.Remove( GetQueueItemCacheName( ctid ) );
		}
		//

		public static void ProcessQueue()
		{
			//Get the queue
			var queue = GetQueue();

			//If the maximum number of items is already being processed, then don't do anything
			if( queue.Where( m => m.Status == QueueItem.StatusType.InProgress ).Count() >= MaxQueueItemsInProgress )
			{
				return;
			}

			//Otherwise, process the next item, or quit if all items are processed
			var nextItem = queue.FirstOrDefault( m => m.Status == QueueItem.StatusType.Waiting );
			if( nextItem == null )
			{
				return;
			}

			//Update the item and allow for parallel processing
			nextItem.Status = QueueItem.StatusType.InProgress;
			nextItem.Started = DateTime.Now;
			AddOrReplaceQueueItem( nextItem );

			//Process the item and catch any errors
			try
			{
				ProcessQueueItem( nextItem );
				nextItem.Status = nextItem.Status == QueueItem.StatusType.Waiting ? QueueItem.StatusType.Success : nextItem.Status;
			}
			catch( Exception ex )
			{
				nextItem.Status = QueueItem.StatusType.Error;
				nextItem.Message = ex.Message + ( ex.InnerException != null ? "; Inner Exception: " + ex.InnerException.Message : "" );
			}

			//Ensure the item gets updated
			nextItem.Finished = DateTime.Now;
			AddOrReplaceQueueItem( nextItem );

			//Process the next item
			ProcessQueue();
		}
		//

		public static void ProcessQueueItem( QueueItem item )
		{
			//Determine which method to use to get the item's data
			var getDataMethod = new List<GetByType>()
			{
				new GetByType( "Assessment", API.AssessmentServices.GetDetailByCtidForAPI ),
				new GetByType( "Collection", API.CollectionServices.GetDetailByCtidForAPI ),
				new GetByType( "CompetencyFramework", API.CompetencyFrameworkServices.GetDetailByCtidForAPI ),
				new GetByType( "ConceptScheme", API.ConceptSchemeServices.GetConceptSchemeOnlyByCTID ),
				new GetByType( "Credential", API.CredentialServices.GetDetailByCtidForAPI ),
				new GetByType( "LearningOpportunity", API.LearningOpportunityServices.GetDetailByCtidForAPI ),
				new GetByType( "Organization", API.OrganizationServices.GetDetailByCtidForAPI ),
				new GetByType( "Pathway", API.PathwayServices.GetDetailByCtidForAPI ),
				new GetByType( "PathwaySet", API.PathwaySetServices.GetDetailByCtidForAPI ),
				new GetByType( "TransferIntermediary", API.TransferIntermediaryServices.GetDetailByCtidForAPI ),
				new GetByType( "TransferValue", API.TransferValueServices.GetDetailByCtidForAPI )
			}.FirstOrDefault( m => item.BroadType.ToLower() == m.Key.ToLower() );

			//Handle the method being missing
			if( getDataMethod == null )
			{
				item.Status = QueueItem.StatusType.Error;
				item.Message = "Unrecognized Type: " + item.BroadType;
				return;
			}

			//Get the data
			var data = getDataMethod.Value( item.CTID, false );
			if( data == null || data.Meta_Id == 0 )
			{
				item.Status = QueueItem.StatusType.Error;
				item.Message = "Unable to find data for CTID: " + item.CTID;
				return;
			}

			//Update the QueueItem
			item.Name = data.Name;
			item.Id = data.Meta_Id ?? 0;
			AddOrReplaceQueueItem( item, false );

			//Flatten the data
			var source = JObject.FromObject( data );
			var flattened = new JObject();
			FlattenObject( source, flattened, "", false );

			//Tag the item with the date it was last refreshed
			flattened.Add( "ElasticRefreshDate", DateTime.UtcNow.ToString( CultureInfo.InvariantCulture.DateTimeFormat.SortableDateTimePattern ) );

			//Update the item in the index
			//Simulate this for now
			System.Threading.Thread.Sleep( 1000 );

			//Finish
			item.Debug = new JObject()
			{
				{ "Flattened", flattened }
			};
			item.Status = QueueItem.StatusType.Success;
		}
		//

		public static void FlattenObject( JObject source, JObject destination, string path, bool forceArray = true )
		{
			//Process properties whose values aren't null or empty
			foreach ( var property in source.Properties().Where( m => m.Value != null && !string.IsNullOrWhiteSpace( m.Value.ToString() ) ).ToList() )
			{
				//Get or create the value holder for the flattened object
				var fullPath = string.IsNullOrWhiteSpace( path ) ? property.Name : path + "-" + property.Name;

				//Handle array value
				if( property.Value.Type == JTokenType.Array )
				{
					//Skip values that are null or empty, and skip the whole array if it contains no values
					var values = ( ( JArray ) property.Value ).Where( m => m != null && !string.IsNullOrWhiteSpace( m.ToString() ) ).ToList();
					if(values.Count() > 0 )
					{
						//For each value in the array, handle it based on whether or not it's an object
						foreach ( var item in values )
						{
							if ( item.Type == JTokenType.Object )
							{
								FlattenObject( ( JObject ) item, destination, fullPath, true );
							}
							else
							{
								AppendValue( destination, item, fullPath, forceArray );
							}
						}
					}
				}
				//Handle object value
				else if( property.Value.Type == JTokenType.Object )
				{
					FlattenObject( ( JObject ) property.Value, destination, fullPath, true );
				}
				//Handle all the other types of value
				else
				{
					AppendValue( destination, property.Value, fullPath, forceArray );
				}
			}
		}
		//

		public static void AppendValue( JObject destination, JToken value, string path, bool forceArray = true )
		{
			//Skip null/empty values
			if ( value == null || string.IsNullOrWhiteSpace( value.ToString() ) )
			{
				return;
			}

			//Force an array for pretty much everything except root level single-value data
			if ( forceArray )
			{
				destination[ path ] = destination[ path ] ?? new JArray();
				( ( JArray ) destination[ path ] ).Add( value );
			}
			else
			{
				destination[ path ] = value;
			}
		}
		//

		private class GetByType : KeyValue<string, Func<string, bool, BaseAPIType>> 
		{ 
			public GetByType( string key, Func<string, bool, BaseAPIType> value )
			{
				Key = key;
				Value = value;
			}
		}
		private class HandleByType : KeyValue<Type, Action<JObject, string, object, List<string>, bool>>
		{
			public HandleByType(Type key, Action<JObject, string, object, List<string>, bool> value )
			{
				Key = key;
				Value = value;
			}
		}
		private class KeyValue<T1, T2>
		{
			public KeyValue() { }

			public KeyValue( T1 key, T2 value )
			{
				Key = key;
				Value = value;
			}

			public T1 Key { get; set; }
			public T2 Value { get; set; }
		}
	}
}
