using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Common
{
    [Serializable]
    public class GeoCoordinates : BaseObject
	{
		//public new Guid ParentId { get; set; } //Hides integer ParentId. Should perhaps be called ParentGuid?
		//public int Id { get; set; }
		public GeoCoordinates()
		{
			Name = "";
			ToponymName = "";
			Region = "";
			Country = "";
			//Bounds = new BoundingBox();
			//Address = new Address();  //Do not initialize this here, it will cause an infinite recursive loop with the constructor of GeoCoordinates
		}

		//public int ParentId { get; set; }

		/// <summary>
		/// ID used by GeoNames.org
		/// NOTE: this is not published, so will not be available!
		/// however, it could be extracted, as should be the last part of the Uri
		/// </summary>
		public int GeoNamesId { get; set; } 
		public string Name { get; set; }
        public string Name_Map { get; set; }
        public bool IsException { get; set; }
		public Address Address { get; set; }

		public string ToponymName { get; set; }
		public string Region { get; set; }
		public string Country { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		/// <summary>
		/// URL of a geonames place
		/// </summary>
		public string GeoURI { get; set; } 
		
		public string TitleFormatted 
		{
			get
			{
				string taxName = string.IsNullOrWhiteSpace( this.ToponymName ) ? "" : this.ToponymName;
				if ( !string.IsNullOrWhiteSpace( this.Name ) )
				{
					return this.Name + ( (taxName.ToLower() == this.Name.ToLower() || taxName == "") ? "" : " (" + taxName + ")" );
				}
				else
				{
					return "";

				}
			}
		}
		public string LocationFormatted { get { return string.IsNullOrWhiteSpace( this.Region ) ? this.Country : this.Region + ", " + this.Country; } }

		public string ProfileSummary { get; set; }
		public BoundingBox Bounds { get; set; } = new BoundingBox();

	}
    //

    [Serializable]
    public class BoundingBox
	{
		public bool IsDefined { get { return !( North == 0 && South == 0 && East == 0 && West == 0 ); } }
		public decimal North { get; set; }
		public decimal South { get; set; }
		public decimal East { get; set; }
		public decimal West { get; set; }
	}
	//
}
