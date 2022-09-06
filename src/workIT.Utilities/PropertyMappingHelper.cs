using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using workIT.Models.Search;

namespace workIT.Utilities
{
	public class PropertyMappingHelper
	{
        public static object CodesManager { get; private set; }

        //Simple automatic mapping for properties of the same name and type for an existing/loaded object
        public static void SimpleUpdate( object input, object output, bool allowOverwritingSkippableValues = false )
		{
			var inputProperties = input.GetType().GetProperties();
			var outputProperties = output.GetType().GetProperties();

			foreach ( var property in inputProperties )
			{
				try
				{

					//Attempt to map embedded objects - probably not ideal, but may be a useful catch-all in some cases
					var matchedProperty = outputProperties.FirstOrDefault( m => m.Name == property.Name );
					if ( matchedProperty != null )
					{
						if ( property.PropertyType.IsClass && property.PropertyType != matchedProperty.PropertyType )
						{
							var item = property.GetValue( input );
							var holder = matchedProperty.GetValue( output );
							SimpleUpdate( item, holder );
						}
						else
						{
							matchedProperty.SetValue( output, property.GetValue( input ) );
						}
					}
				
				}
				catch { }
			}
		}
		//

		//Simple mapping when the only loaded object is the input
		public static T SimpleMap<T>( object input ) where T : new()
		{
			if ( input == null )
			{
				return default( T );
			}

			var result = new T();
			SimpleUpdate( input, result, true );

			return result;
		}
		//

		//Simple mapping for lists
		public static List<T2> SimpleMap<T1, T2>( List<T1> input ) where T2 : new()
		{
			var result = new List<T2>();

			foreach ( var item in input )
			{
				result.Add( SimpleMap<T2>( item ) );
			}

			return result;
		}
		//

		//Get a property from an instance of the class
		public static MemberInfo GetProperty<T>( Expression<Func<T>> property )
		{
			return ( ( MemberExpression )property.Body ).Member;
		}
		//

		//Get a property name as a string from an instance of the class
		public static string GetPropertyName<T>( Expression<Func<T>> property )
		{
			return GetProperty( property ).Name;
		}
		//

		//Stem and tokenize keywords for searching
		public static List<string> TokenizeKeywords( string keywords )
		{
			//Tokenize keywords
			var keywordTokensList = new List<string>();
			foreach ( var item in ( keywords ?? "" ).ToLower().Split( ' ' ) )
			{
				keywordTokensList.Add( item );
				//Stemming
				if ( item.Length > 3 )
				{
					keywordTokensList.Add( item.Substring( 0, item.Length - 1 ) );
				}
				if ( item.Length > 4 )
				{
					keywordTokensList.Add( item.Substring( 0, item.Length - 2 ) );
				}
				if ( item.Length > 5 )
				{
					keywordTokensList.Add( item.Substring( 0, item.Length - 3 ) );
				}
			}
			return keywordTokensList;
		}
		//

		//public static void HandleDateFilters<T>( ref IQueryable<T> data, SearchQuery query ) where T : new()
		//{
		//	AddDateFilter( ref data, "Created", true, query.CreatedStartDate, false ); // data = data.Where( m => m.Created >= query.CreatedStartDate );
		//	AddDateFilter( ref data, "Created", false, query.CreatedEndDate, true ); // data = data.Where( m => m.Created <= query.CreatedEndDate );
		//	AddDateFilter( ref data, "LastUpdated", true, query.LastUpdatedStartDate, false ); // data = data.Where( m => m.LastModified >= query.LastModifiedStartDate );
		//	AddDateFilter( ref data, "LastUpdated", false, query.LastUpdatedEndDate, true ); // data = data.Where( m => m.LastModified <= query.LastModifiedEndDate );
		//}

		public static void AddDateFilter<T>( ref IQueryable<T> data, string propertyName, bool greaterThan, string dateText, bool atEndOfDate )
		{
			try
			{
				var property = typeof( T ).GetProperty( propertyName );
				if ( !string.IsNullOrWhiteSpace( dateText ) && property != null )
				{
					//Get date
					var compareDate = DateTime.Parse( dateText );
					if ( atEndOfDate )
					{
						compareDate = compareDate.AddDays( 1 ).AddSeconds( -1 ); //Ensure a min and max date range of the same date catches everything that happened that day
					}

					//Build expression
					var dataParameter = Expression.Parameter( typeof( T ), "m" ); //m
					var objectDotProperty = Expression.PropertyOrField( dataParameter, property.Name ); // m.PropertyName
					var comparisonValue = Expression.Convert( Expression.Constant( compareDate ), property.PropertyType ); //parsedDate
					var expressionBody = greaterThan ?
						Expression.GreaterThanOrEqual( objectDotProperty, comparisonValue ) : //m.PropertyName >= parsedDate
						Expression.LessThanOrEqual( objectDotProperty, comparisonValue ); //m.PropertyName <= parsedDate

					//Apply expression to query
					data = data.Where( Expression.Lambda<Func<T, bool>>( expressionBody, dataParameter ) ); //data = data.Where( m => m.PropertyName >= parsedDate );
				}
			}
			catch { }
		}

		/// <summary>
		/// from main search autocomplete just need the relationships.
		/// from occupations autocomplete, probably just need the orgId? Or only orgIds with a rel of 30
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public static List<int> GetAnyRelationships( MainSearchInput query, ref List<int> targetOrgIds )
		{
			List<int> relationshipTypeIds = new List<int>();
			//targetOrgIds = new List<int>();
			//NOTE the same method in elastic searches uses CODE!!!!
			foreach ( var filter in query.FiltersV2.Where( m => m.Type == MainSearchFilterV2Types.CUSTOM ).ToList() )
			{
				var item = filter.AsOrgRolesItem();
				//no category. set to 
				if ( item.CategoryId < 1 )
					item.CategoryId = 13; // CodesManager.PROPERTY_CATEGORY_CREDENTIAL_AGENT_ROLE;
				if ( item == null || item.CategoryId < 1 )
					continue;

				if ( item.CategoryId == 13 )
				{
					if ( filter.Name == "organizationroles" )
					{
						//item.Id is the orgId
						relationshipTypeIds.AddRange( item.IdsList );
						targetOrgIds.Add( item.Id );
					}
				}
			}

			return relationshipTypeIds;
		}

	}
}
