using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RA.Models.JsonV2;

namespace Import.Services.RegistryModels
{
	public class CredentialDescriptionSet : Credential
	{


		public List<Credential> HasCredentials { get; set; } = new List<Credential>();

		public List<Credential> IsPartOfCredentials { get; set; } = new List<Credential>();
	}
}
