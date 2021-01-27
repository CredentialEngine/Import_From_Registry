using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models;
using workIT.Models.Common;

namespace workIT.Models.Helpers.Reports
{
	public class Statistic
	{
		public Statistic()
		{
			IsSearchabilityAllowed = true;
		}
		public Statistic( string title, string description, int value, string id, List<string> tags = null )
		{
			Title = title;
			Description = description;
			Value = value;
			Id = id;
			Tags = ( tags ?? new List<string>() );
			CategoryId = "";
			CodeId = "";
			IsSearchabilityAllowed = true;
		}
		public Statistic( string title, string description, int value, string id, List<string> tags = null, string categoryID = "", string codeID = "", bool searchable = true )
		{

			Title = title;
			Description = description;
			Value = value;
			Id = id;
			Tags = ( tags ?? new List<string>() );
			CategoryId = categoryID ?? "";
			CodeId = codeID ?? "";
			if ( categoryID == "15" && ( codeID == "1" || codeID == "2" ) )
			{
				IsSearchabilityAllowed = false;
			}
			else
			{
				IsSearchabilityAllowed = searchable;
			}

		}
		public Statistic( string title, string description, int value, string id, List<string> tags = null, string categoryID = "", int primarySortOrder = 0, string codeID = "", bool searchable = true )
		{
			Title = title;
			Description = description;
			Value = value;
			Id = id;
			Tags = ( tags ?? new List<string>() );
			CategoryId = categoryID ?? "";
			PrimarySortOrder = primarySortOrder;
			CodeId = codeID ?? "";
			IsSearchabilityAllowed = searchable;
		}
		public string Title { get; set; }
		public string Description { get; set; }
		public int PrimarySortOrder { get; set; } = 0;
		public int Value { get; set; }
		public string Id { get; set; }
		public List<string> Tags { get; set; } = new List<string>();
		public string CategoryId { get; set; } //Category ID to be fed to the search as a filter
		public string CodeId { get; set; } //Code ID to be fed to the search as a filter
		public bool IsSearchabilityAllowed { get; set; } //Master switch for enabling/disabling search link for this statistic
	}

	public class CommonTotals
	{
		public CommonTotals()
		{
			MainEntityTotals = new List<CodeItem>();
			PropertiesTotals = new List<CodeItem>();
			PropertiesTotalsByEntity = new List<CodeItem>();
			AgentServiceTypes = new Enumeration();
		}
		public List<HistoryTotal> CredentialHistory { get; set; } = new List<HistoryTotal>();
		public List<HistoryTotal> OrganizationHistory { get; set; } = new List<HistoryTotal>();
		public List<HistoryTotal> AssessmentHistory { get; set; } = new List<HistoryTotal>();
		public List<HistoryTotal> LearningOpportunityHistory { get; set; } = new List<HistoryTotal>();
		/// <summary>
		/// All with status of < 3
		/// </summary>
		public int TotalOrganizations { get; set; }
		/// <summary>
		/// Need means to identify a partner org
		/// Scarlett updated partner list to include the orgId. This will require regular imports.
		/// </summary>
		public int TotalPartnerOrganizations { get; set; }
		public int TotalOtherOrganizations { get; set; }
		public int TotalQAOrganizations { get; set; }

		/// <summary>
		/// Credentials identified by partners to be entered
		/// </summary>
		public int TotalPartnerCredentials { get; set; }
		public int TotalEnteredCredentials { get; set; }

		/// <summary>
		/// Should be TotalPartnerCredentials - TotalEnteredCredentials
		/// if the latter can be accurately determined
		/// </summary>
		public int TotalPendingCredentials { get; set; }
		/// <summary>
		/// not sure
		/// </summary>
		public int TotalDirectCredentials { get; set; }
		/// <summary>
		/// Credentials entered as connections
		/// </summary>
		public int TotalOtherCredentials { get; set; }
		public int TotalCredentialsAtCurrentCtdl { get; set; }
		public int TotalCredentialsToBeUpdatedToCurrentCtdl { get; set; }

		public List<CodeItem> MainEntityTotals { get; set; }
		public List<CodeItem> EntityRegionTotals { get; set; } = new List<CodeItem>();
		public Enumeration AgentServiceTypes { get; set; }
		public List<CodeItem> PropertiesTotals { get; set; }
		public List<CodeItem> PropertiesTotalsByEntity { get; set; }
		public List<CodeItem> SOC_Groups { get; set; }
		public List<CodeItem> OrgIndustry_Groups { get; set; }
		public List<CodeItem> CredentialIndustry_Groups { get; set; }
		//
		public List<CodeItem> CredentialCIP_Groups { get; set; }
		public List<CodeItem> AssessmentCIP_Groups { get; set; }
		public List<CodeItem> LoppCIP_Groups { get; set; }
		//public Enumeration CredentialStatusType { get; set; }
		public List<CodeItem> GetVocabularyItems( string categorySchema, bool includeEmpty = false )
		{
			if ( categorySchema == "ceterms:AgentServiceType" )
			{
				return AgentServiceTypes.Items.ConvertAll( m => new CodeItem() { Id = m.Id, SchemaName = m.SchemaName, Totals = m.Totals, Name = m.Name, Description = m.Description, CategoryId = m.ParentId, Code = m.CodeId.ToString() } ).Where( m => m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			}
			else if ( categorySchema == "ceterms:PathwayComponentType" )
			{
				return PropertiesTotals.Where( m => m.ParentSchemaName == categorySchema && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			}
			else
			{
				return PropertiesTotals.Where( m => m.CategorySchema == categorySchema && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			}
		}

		public List<Statistic> GetStatistics( string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = false, bool allowSearchability = true )
		{
			return GetVocabularyItems( categorySchema, includeEmpty )
				.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
				.ToList();
		}




		public List<Statistic> GetStatisticsByEntity( int entityTypeId, string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = true, bool allowSearchability = true )
		{
			var list = PropertiesTotalsByEntity.Where( m => m.EntityTypeId == entityTypeId && m.CategorySchema == categorySchema && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			return list.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
				.ToList();
		}
		public List<Statistic> GetStatisticsByEntity( string entityType, string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = true, bool allowSearchability = true )
		{
			var list = PropertiesTotalsByEntity.Where( m => m.EntityType == entityType && m.CategorySchema == categorySchema && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			return list.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
				.ToList();
		}
		public List<Statistic> GetStatisticsByEntityRegion( int entityTypeId, string country, string idPrefix, List<string> tags, bool includeEmpty = true, bool allowSearchability = true )
		{
			var list = EntityRegionTotals.Where( m => m.EntityTypeId == entityTypeId && m.CodeGroup == country && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
			return list.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
				.ToList();
		}
		public List<Statistic> GetHistory( string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = false, bool allowSearchability = true )
		{
			return GetVocabularyItems( categorySchema, includeEmpty )
				.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
				.ToList();
		}

		public Statistic GetSingleStatistic( string schemaName, string idPrefix, List<string> tags, bool allowSearchability, string title = "", string description = "", bool includeEmpty = false, int overrideSortOrder = -1 )
		{
			if (schemaName == "frameworkReport:Competencies" )
			{

			}
			else if ( schemaName == "frameworkReport:PathwayComponents" )
			{

			}
			var m = PropertiesTotals.Concat( PropertiesTotalsByEntity ).FirstOrDefault( x => x.SchemaName == schemaName.Trim() && x.Totals > ( includeEmpty ? -1 : 0 ) );
			if ( m == null ) return new Statistic();
			else return new Statistic(
					string.IsNullOrWhiteSpace( title ) ? m.Name : title,
					string.IsNullOrWhiteSpace( description ) ? m.Description : description,
					m.Totals,
					idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ),
					tags,
					m.CategoryId.ToString(),
					overrideSortOrder > - 1 ? overrideSortOrder : m.SortOrder,
					m.Id.ToString(),
					allowSearchability
				);
			//return ( PropertiesTotals.Concat( PropertiesTotalsByEntity ) ).Where( m => m.SchemaName == schemaName && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList()
			//    .ConvertAll( m => new Statistic(
			//        string.IsNullOrWhiteSpace( title ) ? m.Name : title,
			//        string.IsNullOrWhiteSpace( description ) ? m.Description : description,
			//        m.Totals,
			//        idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ),
			//        tags,
			//        m.CategoryId.ToString(),
			//        m.SortOrder,
			//        m.Id.ToString(),
			//        allowSearchability
			//    ) )
			//    .FirstOrDefault() ?? new Statistic();

			// return single;
		}
		//public Statistic GetSingleStatisticViaParentSchema(string schemaName, string idPrefix, List<string> tags, bool allowSearchability, string title = "", string description = "")
		//{
		//    return (PropertiesTotals.Concat(PropertiesTotalsByEntity)).Where(m => m.ParentSchemaName == schemaName).ToList()
		//        .ConvertAll(m => new Statistic(
		//           string.IsNullOrWhiteSpace(title) ? m.Name : title,
		//           string.IsNullOrWhiteSpace(description) ? m.Description : description,
		//           m.Totals,
		//           idPrefix + "_" + (m.SchemaName ?? "").Replace(":", "_"),
		//           tags,
		//           m.CategoryId.ToString(),
		//           m.Id.ToString(),
		//           allowSearchability
		//       ))
		//        .FirstOrDefault() ?? new Statistic();
		//}
	}
	public class HistoryTotal

	{
		public HistoryTotal()
		{

		}
		public HistoryTotal( string description )
		{
			Description = description;
		}
		public DateTime Period { get; set; }
		public int EntityTypeId { get; set; }
		public int CreatedCount { get; set; }
		public int UpdatedCount { get; set; }
        public int DeletedCount { get; set; }
		public string Description { get; set; }

	}
}
