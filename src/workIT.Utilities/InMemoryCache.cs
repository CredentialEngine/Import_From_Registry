using System;
using System.Runtime.Caching;

namespace workIT.Utilities
{
    public class InMemoryCache : ICacheService
    {
        public T Get<T>( string cacheKey ) where T : class
        {
            return MemoryCache.Default.Get( cacheKey ) as T;
        }

        public T GetOrSet<T>( string cacheKey, Func<T> getItemCallback ) where T : class
        {
            T item = MemoryCache.Default.Get( cacheKey ) as T;
            if ( item == null )
            {
                item = getItemCallback();
                MemoryCache.Default.Add( cacheKey, item, DateTime.Now.AddMinutes( 10 ) );
            }
            return item;
        }

        public bool Remove( string cacheKey )
        {
            object item = MemoryCache.Default.Remove( cacheKey );
            return item != null;
        }

        public T Set<T>( string cacheKey, T item ) where T : class
        {
            if ( item != null )
            {
                MemoryCache.Default.Add( cacheKey, item, DateTime.Now.AddMinutes( 10 ) );
            }

            return item;
        }
    }

    interface ICacheService
    {
        T GetOrSet<T>( string cacheKey, Func<T> getItemCallback ) where T : class;

        T Set<T>( string cacheKey, T item ) where T : class;

        T Get<T>( string cacheKey ) where T : class;

        bool Remove( string cacheKey );
    }
}
