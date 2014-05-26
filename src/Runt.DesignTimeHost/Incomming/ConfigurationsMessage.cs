using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.DesignTimeHost.Incomming
{
    public class ConfigurationsMessage
    {
        public string ProjectName { get; set; }
        public IList<ConfigurationData> Configurations { get; set; }
        public IDictionary<string, string> Commands { get; set; }
    }
}
