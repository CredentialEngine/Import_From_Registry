using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RA.Models.JsonV2
{
    /// <summary>
    /// Common input class for all condition profiles
    /// </summary>
    public class VerificationServiceProfile
    {
        public VerificationServiceProfile()
        {
            EstimatedCost = new List<CostProfile>();
            Jurisdiction = new List<JurisdictionProfile>();
            //Region = new List<GeoCoordinates>();
            OfferedBy = new List<string>();
            VerifiedClaimType = new List<CredentialAlignmentObject>();
            VerificationDirectory = new List<string>();
            VerificationService = new List<string>();
            TargetCredential = new List<string>();
            Type = "ceterms:VerificationServiceProfile";
        }

        [JsonProperty( "@type" )]
        public string Type { get; set; }

        /// <summary>
        /// URI
        /// </summary>
        [JsonProperty( "@id" )]
        public string CtdlId { get; set; }

        /// <summary>
        /// Globally unique Credential Transparency Identifier (CTID) by which the creator, owner or provider of a resource recognizes it in transactions with the external environment (e.g., in verifiable claims involving the resource).
        /// required
        /// <see cref="https://credreg.net/ctdl/terms/ctid"/>
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:ctid" )]
        public string CTID { get; set; }

        [JsonProperty( PropertyName = "ceterms:description" )]
        public LanguageMap Description { get; set; }

        [JsonProperty( PropertyName = "ceterms:dateEffective" )]
        public string DateEffective { get; set; }

		[JsonProperty( PropertyName = "ceterms:subjectWebpage" )]
		public string SubjectWebpage { get; set; }

		[JsonProperty( PropertyName = "ceterms:estimatedCost" )]
        public List<CostProfile> EstimatedCost { get; set; }

        [JsonProperty( PropertyName = "ceterms:holderMustAuthorize" )]
        public bool? HolderMustAuthorize { get; set; }

        [JsonProperty( PropertyName = "ceterms:targetCredential" )]
        public List<string> TargetCredential { get; set; }

        [JsonProperty( PropertyName = "ceterms:verificationDirectory" )]
        public List<string> VerificationDirectory { get; set; }

        [JsonProperty( PropertyName = "ceterms:verificationMethodDescription" )]
        public LanguageMap VerificationMethodDescription { get; set; }

        [JsonProperty( PropertyName = "ceterms:verificationService" )]
        public List<string> VerificationService { get; set; }

        [JsonProperty( PropertyName = "ceterms:verifiedClaimType" )]
        public List<CredentialAlignmentObject> VerifiedClaimType { get; set; }

        [JsonProperty( PropertyName = "ceterms:offeredBy" )]
        public List<string> OfferedBy { get; set; }

        [JsonProperty( PropertyName = "ceterms:jurisdiction" )]
        public List<JurisdictionProfile> Jurisdiction { get; set; }

        [JsonProperty( PropertyName = "ceterms:offeredIn" )]
        public List<JurisdictionProfile> OfferedIn { get; set; }
    }
}
