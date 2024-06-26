using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Caching;
using Newtonsoft.Json.Linq;

namespace workIT.Factories
{
	public class ProgressTrackingManager
	{
		private static string GetCacheKey( Guid transactionGUID )
		{
			return "ProgressTracker_" + transactionGUID;
		}
		//

		/// <summary>
		/// Create a Progress Tracker from a totalItemsCount and return it
		/// </summary>
		/// <param name="totalItemsCount"></param>
		/// <returns></returns>
		public static ProgressTrackingHelper CreateProgressTracker( int totalItemsCount )
		{
			return CreateProgressTracker( Guid.NewGuid(), totalItemsCount );
		}

		/// <summary>
		/// Create a Progress Tracker from a transactionGUID and totalItemsCount and return it
		/// </summary>
		/// <param name="transactionGUID"></param>
		/// <param name="totalItemsCount"></param>
		/// <returns></returns>
		public static ProgressTrackingHelper CreateProgressTracker( Guid transactionGUID, int totalItemsCount )
		{
			var tracker = new ProgressTrackingHelper( transactionGUID, totalItemsCount );
			SaveProgressTracker( tracker, 6 );

			return tracker;
		}
		//

		/// <summary>
		/// Utility method to alter the cache lifetime for a ProgressTracker. Should almost never be needed, but just in case.
		/// </summary>
		/// <param name="transactionGUID"></param>
		/// <param name="lifetimeInHours"></param>
		/// <returns></returns>
		public static ProgressTrackingHelper SetProgressTrackerCacheLifetime( Guid transactionGUID, int lifetimeInHours )
		{
			var tracker = GetProgressTracker( transactionGUID, false, 0 );
			if ( tracker != null )
			{
				SaveProgressTracker( tracker, lifetimeInHours <= 0 ? 1 : lifetimeInHours );
			}

			return tracker;
		}
		//

		/// <summary>
		/// Get (or, optionally, create) a ProgressTrackingHelper from a transactionGUID. A new one will only be created if one is not found for that transactionGUID. If a new one is created, that transactionGUID (and the provided totalItems count) will be assigned to it.
		/// </summary>
		/// <param name="transactionGUID">Transaction GUID to lookup the ProgressTrackingHelper with.</param>
		/// <param name="createNewIfNull">If no ProgressTrackingHelper is found for the provided transactionGUID, one will be created (and assigned that transactionGUID) if "true" is passed to this argument.</param>
		/// <param name="totalItemsForNewTracker">If a new ProgressTrackingHelper is created, this value will be used for its TotalItems property.</param>
		/// <returns></returns>
		public static ProgressTrackingHelper GetProgressTracker( Guid transactionGUID, bool createNewIfNull, int totalItemsForNewTracker )
		{
			var key = GetCacheKey( transactionGUID );
			var tracker = MemoryCache.Default.Get( key ) as ProgressTrackingHelper;

			if ( tracker == null && createNewIfNull )
			{
				tracker = new ProgressTrackingHelper( transactionGUID, totalItemsForNewTracker );
				SaveProgressTracker( tracker );
			}

			return tracker;
		}
		//

		/// <summary>
		/// Reset the tracker's counters for processed, successful, and erroneous items (this will reset the progress bar client-side) and include a message indicating that this phase has been reached.
		/// </summary>
		/// <param name="transactionGUID">Transaction GUID to lookup the ProgressTrackingHelper with.</param>
		/// <param name="createNewIfNull">If no ProgressTrackingHelper is found for the provided transactionGUID, one will be created (and assigned that transactionGUID) if "true" is passed to this argument.</param>
		/// <param name="setTotalItems">Total Items for this phase</param>
		/// <param name="addMessage">Message to add at the start of this phase</param>
		public static ProgressTrackingHelper InitializeProgressTrackerPhase( Guid transactionGUID, bool createNewIfNull, int setTotalItems, string addMessage )
		{
			return UpdateProgressTracker( transactionGUID, createNewIfNull, setTotalItems, tracker => {
				tracker.Messages.Add( addMessage );
				tracker.ProcessedItems = 0;
				tracker.SuccessfulItems = 0;
				tracker.ErroneousItems = 0;
				tracker.TotalItems = setTotalItems;
			} );
		}
		//

		/// <summary>
		/// Update a Progress Tracker based on its GUID. Tracker will be locked to the current thread while the update is happening.
		/// </summary>
		/// <param name="transactionGUID">Transaction GUID for the Progress Tracker</param>
		/// <param name="UpdateMethod">Method to update the Progress Tracker</param>
		/// <returns></returns>
		public static ProgressTrackingHelper UpdateProgressTracker( Guid transactionGUID, bool createNewIfNull, int totalItemsForNewTracker, Action<ProgressTrackingHelper> UpdateMethod )
		{
			var tracker = GetProgressTracker( transactionGUID, createNewIfNull, totalItemsForNewTracker );
			if ( tracker != null )
			{
				tracker.ThreadSafeUpdate( () => UpdateMethod( tracker ) );
				return tracker;
			}
			else
			{
				return null;
			}
		}
		//

		/// <summary>
		/// Immediately remove the ProgressTracker
		/// </summary>
		/// <param name="transactionGUID"></param>
		public static void DeleteProgressTracker( Guid transactionGUID )
		{
			var key = GetCacheKey( transactionGUID );
			MemoryCache.Default.Remove( key );
		}
		//

		/// <summary>
		/// Remove the ProgressTracker after a delay, in order to help ensure any remaining messages are fetched first
		/// </summary>
		/// <param name="transactionGUID"></param>
		/// <param name="seconds"></param>
		public static async void DeleteProgressTrackerAfterDelay( Guid transactionGUID, int milliseconds = 10000 )
		{
			var key = GetCacheKey( transactionGUID );
			await Task.Delay( milliseconds );
			MemoryCache.Default.Remove( key );
			/*
			System.Threading.ThreadPool.QueueUserWorkItem((object data) =>
			{
				System.Threading.Thread.Sleep( milliseconds );
				MemoryCache.Default.Remove( key );
			}, new { } );
			*/
		}
		//

		private static void SaveProgressTracker( ProgressTrackingHelper tracker, int lifetimeInHours = 6 )
		{
			var key = GetCacheKey( tracker.TransactionGUID );
			MemoryCache.Default.Remove( key );
			MemoryCache.Default.Add( key, tracker, new DateTimeOffset( DateTime.Now.AddHours( lifetimeInHours ) ) ); //If you change the number here, be sure to update the param explanation in the UpdateProgressTracker method
		}
		//

		/// <summary>
		/// Get a copy of the current ProgressTrackingHelper and clear the live/real tracker's Messages and Errors. Used to send a copy of the current progress back to the client periodically.
		/// </summary>
		/// <param name="transactionGUID"></param>
		/// <returns></returns>
		public static ProgressTrackingHelper GetProgressForClient( Guid transactionGUID )
		{
			var tracker = GetProgressTracker( transactionGUID, true, 0 );
			return GetProgressForClient( tracker );
		}
		//

		/// <summary>
		/// Get a copy of the current ProgressTrackingHelper and clear the live/real tracker's Messages and Errors. Used to send a copy of the current progress back to the client periodically.
		/// </summary>
		/// <param name="tracker"></param>
		/// <returns></returns>
		public static ProgressTrackingHelper GetProgressForClient( ProgressTrackingHelper tracker )
		{
			//Create the response object
			var returnableTracker = new ProgressTrackingManager.ProgressTrackingHelper()
			{
				TransactionGUID = tracker.TransactionGUID,
				TotalItems = tracker.TotalItems,
				ProcessedItems = tracker.ProcessedItems,
				SuccessfulItems = tracker.SuccessfulItems,
				ErroneousItems = tracker.ErroneousItems,
				LiveData = tracker.LiveData,
				CancelProcessing = tracker.CancelProcessing
			};

			//Lock the active tracker long enough to extract its messages
			lock ( tracker )
			{
				returnableTracker.Messages = tracker.Messages.ToList();
				tracker.Messages.Clear();

				returnableTracker.Errors = tracker.Errors.ToList();
				tracker.Errors.Clear();
			}

			//Return the copied tracker
			return returnableTracker;
		}
		//

		/// <summary>
		/// Process a list of items using System.Threading.Tasks.Parallel()-ization in chunks of a specified size (which may perform better in some situations) and track the progress.<br />
		/// WARNING: This process has been known to occasionally error out(?) on occasion, but work when tried again. It's unclear whether that is the fault of chunking, or if the issue is elsewhere.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items">List of items to process.</param>
		/// <param name="PerItemHandler">Method that processes a single item and updates the tracker.</param>
		/// <param name="PerItemErrorHandler">Method that handles an error relating to a single item and updates the tracker.</param>
		/// <param name="transactionGUID">GUID for the transaction. If no ProgressTracker is found for the provided transactionGUID, one will be created.</param>
		/// <param name="chunkCount">Maximum number of chunks to divide items into. The last chunk will have the remainder of items.</param>
		public static void ProcessInParallelByChunkCountAndTrack<T>( List<T> items, Action<T, ProgressTrackingHelper> PerItemHandler, Action<T, ProgressTrackingHelper, Exception> PerItemErrorHandler, Guid transactionGUID, int chunkCount )
		{
			var chunkSize = ( int ) Math.Ceiling( ( double ) items.Count() / chunkCount );
			ProcessInParallelByChunkSizeAndTrack( items, PerItemHandler, PerItemErrorHandler, transactionGUID, chunkSize );
		}
		//

		/// <summary>
		/// Process a list of items using System.Threading.Tasks.Parallel()-ization in chunks of a specified size (which may perform better in some situations) and track the progress.<br />
		/// WARNING: This process has been known to occasionally error out(?) on occasion, but work when tried again. It's unclear whether that is the fault of chunking, or if the issue is elsewhere.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items">List of items to process.</param>
		/// <param name="PerItemHandler">Method that processes a single item and updates the tracker.</param>
		/// <param name="PerItemErrorHandler">Method that handles an error relating to a single item and updates the tracker.</param>
		/// <param name="transactionGUID">GUID for the transaction. If no ProgressTracker is found for the provided transactionGUID, one will be created.</param>
		/// <param name="chunkSize">Number of items per chunk. The last chunk will have the remainder of items.</param>
		public static void ProcessInParallelByChunkSizeAndTrack<T>( List<T> items, Action<T, ProgressTrackingHelper> PerItemHandler, Action<T, ProgressTrackingHelper, Exception> PerItemErrorHandler, Guid transactionGUID, int chunkSize )
		{
			//Break the workload into chunks
			var chunks = new List<List<T>>();
			var index = 0;
			while ( index < items.Count() )
			{
				chunks.Add( items.Skip( index ).Take( chunkSize ).ToList() );
				index += chunkSize;
			}

			//Get or create the Progress Tracker, and hold onto the reference so we don't need to keep looking it up
			var tracker = GetProgressTracker( transactionGUID, true, items.Count() );

			//Use the built-in Parallelization to process the items
			Parallel.ForEach( chunks, chunk =>
			{
				foreach ( var item in chunk )
				{
					try
					{
						PerItemHandler( item, tracker );
					}
					catch ( Exception ex )
					{
						PerItemErrorHandler( item, tracker, ex );
					}
				}
			} );
		}
		//

		/// <summary>
		/// Process a list of items using System.Threading.Tasks.Parallel()-ization and track the progress.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items">List of items to process.</param>
		/// <param name="PerItemHandler">Method that processes a single item and updates the tracker.</param>
		/// <param name="PerItemErrorHandler">Method that handles an error relating to a single item and updates the tracker.</param>
		/// <param name="transactionGUID">GUID for the transaction. If no ProgressTracker is found for the provided transactionGUID, one will be created.</param>
		public static ProgressTrackingHelper ProcessInParallelAndTrack<T>( Guid transactionGUID, List<T> items, Action<T, ProgressTrackingHelper, long> PerItemHandler, Action<T, ProgressTrackingHelper, long, Exception> PerItemErrorHandler, Action<ProgressTrackingHelper> OnTransactionCanceledHandler, bool autoIncrementItemCounters )
		{
			//Get or create the Progress Tracker, and hold ont othe reference so we don't need to keep looking it up
			var tracker = GetProgressTracker( transactionGUID, true, items.Count() );

			//Use the built-in Parallelization to process the items
			Parallel.ForEach( items, ( item, state, index ) =>
			{
				if ( !tracker.CancelProcessing )
				{
					//Try to call the desired method
					try
					{
						PerItemHandler( item, tracker, index );

						//Increment the success item counter
						if ( autoIncrementItemCounters )
						{
							tracker.ThreadSafeUpdate( () => tracker.SuccessfulItems++ );
							//UpdateProgressTracker( tracker, () => tracker.SuccessfulItems++ );
						}
					}
					//Handle errors
					catch ( Exception ex )
					{
						try
						{
							PerItemErrorHandler( item, tracker, index, ex );
						}
						catch ( Exception errorEx )
						{
							tracker.Errors.Add( "Warning: Error handler for item at index " + index + " resulted in an additional error!" );
						}

						//Increment the error item counter
						if ( autoIncrementItemCounters )
						{
							tracker.ThreadSafeUpdate( () => tracker.ErroneousItems++ );
							//UpdateProgressTracker( tracker, () => tracker.ErroneousItems++ );
						}
					}

					//Increment the processed items regardless of success or error
					if ( autoIncrementItemCounters )
					{
						tracker.ThreadSafeUpdate( () => tracker.ProcessedItems++ );
						//UpdateProgressTracker( tracker, () => tracker.ProcessedItems++ );
					}
				}
			} );

			//If the transaction was canceled, handle it
			if ( tracker.CancelProcessing )
			{
				OnTransactionCanceledHandler( tracker );
			}

			//Return the tracker
			return tracker;
		}

		[Serializable]
		public class ProgressTrackingHelper
		{
			public ProgressTrackingHelper()
			{
				TransactionGUID = Guid.NewGuid();
				Messages = new List<string>();
				Errors = new List<string>();
				LiveData = new JObject();
				CancelProcessing = false;
			}

			public ProgressTrackingHelper( Guid transactionGUID, int totalItems )
			{
				Messages = new List<string>();
				Errors = new List<string>();
				LiveData = new JObject();
				TransactionGUID = transactionGUID;
				TotalItems = totalItems;
				CancelProcessing = false;
			}

			public Guid TransactionGUID { get; set; }
			public int TotalItems { get; set; }
			public int ProcessedItems { get; set; }
			public int SuccessfulItems { get; set; }
			public int ErroneousItems { get; set; }

			public List<string> Errors { get; set; }
			public List<string> Messages { get; set; }

			public JObject LiveData { get; set; }
			public bool CancelProcessing { get; set; }

			/// <summary>
			/// Convenience method to lock the Progress Tracking Helper to the current thread while the UpdateMethod is running
			/// </summary>
			/// <param name="UpdateMethod"></param>
			public void ThreadSafeUpdate( Action UpdateMethod )
			{
				lock ( this )
				{
					UpdateMethod();
				}
			}

		}
		//

		/*
		public class ConcurrentList<T> : System.Collections.Concurrent.ConcurrentQueue<T>
		{
			public void Add( T item )
			{
				this.Enqueue( item );
			}

			public void AddRange( List<T> items )
			{
				foreach( var item in items )
				{
					this.Enqueue( item );
				}
			}
		}
		*/
	}
	//
}
