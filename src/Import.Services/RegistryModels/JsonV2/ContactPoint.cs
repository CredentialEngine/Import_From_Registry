﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RA.Models.JsonV2
{
    public class ContactPoint
    {
        public ContactPoint()
        {
            PhoneNumbers = new List<string>();
            Emails = new List<string>();
            SocialMediaPages = new List<string>();
            Type = "ceterms:ContactPoint";
            //ContactOption = new List<string>();
        }

        [JsonProperty( PropertyName = "@type" )]
        public string Type { get; set; }

        [JsonProperty( PropertyName = "ceterms:name" )]
        public LanguageMap Name { get; set; }

        /// <summary>
        /// Specification of the type of contact
        /// </summary>
        [JsonProperty( PropertyName = "ceterms:contactType" )]
        public LanguageMap ContactType { get; set; }

		/// <summary>
		/// An option available on this contact point.
		/// For example, a toll-free number or support for hearing-impaired callers.
		/// </summary>
		//[JsonProperty( PropertyName = "ceterms:contactOption" )]
		//public List<string> ContactOption { get; set; }

		[JsonProperty( PropertyName = "ceterms:faxNumber" )]
		public List<string> FaxNumber { get; set; } = new List<string>();

		[JsonProperty( PropertyName = "ceterms:email" )]
		public List<string> Emails { get; set; }

		[JsonProperty( PropertyName = "ceterms:socialMedia" )]
		public List<string> SocialMediaPages { get; set; }

		[JsonProperty( PropertyName = "ceterms:telephone" )]
		public List<string> PhoneNumbers { get; set; }
	}
}
