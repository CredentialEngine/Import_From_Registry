using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.JsonV2
{
    /// <summary>
    /// Class to represent a LanguageMap and related components
    /// </summary>
    public class LanguageMap : Dictionary<string, string>
    {
        public LanguageMap() { }
        public LanguageMap( string text )
        {
            this.Add( "en", text );
        }
        public LanguageMap( string languageCode, string text )
        {
            this.Add( languageCode, text );
        }

        public override string ToString()
        {
            //if nothing found for default, should return first one
            string value = ToString( "en" );
            if ( string.IsNullOrWhiteSpace( value ) )
            {
                value = this.FirstOrDefault().Value;
            } 
            return value;
        }
        public string ToString( string languageCode )
        {
            return LanguageMap.ToString( this, languageCode );
        }
        public static string ToString( LanguageMap map, string languageCode )
        {
            //Fast check
            if ( map.ContainsKey( languageCode ) )
            {
                return map[ languageCode ];
            }

            //Search
            var parts = languageCode.ToLower().Split( '-' ).ToList();
            while ( parts.Count() > 0 )
            {
                var match = map.Keys.FirstOrDefault( m => m.ToLower() == string.Join( "-", parts ) );
                if ( match != null )
                {
                    return map[ match ];
                }
                var closeMatch = map.Keys.FirstOrDefault( m => m.ToLower().Contains( string.Join( "-", parts ) ) );
                if ( closeMatch != null )
                {
                    return map[ closeMatch ];
                }
                parts.Remove( parts.Last() );
            }

            //Default
            return "";
        }
    }

    public class LanguageMapList : Dictionary<string, List<string>>
    {
        public LanguageMapList() { }
        public LanguageMapList( List<string> items )
        {
            this.Add( "en", items );
        }
        public LanguageMapList( string languageCode, List<string> items )
        {
            this.Add( languageCode, items );
        }

        public List<string> ToList()
        {
            return ToList( "en" );
        }
        public List<string> ToList( string languageCode )
        {
            return LanguageMapList.ToList( this, languageCode );
        }
        public static List<string> ToList( LanguageMapList list, string languageCode )
        {
            //Fast check
            if ( list.ContainsKey( languageCode ) )
            {
                return list[ languageCode ];
            }

            //Search
            var parts = languageCode.ToLower().Split( '-' ).ToList();
            while ( parts.Count() > 0 )
            {
                var match = list.Keys.FirstOrDefault( m => m.ToLower() == string.Join( "-", parts ) );
                if ( match != null )
                {
                    return list[ match ];
                }
                var closeMatch = list.Keys.FirstOrDefault( m => m.ToLower().Contains( string.Join( "-", parts ) ) );
                if ( closeMatch != null )
                {
                    return list[ closeMatch ];
                }
                parts.Remove( parts.Last() );
            }

            //Default
            return new List<string>();
        }
    }
}
