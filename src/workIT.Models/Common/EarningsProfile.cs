using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.QData;

namespace workIT.Models.Common
{
	/// <summary>
	/// Earnings Profile
	/// Entity that describes earning and related statistical information for a given credential.
	/// </summary>
	public class EarningsProfile : OutcomesBaseObject
	{

		/// <summary>
		///  Lower interquartile earnings.
		/// </summary>
		public int LowEarnings { get; set; }

		/// <summary>
		///  Median earnings.
		/// </summary>
		public int MedianEarnings { get; set; }

		/// <summary>
		///  Upper interquartile earnings.
		/// </summary>
		public int HighEarnings { get; set; }

		/// <summary>
		/// Number of months after earning a credential when employment and earnings data is collected.
		/// Number of months usually range between 3 months (one quarter) to ten years.
		/// </summary>
		public int PostReceiptMonths { get; set; }


	}
	public class Entity_EarningsProfile
	{
		public int Id { get; set; }
		public int EntityId { get; set; }
		public int EarningsProfileId { get; set; }
		public System.DateTime Created { get; set; }
		public EarningsProfile EarningsProfile { get; set; } = new EarningsProfile();
	}
}
