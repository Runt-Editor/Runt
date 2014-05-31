using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Runt.DesignTimeHost.Incomming
{
    public class CompilationSettings
    {
        public int LanguageVersion { get; set; }
        public IEnumerable<string> Defines { get; set; }
        public JObject CompilationOptions { get; set; }
    }
}
