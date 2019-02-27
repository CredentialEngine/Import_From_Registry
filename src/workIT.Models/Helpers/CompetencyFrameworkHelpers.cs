using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using Newtonsoft.Json.Linq;

namespace workIT.Models.Helpers.CompetencyFrameworkHelpers
{
	public class AsyncItem
	{
		public bool IsInProgress { get; set; }
	}
	//

	public class AsyncItemSet<T> where T : AsyncItem
	{
		public List<T> Items { get; set; }
		public bool AreAllFinished { get { return Items == null || Items.Count() == 0 ? true : Items.Where( m => m.IsInProgress ).Count() == 0; } }
		public void WaitUntilAllAreFinished( int checkFrequency = 10 )
		{
			while ( !AreAllFinished )
			{
				Thread.Sleep( checkFrequency );
			}
		}
	}
	//

	public class FrameworkSearchItem : AsyncItem
	{
		public FrameworkSearchItem()
		{
			CompetencyCTIDs = new List<string>();
			Results = new List<JObject>();
			SkipResults = 0;
			TakeResults = 50;
		}
		public string FrameworkCTID { get; set; }
		public List<string> CompetencyCTIDs { get; set; }
		public List<JObject> Results { get; set; }
		public FrameworkSearchMethod ProcessMethod { get; set; }
		public int SkipResults { get; set; }
		public int TakeResults { get; set; }
		public int TotalResults { get; set; }
		public string ClientIP { get; set; }
	}
	//

	public class RegistryGetItem : AsyncItem
	{
		public string Identifier { get; set; }
		public string Url { get; set; }
		public string Result { get; set; }
	}
	//

	public class FrameworkSearchItemSet : AsyncItemSet<FrameworkSearchItem>
	{
		public FrameworkSearchItemSet()
		{
			Items = new List<FrameworkSearchItem>();
		}
	}
	//

	public delegate List<JObject> FrameworkSearchMethod( List<string> competencyCTIDs, int skip, int take, ref int totalResults, string clientIP );
	//


}
