﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
	public class WidgetV2 : BaseObject
	{
		public WidgetV2()
		{
			Locations = new LocationSet();
			CredentialFilters = new FilterSet();
			OrganizationFilters = new FilterSet();
			AssessmentFilters = new FilterSet();
			LearningOpportunityFilters = new FilterSet();
			WidgetColors = new List<ColorPair>();
			LabelMaps = new List<LabelMap>();
			InitializeLists( this );
		}

		//Basic Data - Created, Last Updated, Id, and RowId are all inherited from BaseObject
		public string Name { get; set; }
		public string Description { get; set; }
		public string BannerText { get; set; }
		public FileReferenceData LogoImage { get; set; }
		public string UrlName { get; set; }
		public string LogoUrl { get; set; }
		public string LogoFileName { get; set; }
		public string OrganizationName { get; set; }
		public string OrganizationCTID { get; set; }
		public string CustomJSON { get; set; }
		public int CreatedById { get; set; }
		public int LastUpdatedById { get; set; }

		//Location Filters
		public LocationSet Locations { get; set; }

		//Top-Level Object Filter Sets
		public FilterSet CredentialFilters { get; set; }
		public FilterSet OrganizationFilters { get; set; }
		public FilterSet AssessmentFilters { get; set; }
		public FilterSet LearningOpportunityFilters { get; set; }
        public FilterSet CompetencyFrameworkFilters { get; set; }
		public FilterSet PathwayFilters { get; set; } = new FilterSet();
		public FilterSet PathwaySetFilters { get; set; } = new FilterSet();
		public FilterSet TransferValueFilters { get; set; } = new FilterSet();
		public FilterSet TransferIntermediaryFilters { get; set; } = new FilterSet();
		public FilterSet ConceptSchemeFilters { get; set; } = new FilterSet();
		public FilterSet CollectionFilters { get; set; } = new FilterSet();
		public List<string> HideGlobalFilters { get; set; }

        //Features
        public List<string> SearchFeatures { get; set; }
		public string CustomCssUrl { get; set; }
		public List<ColorPair> WidgetColors { get; set; }
		/// <summary>
		/// May need 4 of these? 
		/// But want at the search section level - in FilterSet or just use: FilterSet.PotentialResult list has values?
		/// </summary>
		public bool HasCredentialPotentialResults { get; set; }
		public bool AllowsCSVExport { get; set; } //Allow export/download of search results to a CSV file
		public List<LabelMap> LabelMaps { get; set; }

		//Internal classes
		//Location Set
		public class LocationSet
		{
			public LocationSet()
			{
				Countries = new List<string>();
				Regions = new List<string>();
				Cities = new List<string>();
				InitializeLists( this );
			}
			public List<string> Countries { get; set; }
			public List<string> Regions { get; set; }
			public List<string> Cities { get; set; }
			public bool IsAvailableOnline { get; set; }
			//example an LWIA or EDR
			public List<string> Subregions { get; set; }
			//add specific properties
			public List<string> LWIAs { get; set; }
			public List<string> EDRs { get; set; }
		}
		//

		//Filter Set
		public class FilterSet
		{
			public FilterSet()
			{
				HideFiltersAndItems = new List<SearchFilterAndItems>();
				InitializeLists( this );
			}
			//Relationships
			public List<Reference> OwnedBy { get; set; } //May not be used
			public List<Reference> OfferedBy { get; set; }
			//public List<Reference> AccreditedBy { get; set; }
			//public List<Reference> ApprovedBy { get; set; }
			//public List<Reference> RegulatedBy { get; set; }
			/// <summary>
			/// Used to limit search results to items identified by this list
			/// </summary>
			public List<Reference> PotentialResults { get; set; }
            /// <summary>
            /// Not sure if needed here. Full name of HasCredentialPotentialResults really only applies to credentials. At some point we may start doing the same for other resource types. So a more general name may be needed.
            /// </summary>
            public bool HasPotentialResults { get; set; }
            /// <summary>
            /// Used to limit searches to resources in these collections
            /// </summary>
            public List<Reference> CollectionResults { get; set; }

			// Checkbox Searches
			//public List<string> HideSearches { get; set; }
			//Checkbox Filters
			public List<string> HideFilters { get; set; }
			public List<string> HideCredentialTypeFilters { get; set; }
			public List<SearchFilterAndItems> HideFiltersAndItems { get; set; }
			public OrganizationRole QualityAssurance { get; set; }
			public OrganizationRole Provider { get; set; }
			
			//String Filters
			public List<string> Competencies { get; set; }
			public List<string> Subjects { get; set; }
			public List<string> Keywords { get; set; }
			public List<string> Industries { get; set; }
			public List<string> Occupations { get; set; }
			public List<string> InstructionalProgramTypes { get; set; }
		}
		//

		//Color Pairs
		public class ColorPair
		{
			public string ColorFor { get; set; }
			public string ForegroundColor { get; set; }
			public string BackgroundColor { get; set; }
			public bool UseDefaultForegroundColor { get; set; }
			public bool UseDefaultBackgroundColor { get; set; }
		}
		//

		public class Reference
		{
			public Reference()
			{
				Properties = new Dictionary<string, object>();
			}
			public int Id { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public string CTID { get; set; }
			public Dictionary<string, object> Properties { get; set; }
		}
		//
		
		public class OrganizationRole
		{
			public OrganizationRole()
			{
				RoleIds = new List<int>();
				Organizations = new List<Reference>();
			}
			 public List<int> RoleIds { get; set; }
			 public List<Reference> Organizations { get; set; }

		}
		//

		public class SearchFilterAndItems
		{
			public string Identifier { get; set; }
			public bool Selected { get; set; }
			public List<int> ItemIDs { get; set; }
		}
		//

		public class LabelMap
		{
			public string CTDLType { get; set; }
			public string BroadType { get; set; }
			public string SingleReplace { get; set; }
			public string PluralReplace { get; set; }
			public string SingleLabel { get; set; }
			public string PluralLabel { get; set; }
		}
		//

		//
		//Utility
		public static void InitializeLists( object self )
		{
			var properties = self.GetType().GetProperties();
			foreach( var prop in properties.Where(m => m.PropertyType == typeof( List<string> ) ) )
			{
				prop.SetValue( self, new List<string>() );
			}
			foreach ( var prop in properties.Where( m => m.PropertyType == typeof( List<int> ) ) )
			{
				prop.SetValue( self, new List<int>() );
			}
			foreach ( var prop in properties.Where( m => m.PropertyType == typeof( List<Reference> ) ) )
			{
				prop.SetValue( self, new List<Reference>() );
			}
			foreach ( var prop in properties.Where( m => m.PropertyType == typeof( OrganizationRole ) ) )
			{
				prop.SetValue( self, new OrganizationRole() );
			}
		}
	}
}
