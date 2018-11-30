using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using workIT.Models.Common;

namespace workIT.Models.ProfileModels
{

	public class CostProfile : BaseProfile
	{
		public CostProfile()
		{
			ExpirationDate = ""; 
			Items = new List<CostProfileItem>();
			CurrencyTypes = new Enumeration();
			Region = new List<JurisdictionProfile>();
			Condition = new List<TextValueProfile>();
		}
		public int EntityId { get; set; }

		public Guid ParentUid { get; set; }
		public int ParentTypeId { get; set; }
		public string ParentType { get; set; }
		public Enumeration CurrencyTypes { get; set; }
		public int CurrencyTypeId { get; set; }
		//not persisted, but used for display
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
		public string ExpirationDate { get; set; }
	
		public string CostDetails { get; set; }
		public List<JurisdictionProfile> Region { get; set; }

		public List<CostProfileItem> Items { get; set; }

		public string StartTime { get { return DateEffective; } set { DateEffective = value; } } //Alias used for publishing
		public string EndTime { get { return ExpirationDate; } set { ExpirationDate = value; } } //Alias used for publishing
		public string StartDate { get { return DateEffective; } set { DateEffective = value; } } //Alias used for publishing
		public string EndDate { get { return ExpirationDate; } set { ExpirationDate = value; } } //Alias used for publishing
		public List<TextValueProfile> Condition { get; set; }
	}
	//

	public class CostProfileItem : BaseObject
	{
		public CostProfileItem()
		{
			ProfileName = "";
			DirectCostType = new Enumeration();
			ResidencyType = new Enumeration();
			ApplicableAudienceType = new Enumeration();
		}

		public int CostProfileId
		{
			get { return ParentId; }
			set { this.ParentId = value; }
		}
		/// <summary>
		/// Not persisted, just used for display
		/// </summary>
		public string ProfileName { get; set; }
		public Enumeration DirectCostType { get; set; }
		public int CostTypeId { get; set; }
		public string CostTypeName { get; set; }
		public string CostTypeSchema { get; set; }
		public string PaymentPattern { get; set; }
		public decimal Price { get; set; }

		public Enumeration ResidencyType { get; set; }

		public Enumeration ApplicableAudienceType { get; set; }

		public string ParentEntityType { get; set; }
		public string Currency { get; set; }
		public string CurrencySymbol { get; set; }
    }

    //
    //Used for publishing
    public class CostProfileMerged : BaseProfile
    {
        public CostProfileMerged()
        {
            CostType = new Enumeration();
            ResidencyType = new Enumeration();
            //EnrollmentType = new Enumeration();
            AudienceType = new Enumeration();
            Condition = new List<TextValueProfile>();

        }
        public Enumeration CostType { get; set; }
        public Enumeration ResidencyType { get; set; }
        //public Enumeration EnrollmentType { get; set; }
        public Enumeration AudienceType { get; set; }
        public List<TextValueProfile> Condition { get; set; }

        public string PaymentPattern { get; set; }
        public string Currency { get; set; }
        public string CurrencySymbol { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

        public decimal Price { get; set; }
        public string Name { get { return ProfileName; } set { ProfileName = value; } }
        public string CostDetails { get; set; }

        public static List<CostProfile> ExpandCosts( List<CostProfileMerged> input )
        {
            var result = new List<CostProfile>();

            //First expand each into its own CostProfile with one CostItem
            var holder = new List<CostProfile>();
            foreach ( var merged in input )
            {
                //Create cost profile
                var cost = new CostProfile()
                {
                    ProfileName = merged.Name,
                    Description = merged.Description,
                    Jurisdiction = merged.Jurisdiction,
                    StartTime = merged.StartTime,
                    EndTime = merged.EndTime,
                    StartDate = merged.StartDate,
                    EndDate = merged.EndDate,
                    CostDetails = merged.CostDetails,
                    Currency = merged.Currency,
                    CurrencySymbol = merged.CurrencySymbol,
                    Condition = merged.Condition,
                    Items = new List<CostProfileItem>()
                };
                //If there's any data for a cost item, create one
                if (merged.Price > 0 ||
                    !string.IsNullOrWhiteSpace( merged.PaymentPattern ) ||
                    merged.AudienceType.Items.Count() > 0 ||
                    merged.CostType.Items.Count() > 0 ||
                    merged.ResidencyType.Items.Count() > 0
                    )
                {
                    cost.Items.Add( new CostProfileItem()
                    {
                        ApplicableAudienceType = merged.AudienceType,
                        DirectCostType = merged.CostType,
                        PaymentPattern = merged.PaymentPattern,
                        Price = merged.Price,
                        ResidencyType = merged.ResidencyType
                    } );
                }
                holder.Add( cost );
            }

            //Remove duplicates and hope that pass-by-reference issues don't cause trouble
            while ( holder.Count() > 0 )
            {
                //Take the first item in holder and set it aside
                var currentItem = holder.FirstOrDefault();
                //Remove it from the holder list so it doesn't get included in the LINQ query results on the next line
                holder.Remove( currentItem );
                //Find any other items in the holder list that match the item we just took out
                var matches = holder.Where( m =>
                    m.ProfileName == currentItem.ProfileName &&
                    m.Description == currentItem.Description &&
                    m.CostDetails == currentItem.CostDetails &&
                    m.Currency == currentItem.Currency &&
                    m.CurrencySymbol == currentItem.CurrencySymbol
                ).ToList();
                //For each matching item...
                foreach ( var item in matches )
                {
                    //Take its cost profile items (if it has any) and add them to the cost profile we set aside
                    currentItem.Items = currentItem.Items.Concat( item.Items ).ToList();
                    //Remove the item from the holder so it doesn't get detected again, and so that we eventually get out of this "while" loop
                    holder.Remove( item );
                }
                //Now that currentItem has all of the cost profile items from all of its matches, add it to the result
                result.Add( currentItem );
            }

            return result;
        }
        //
    }

}
