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
            IsSearchabilityAllowed = searchable;
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
        public Enumeration AgentServiceTypes { get; set; }
        public List<CodeItem> PropertiesTotals { get; set; }
        public List<CodeItem> PropertiesTotalsByEntity { get; set; }
        public List<CodeItem> SOC_Groups { get; set; }
        public List<CodeItem> OrgIndustry_Groups { get; set; }
        public List<CodeItem> CredentialIndustry_Groups { get; set; }

        public List<CodeItem> AssessmentCIP_Groups { get; set; }
        public List<CodeItem> LoppCIP_Groups { get; set; }
        public List<CodeItem> GetVocabularyItems( string categorySchema, bool includeEmpty = false )
        {
            if ( categorySchema == "ceterms:AgentServiceType" )
            {
                return AgentServiceTypes.Items.ConvertAll( m => new CodeItem() { Id = m.Id, SchemaName = m.SchemaName, Totals = m.Totals, Name = m.Name, Description = m.Description, CategoryId = m.ParentId, Code = m.CodeId.ToString() } ).Where( m => m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
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

        /// <summary>
        /// first try to use GetStatistics
        /// </summary>
        /// <param name="categorySchema"></param>
        /// <param name="idPrefix"></param>
        /// <param name="tags"></param>
        /// <param name="includeEmpty"></param>
        /// <param name="allowSearchability"></param>
        /// <returns></returns>
        //public List<Statistic> GetStatisticsBySocGroup( string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = false, bool allowSearchability = true )
        //{
        //	return GetVocabularyItems( "ctdl:SocGroup", includeEmpty )
        //		.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
        //		.ToList();
        //}

        public List<Statistic> GetStatisticsByEntity( string entityType, string categorySchema, string idPrefix, List<string> tags, bool includeEmpty = true, bool allowSearchability = true )
        {
            var list = PropertiesTotalsByEntity.Where( m => m.EntityType == entityType && m.CategorySchema == categorySchema && m.Totals > ( includeEmpty ? -1 : 0 ) ).ToList();
            return list.ConvertAll( m => new Statistic( m.Name, m.Description, m.Totals, idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ), tags, m.CategoryId.ToString(), m.Id.ToString(), allowSearchability ) )
                .ToList();
        }

        public Statistic GetSingleStatistic( string schemaName, string idPrefix, List<string> tags, bool allowSearchability, string title = "", string description = "", bool includeEmpty = false )
        {
            var m = PropertiesTotals.Concat( PropertiesTotalsByEntity ).SingleOrDefault( x => x.SchemaName == schemaName.Trim() && x.Totals > ( includeEmpty ? -1 : 0 ) );
            if ( m == null ) return new Statistic();
            else return new Statistic(
                    string.IsNullOrWhiteSpace( title ) ? m.Name : title,
                    string.IsNullOrWhiteSpace( description ) ? m.Description : description,
                    m.Totals,
                    idPrefix + "_" + ( m.SchemaName ?? "" ).Replace( ":", "_" ),
                    tags,
                    m.CategoryId.ToString(),
                    m.SortOrder,
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

}
