using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Import.Services.RegistryModels
{
	public class BNodeWrapper
	{
		/// <summary>
		/// An identifier for use with blank nodes
		/// </summary>
		[JsonProperty( "@id" )]
		public string BNodeId { get; set; }

		/// <summary>
		/// the type of the object
		/// </summary>
		[JsonProperty( "@type" )]
		public string Type { get; set; }

		public int EntityTypeId { get; set; }

		/// <summary>
		/// The content for the blank node, example: credential, 
		/// Required.
		/// </summary>
		public object Resource { get; set; }
	}
}
