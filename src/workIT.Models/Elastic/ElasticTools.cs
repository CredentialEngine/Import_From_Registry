using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

namespace workIT.Models.Elastic.Tools
{
	[Serializable]
	public class QueueItem
	{
		public QueueItem()
		{
			Added = DateTime.Now;
			Status = StatusType.Waiting;
		}

		//Infrastructure
		[JsonConverter(typeof(StringEnumConverter))]
		public enum StatusType { Unknown, InProgress, Error, Waiting, Success }
		public StatusType Status { get; set; }
		public string Message { get; set; }
		public DateTime Added { get; set; }
		public DateTime Started { get; set; }
		public DateTime Finished { get; set; }

		//Data
		public string BroadType { get; set; }
		public string CTDLType { get; set; }
		public string Name { get; set; }
		public string CTID { get; set; }
		public int Id { get; set; }
		public JObject Debug { get; set; }

	}
	//

	public class QueueSummary
	{
		public List<QueueItem> Items { get; set; }
		public int TotalItems { get; set; }
		public int TotalWaiting { get; set; }
		public int TotalInProgress { get; set; }
		public int TotalSuccess { get; set; }
		public int TotalError { get; set; }
	}
}
