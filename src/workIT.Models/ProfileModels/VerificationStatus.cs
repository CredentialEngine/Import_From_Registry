using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.ProfileModels
{
    [Serializable]
    public class VerificationStatus : BaseProfile
	{

		public string Name
		{
			get { return ProfileName; }
			set { ProfileName = value; }
		}

		public string URL { get; set; }

	}
}
