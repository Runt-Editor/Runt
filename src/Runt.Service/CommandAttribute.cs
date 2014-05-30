using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runt.Service
{
    [AttributeUsage(AttributeTargets.Method)]
    class CommandAttribute : Attribute
    {
        readonly string _name;
        public CommandAttribute(string name)
        {
            Contract.Requires(name != null);
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
