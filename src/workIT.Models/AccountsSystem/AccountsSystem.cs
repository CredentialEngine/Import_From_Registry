using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.AccountsSystem
{
	public class AccountsSystemBase
	{
		public int Id { get; set; }
		public Guid RowId { get; set; }
	}
	//

	public class AccountsSystemUser : AccountsSystemBase
	{
		public string Email { get; set; }
	}
	//

	public class AccountsSystemOrganization : AccountsSystemBase
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string CTID { get; set; }
		public List<string> ApprovedPublishingMethods { get; set; }
		public List<string> ApprovedConsumingMethods { get; set; }
		public List<string> ApprovedPublishingRoles { get; set; }
	}
	//

	public class OrganizationQuery
	{
		public OrganizationQuery()
		{
			OrderBy = SortOrders.Relevance;
			Skip = 0;
			Take = 20;
		}

		public enum SortOrders { Relevance, Name, LastUpdated, Id }

		public string Keywords { get; set; }
		public List<string> CTIDs { get; set; }
		public string StateProvince { get; set; }
		public bool ForCurrentUserOnly { get; set; }
		public List<string> ForUserAccountEmails { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public SortOrders OrderBy { get; set; }
		public string Password { get; set; }

		//Helper function
		public static SortOrders ParseSortOrder( string order )
		{
			try
			{
				return ( SortOrders ) Enum.Parse( typeof( SortOrders ), order, false );
			}
			catch
			{
				return SortOrders.Relevance;
			}
		}
	}
	//

	public class QueryResult<T>
	{
		public List<T> Results { get; set; }
		public int TotalResults { get; set; }
		public bool Valid { get; set; }
		public string Status { get; set; }
	}
	//
}
