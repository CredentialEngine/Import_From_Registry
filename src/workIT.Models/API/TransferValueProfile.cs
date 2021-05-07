using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MC = workIT.Models.Common;
using MD = workIT.Models.API;
using ME = workIT.Models.Elastic;
using MPM = workIT.Models.ProfileModels;
using WMS = workIT.Models.Search;

namespace workIT.Models.API
{
	public class TransferValueProfile : BaseDisplay
	{
		public TransferValueProfile()
		{
			EntityTypeId = 26;
			BroadType = "TransferValueProfile";
			CTDLType = "ceterms:TransferValueProfile";
			CTDLTypeLabel = "Transfer Value Profile";
		}
		#region Required 


		/// <summary>
		/// Organization(s) that owns this resource
		/// </summary>
		public List<Outline> OrganizationRole { get; set; } = new List<Outline>();
		#endregion

		/// <summary>
		/// Identifier
		/// Definition:	Alphanumeric token that identifies this resource and information about the token's originating context or scheme.
		/// </summary>	
		public List<IdentifierValue> Identifier { get; set; } = new List<IdentifierValue>();

		public List<TransferValueProfile> DerivedFrom { get; set; } = new List<TransferValueProfile>();

		public WMS.AJAXSettings DevelopmentProcess { get; set; } 

		/// <summary>
		/// Date the validity or usefulness of the information in this resource begins.
		/// </summary>
		public string StartDate { get; set; }

		/// <summary>
		/// Date this assertion ends.
		/// </summary>
		public string EndDate { get; set; }


		/// <summary>
		/// A suggested or articulated credit- or point-related transfer value.
		/// </summary>
		public List<ValueProfile> TransferValue { get; set; } = new List<ValueProfile>();
		public string TransferValueJson { get; set; }
		/// <summary>
		///  Resource that provides the transfer value described by this resource, according to the entity providing this resource.
		/// </summary>
		public WMS.AJAXSettings TransferValueFrom { get; set; }

		/// <summary>
		///  Resource that accepts the transfer value described by this resource, according to the entity providing this resource.
		/// </summary>
		public WMS.AJAXSettings TransferValueFor { get; set; } 

	}
}
