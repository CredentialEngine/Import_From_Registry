using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.API
{
    public class DetailRequest
    {

        public int Id { get; set; }
        public int WidgetId { get; set; }
        public bool IsAPIRequest { get; set; }
        public bool SkippingCache { get; set; }
    }
}
