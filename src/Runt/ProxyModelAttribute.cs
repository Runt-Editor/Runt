using System;

namespace Runt
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProxyModelAttribute : Attribute
    {
        readonly Type _type;

        public ProxyModelAttribute(Type type)
        {
            _type = type;
        }

        public Type Type
        {
            get { return _type; }
        }
    }
}
