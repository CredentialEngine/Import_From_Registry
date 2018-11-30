using Nest;

namespace workIT.Services
{
    public class WidgetQueryFilters
    {
		public QueryContainer OrganizationConnectionQuery { get; set; }
		public QueryContainer LocationQueryFilters { get; set; }


		public QueryContainer OwningOrgsQuery { get; set; }
        public QueryContainer LocationQuery { get; set; }
        public QueryContainer CountryQuery { get; set; }
        public QueryContainer CityQuery { get; set; }
        public QueryContainer KeywordQuery { get; set; }
    }


    public class LocationQueryFilters
    {        
        public QueryContainer LocationQuery { get; set; }
        public QueryContainer CountryQuery { get; set; }
        public QueryContainer CityQuery { get; set; }        
    }
}
