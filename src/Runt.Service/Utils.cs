using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Runt.Service
{
    public class Utils
    {
        public static Func<T, Tuple<T, JObject>> Update<T>(Func<T, JObject, T> update)
            where T : class
        {
            return value =>
            {
                var change = new JObject();
                if (value == null)
                    return new Tuple<T, JObject>(null, change);

                var newValue = update(value, change);
                Cull(change);
                if (change.Count == 0)
                    change = null;
                return new Tuple<T, JObject>(newValue, change);
            };
        }

        static void Cull(JObject obj)
        {
            foreach (var prop in obj.Properties())
            {
                JObject o = prop.Value as JObject;
                if (o != null)
                    Cull(o);
            }

            if (obj.Properties().All(p => p.Value is JObject && ((JObject)p.Value).Count == 0))
                obj.RemoveAll();
        }
    }
}
