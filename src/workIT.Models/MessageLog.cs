using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models
{
	public partial class MessageLog
	{
		public MessageLog()
		{
			Application = "CredentialFinder";
			MessageType = "Error";
		}
		public int Id { get; set; }
		public System.DateTime Created { get; set; }
		public string DisplayDate
		{
			get
			{
				if ( Created != null )
				{
					return this.Created.ToString( "yyyy-MM-dd HH.mm.ss" );
				}
				else
					return "";
			}
		}
		public string Application { get; set; }
		public string Activity { get; set; }
		public string MessageType { get; set; }
		public string Message { get; set; }
		public string Description { get; set; }
		public int? ActionByUserId { get; set; }
		public string ActivityObjectId { get; set; }
		public string RelatedUrl { get; set; }
		public string SessionId { get; set; }
		public string IPAddress { get; set; }
		public string Tags { get; set; }
	}
}
