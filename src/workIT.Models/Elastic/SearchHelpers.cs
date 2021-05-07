using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Elastic
{
	public class ElasticConfiguration
	{
		public BestFields BestFields { get; set; } = new BestFields();
		public CrossFields CrossFields { get; set; } = new CrossFields();
		public PhrasePrefix PhrasePrefix { get; set; } = new PhrasePrefix();
		public MatchPhrasePrefix MatchPhrasePrefix { get; set; } = new MatchPhrasePrefix();
	}
	/// <summary>
	/// Best Fields ignores the order of the phrase and results are based on anything with the words in the phrase.
	/// </summary>
	public class BestFields
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 10;
		public int NameOrganization { get; set; } = 100;
		public int OwnerOrganizationName { get; set; } = 0;
		public int Description { get; set; } = 5;
		public int AlternateNames { get; set; } = 0;
		public int Keyword { get; set; } = 0;
		public int Occupation { get; set; } = 0;
		public int Industry { get; set; } = 0;
		public int InstructionalPrograms { get; set; } = 25;
		public int QualityAssurancePhrase { get; set; } = 25;

	}

	/// <summary>
	/// Cross Fields looks for each term in any of the fields, which is useful for queries like "nursing ohio"
	/// </summary>
	public class CrossFields
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 5;
		public int CredentialType { get; set; } = 30;
		public int City { get; set; } = 5;
		public int Region { get; set; } = 5;
		public int Country { get; set; } = 0;
	}

	/// <summary>
	/// Phrase Prefix is looking for matches to what a user types in (i.e., the phrase)"
	/// </summary>
	public class PhrasePrefix
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 100;
		public int NameOrganization { get; set; } = 100;
		public int NameAlphanumericOnly { get; set; } = 10;
		//
		public int OwnerOrganizationName { get; set; } = 90;
		public int Description { get; set; } = 50;
		public int AlternateNames { get; set; } = 75;
		public int Keyword { get; set; } = 20;
		public int Occupation { get; set; } = 0;
		public int Industry { get; set; } = 0;
		public int InstructionalPrograms { get; set; } = 25;
	}

	public class MatchPhrasePrefix
	{
		public bool ExcludeQuery { get; set; }
		public int Name { get; set; } = 0;
		public int Keywords { get; set; } = 0;

	}
}
