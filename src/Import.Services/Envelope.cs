﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Import.Services
{
	public class Envelope
	{
		[JsonProperty( PropertyName = "envelope_type" )]
		public string EnvelopeType { get; set; }

		[JsonProperty( PropertyName = "envelope_version" )]
		public string EnvelopeVersion { get; set; }


		/// <summary>
		/// Not used for an add - may be added back later
		/// </summary>
		//[JsonProperty( PropertyName = "envelope_id" )]
		//public string EnvelopeIdentifier { get; set; }

		[JsonProperty( PropertyName = "envelope_community" )]
		public string EnvelopeCommunity { get; set; }

		[JsonProperty( PropertyName = "resource" )]
		public string Resource { get; set; }

		[JsonProperty( PropertyName = "resource_format" )]
		public string ResourceFormat { get; set; }

		[JsonProperty( PropertyName = "resource_encoding" )]
		public string ResourceEncoding { get; set; }

		[JsonProperty( PropertyName = "resource_public_key" )]
		public string ResourcePublicKey { get; set; }
	}
	public class UpdateEnvelope : Envelope
	{
		/// <summary>
		/// NOTE: at this time, the EnvelopeIdentifier is not provided when doing an initial publish (ie. an Add). It is only used for an update, and will contain the envelope identifier returned by the registry from the initial publish.
		/// </summary>
		[JsonProperty( PropertyName = "envelope_id" )]
		public string EnvelopeIdentifier { get; set; }

	}


	public class ReadEnvelope : Envelope
	{
		[JsonProperty( PropertyName = "envelope_id" )]
		public string EnvelopeIdentifier { get; set; }


		[JsonProperty( PropertyName = "decoded_resource" )]
		public object DecodedResource { get; set; }

		//probably don't care about the headers, but include for now
		[JsonProperty( PropertyName = "node_headers" )]
		public NodeHeader NodeHeaders { get; set; }

	}
	public class NodeHeader
	{
		[JsonProperty( PropertyName = "resource_digest" )]
		public string ResourceDigest { get; set; }

		[JsonProperty( PropertyName = "versions" )]
		public List<NodeVersion> NodeVersions { get; set; }

		[JsonProperty( PropertyName = "created_at" )]
		public string CreatedAt { get; set; }

		[JsonProperty( PropertyName = "updated_at" )]
		public string UpdatedAt { get; set; }

		[JsonProperty( PropertyName = "deleted_at" )]
		public string DeletedAt { get; set; }
	}

	public class NodeVersion
	{
		[JsonProperty( PropertyName = "head" )]
		public string head { get; set; }

		[JsonProperty( PropertyName = "event" )]
		public string EventType { get; set; }

		[JsonProperty( PropertyName = "created_at" )]
		public string CreatedAt { get; set; }

		[JsonProperty( PropertyName = "actor" )]
		public string Actor { get; set; }

		[JsonProperty( PropertyName = "url" )]
		public string Url { get; set; }

	}
	public class DeleteObject
	{
		public DeleteObject()
		{
			deleteLabel = "true";
		}
		[JsonProperty( PropertyName = "delete" )]
		public string deleteLabel { get; set; }

		[JsonProperty( PropertyName = "ctld:ctid" )]
		public string Ctid { get; set; }

		[JsonProperty( PropertyName = "deletedBy" )]
		public string Actor { get; set; }

	}

	public class DeleteEnvelope
	{
		//[JsonProperty( PropertyName = "envelope_community" )]
		//public string EnvelopeCommunity { get; set; }


		//[JsonProperty( PropertyName = "envelope_id" )]
		//public string EnvelopeIdentifier { get; set; }

		/// <summary>
		/// No particular value idenified at this time. 
		/// </summary>
		[JsonProperty( PropertyName = "delete_token" )]
		public string DeleteToken { get; set; }

		[JsonProperty( PropertyName = "delete_token_format" )]
		public string ResourceFormat { get; set; }

		[JsonProperty( PropertyName = "delete_token_encoding" )]
		public string ResourceEncoding { get; set; }

		[JsonProperty( PropertyName = "delete_token_public_key" )]
		public string ResourcePublicKey { get; set; }

	}
}
