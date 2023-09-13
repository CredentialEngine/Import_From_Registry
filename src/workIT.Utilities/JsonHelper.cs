using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace workIT.Utilities
{
    public static class JsonHelper
    {
        /// <summary>
        /// Get a JSONResult from an input object. Will return with JsonRequestBehavior set to AllowGet and MaxJsonLength set to Int32.MaxValue.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static JsonResult GetRawJson( object input )
        {
            var result = new JsonResult();
            result.Data = input;
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.MaxJsonLength = Int32.MaxValue;
            return result;
        }

        /// <summary>
        /// Get a JSONResult from an input object with wrapper to help with client-side error handling.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="valid"></param>
        /// <param name="status"></param>
        /// <param name="extra"></param>
        /// <returns></returns>
        public static JsonResult GetJsonWithWrapper( object input, bool valid, string status, object extra )
        {
            var data = new
            {
                data = input,
                valid = valid,
                status = status,
                extra = extra
            };
            return GetRawJson( data );
        }
        public static JsonResult GetJsonWithWrapper( object input )
        {
            return GetJsonWithWrapper( input, true, "okay", null );
        }
        /// <summary>
		/// Generic handling of Json object - especially for unexpected types
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static Dictionary<string, object> JsonToDictionary( string json )
        {
            var result = new Dictionary<string, object>();
            var obj = JObject.Parse( json );
            foreach ( var property in obj )
            {
                result.Add( property.Key, JsonToObject( property.Value ) );
            }
            return result;
        }
        public static object JsonToObject( JToken token )
        {
            switch ( token.Type )
            {
                case JTokenType.Object:
                {
                    return token.Children<JProperty>().ToDictionary( property => property.Name, property => JsonToObject( property.Value ) );
                }
                case JTokenType.Array:
                {
                    var result = new List<object>();
                    foreach ( var obj in token )
                    {
                        result.Add( JsonToObject( obj ) );
                    }
                    return result;
                }
                default:
                {
                    return ( ( JValue )token ).Value;
                }
            }
        }

        public static JsonSerializerSettings GetJsonSettings( bool doingFormating = true )
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new EmptyNullResolver(),
                Formatting = doingFormating ? Formatting.Indented : Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return settings;
        }
		public static JsonSerializerSettings GetJsonSettingsAll( bool doingFormating = true )
		{
			var settings = new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Include,
				ContractResolver = new EmptyNullResolver(),
				Formatting = doingFormating ? Formatting.Indented : Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			return settings;
		}
		//Force properties to be serialized in alphanumeric order
		public class AlphaNumericContractResolver : DefaultContractResolver
        {
            protected override System.Collections.Generic.IList<JsonProperty> CreateProperties( System.Type type, MemberSerialization memberSerialization )
            {
                return base.CreateProperties( type, memberSerialization ).OrderBy( m => m.PropertyName ).ToList();
            }
        }

		//change to this, so original order is maintained
		public class EmptyNullResolver : DefaultContractResolver
		//public class EmptyNullResolver : AlphaNumericContractResolver
        {
            protected override JsonProperty CreateProperty( MemberInfo member, MemberSerialization memberSerialization )
            {
                var property = base.CreateProperty( member, memberSerialization );
                var isDefaultValueIgnored = ( ( property.DefaultValueHandling ?? DefaultValueHandling.Ignore ) & DefaultValueHandling.Ignore ) != 0;

                if ( isDefaultValueIgnored )
                    if ( !typeof( string ).IsAssignableFrom( property.PropertyType ) && typeof( IEnumerable ).IsAssignableFrom( property.PropertyType ) )
                    {
                        Predicate<object> newShouldSerialize = obj =>
                        {
                            var collection = property.ValueProvider.GetValue( obj ) as ICollection;
                            return collection == null || collection.Count != 0;
                        };
                        Predicate<object> oldShouldSerialize = property.ShouldSerialize;
                        property.ShouldSerialize = oldShouldSerialize != null ? o => oldShouldSerialize( oldShouldSerialize ) && newShouldSerialize( oldShouldSerialize ) : newShouldSerialize;
                    }
                    else if ( typeof( string ).IsAssignableFrom( property.PropertyType ) )
                    {
                        Predicate<object> newShouldSerialize = obj =>
                        {
                            var value = property.ValueProvider.GetValue( obj ) as string;
                            return !string.IsNullOrEmpty( value );
                        };

                        Predicate<object> oldShouldSerialize = property.ShouldSerialize;
                        property.ShouldSerialize = oldShouldSerialize != null ? o => oldShouldSerialize( oldShouldSerialize ) && newShouldSerialize( oldShouldSerialize ) : newShouldSerialize;
                    }
                return property;
            }
        }
    }
}