using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Import.Services.RegistryModels
{
	/// <summary>
	/// Registry Assistant Response
	/// </summary>
	public class RegistryAssistantResponse
	{
		public RegistryAssistantResponse()
		{
			Messages = new List<string>();
			Payload = "";
		}

		/// True if action was successfull, otherwise false
		public bool Successful { get; set; }
		/// <summary>
		/// List of error or warning messages
		/// </summary>
		public List<string> Messages { get; set; }

		public string CTID { get; set; }
		/// <summary>
		/// URL for the registry envelope that contains the document just add/updated
		/// </summary>
		public string EnvelopeUrl { get; set; }
		/// <summary>
		/// URL for the graph endpoint for the document just add/updated
		/// </summary>
		public string GraphUrl { get; set; }
		/// <summary>
		/// Credential Finder Detail Page URL for the document just published (within 30 minutes of publishing)
		/// </summary>
		public string CredentialFinderUrl { get; set; }
		/// <summary>
		/// Identifier for the registry envelope that contains the document just add/updated
		/// </summary>
		public string RegistryEnvelopeIdentifier { get; set; }

		/// <summary>
		/// Payload of request to registry, containing properties formatted as CTDL - JSON-LD
		/// </summary>
		public string Payload { get; set; }
	}
}
