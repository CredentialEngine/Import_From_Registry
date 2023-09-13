namespace RA.Models.Input
{
	public class BaseRequest 
    {
		/// <summary>
		/// DefaultLanguage is used with Language maps where there is more than one entry for InLanguage, and the user doesn't want to have the first language in the list be the language used with language maps. 
		/// </summary>
		public string DefaultLanguage { get; set; } = "en-US";
        /// <summary>
        /// Identifier for Organization which Owns the data being published
        /// 2017-12-13 - this will be the CTID for the owning org, even if publisher is third party.
        /// </summary>
        public string PublishForOrganizationIdentifier { get; set; }

		/// <summary>
		/// Flag to indicate if the data being published is from a primary source (true) or a secondary source (false)
		/// There would be a risk where if not provided, defaults to false
		/// </summary>
		public bool IsPrimarySourceRecord { get; set; } = true;

		/// <summary>
		/// Envelope Identifier
		/// Optional property, used where the publishing entity wishes to store the identifier.
		/// Contains registry envelope identifier for a document in the registy. It should be empty for a new document. 
		/// </summary>
		public string RegistryEnvelopeId { get; set; }

		/// <summary>
		/// Leave blank for default
		/// </summary>
		public string Community { get; set; }
	}

}
