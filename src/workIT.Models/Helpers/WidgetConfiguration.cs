using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace workIT.Models.Helpers
{
   public class WidgetConfiguration
    {
        public string Name { get; set; }

        public string LogoUrl { get; set; }
        public string ApiKey { get; set; }
        public string  StyleSheet  { get; set; }

        public bool HideDescription { get; set; }

        public bool HideGrayButtons { get; set; }

    }
}
