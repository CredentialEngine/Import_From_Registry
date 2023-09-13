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
        public QueryContainer RegionQuery { get; set; }
        public QueryContainer CountryQuery { get; set; }
        public QueryContainer CityQuery { get; set; }        
    }

    public class HistoryQueryFilters
    {
        public QueryContainer CreatedFromQuery { get; set; }
        public QueryContainer CreatedToQuery { get; set; }
        public QueryContainer HistoryFromQuery { get; set; }
        public QueryContainer HistoryToQuery { get; set; }
    }
}
