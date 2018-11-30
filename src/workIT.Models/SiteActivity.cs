using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models
{
    [Serializable]
    public class SiteActivity : BaseObject
	{
		public SiteActivity()
		{
			CreatedDate = System.DateTime.Now;
			ActivityType = "Audit";
		}
		//public int Id { get; set; }
		public DateTime CreatedDate {
			get { return this.Created; }
			set { this.Created = value; }
		}
		public string DisplayDate
		{
			get 
			{
				if ( Created != null )
				{
					return this.Created.ToString( "yyyy-MM-dd HH.mm.ss" );
				} else 
				return ""; 
			}
		}
		public string ActivityType { get; set; }
		public string Activity { get; set; }
		public string Event { get; set; }
		public string Comment { get; set; }
		public Nullable<int> TargetUserId { get; set; }
		public Nullable<int> ActionByUserId { get; set; }
		public string ActionByUser { get; set; }
		public Nullable<int> ActivityObjectId { get; set; }
		public Nullable<int> ObjectRelatedId { get; set; }
		public string RelatedImageUrl { get; set; }
		public string RelatedTargetUrl { get; set; }
		public Nullable<int> TargetObjectId { get; set; }
		public string SessionId { get; set; }
		public string IPAddress { get; set; }
		public string Referrer { get; set; }
		public Nullable<bool> IsBot { get; set; }
	}
}
