using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Caching;

namespace workIT.Factories
{
    public class ProgressTrackingManager
    {
        private static string GetCacheKey( Guid transactionGUID )
        {
            return "ProgressTracker_" + transactionGUID;
        }
        //

        public static Guid CreateProgressTracker( int totalItemsCount )
        {
            return CreateProgressTracker( Guid.NewGuid(), totalItemsCount );
        }
        public static Guid CreateProgressTracker( Guid transactionGUID, int totalItemsCount )
        {
            var tracker = new ProgressTrackingHelper( transactionGUID, totalItemsCount );
            SaveProgressTracker( tracker );

            return transactionGUID;
        }
        //

        public static ProgressTrackingHelper GetProgressTracker( Guid transactionGUID, bool returnNullIfNotFound = true )
        {
            var key = GetCacheKey( transactionGUID );
            var tracker = MemoryCache.Default.Get( key ) as ProgressTrackingHelper;

            return tracker != null ? tracker : ( returnNullIfNotFound ? null : new ProgressTrackingHelper() );
        }
        //

        /// <summary>
        /// Update a Progress Tracker based on its GUID
        /// </summary>
        /// <param name="transactionGUID">Transaction GUID for the Progress Tracker</param>
        /// <param name="UpdateMethod">Method to update the Progress Tracker</param>
        /// <param name="refreshCacheExpiration">Only set to true if the process has been going long enough that the cache item is nearing its expiration (check the SaveProgressTracker method to see the expiration limit, currently 6 hours)</param>
        /// <returns></returns>
        public static bool UpdateProgressTracker( Guid transactionGUID, Action<ProgressTrackingHelper> UpdateMethod, bool refreshCacheExpiration = false )
        {
            var tracker = GetProgressTracker( transactionGUID );
            if ( tracker != null )
            {
                UpdateMethod( tracker );
                if ( refreshCacheExpiration )
                {
                    //Only necessary if the process runs long enough that the cache time limit might expire. Otherwise, the item is already updated by the previous method.
                    SaveProgressTracker( tracker );
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        //

        public static void DeleteProgressTracker( Guid transactionGUID )
        {
            var key = GetCacheKey( transactionGUID );
            MemoryCache.Default.Remove( key );
        }
        //

        private static void SaveProgressTracker( ProgressTrackingHelper tracker )
        {
            var key = GetCacheKey( tracker.TransactionGUID );
            MemoryCache.Default.Remove( key );
            MemoryCache.Default.Add( key, tracker, new DateTimeOffset( DateTime.Now.AddHours( 6 ) ) ); //If you change the number here, be sure to update the param explanation in the UpdateProgressTracker method
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

            //Get or create the Progress Tracker
            //Doing it this way avoids the need to keep looking it up
            var tracker = GetProgressTracker( transactionGUID, true );
            if ( tracker == null )
            {
                CreateProgressTracker( transactionGUID, items.Count() );
                tracker = GetProgressTracker( transactionGUID, false );
            }

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
        public static void ProcessInParallelAndTrack<T>( List<T> items, Action<T, ProgressTrackingHelper> PerItemHandler, Action<T, ProgressTrackingHelper, Exception> PerItemErrorHandler, Guid transactionGUID )
        {
            //Get or create the Progress Tracker
            //Doing it this way avoids the need to keep looking it up
            var tracker = GetProgressTracker( transactionGUID, true );
            if ( tracker == null )
            {
                CreateProgressTracker( transactionGUID, items.Count() );
                tracker = GetProgressTracker( transactionGUID, false );
            }

            //Use the built-in Parallelization to process the items
            Parallel.ForEach( items, item =>
            {
                try
                {
                    PerItemHandler( item, tracker );
                }
                catch ( Exception ex )
                {
                    PerItemErrorHandler( item, tracker, ex );
                }
            } );
        }
        //

        [Serializable]
        public class ProgressTrackingHelper
        {
            public ProgressTrackingHelper()
            {
                TransactionGUID = Guid.NewGuid();
                Messages = new List<string>();
                Errors = new List<string>();
            }

            public ProgressTrackingHelper( Guid transactionGUID, int totalItems )
            {
                Messages = new List<string>();
                Errors = new List<string>();
                TransactionGUID = transactionGUID;
                TotalItems = totalItems;
            }

            public Guid TransactionGUID { get; set; }
            public int TotalItems { get; set; }
            public int ProcessedItems { get; set; }
            public List<string> Messages { get; set; }
            public List<string> Errors { get; set; }
        }
        //
    }

}
