using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runt.Core.Model.FileTree;

namespace Runt.Core
{
    static class Utils
    {
        internal static void RegisterChange<T>(JObject change, Expression<Func<T>> property, T newVal, JObject partials)
        {
            Contract.Requires(change != null);
            Contract.Requires(property != null);

            var name = NameOf(property);
            if (name == null)
                return;

            RegisterChange(change, name, newVal, partials);
        }

        internal static void RegisterChange<T>(JObject change, string property, T newVal, JObject partials)
        {
            Contract.Requires(change != null);
            Contract.Requires(property != null);

            JToken subChange;
            if (typeof(T) == typeof(bool))
                subChange = new JValue((bool)(object)newVal);
            else if (newVal is IEnumerable<Entry>)
                subChange = JArray.FromObject(newVal);
            else
                subChange = partials ?? (newVal == null ? null : JObject.FromObject(newVal));
            if (change.Property(property) != null)
                Merge((JObject)change.Property(property).Value, (JObject)subChange);
            else
                change.Add(new JProperty(property, subChange));
        }

        static void Merge(JObject into, JObject from)
        {
            foreach (var prop in from.Properties())
            {
                var toProp = into.Property(prop.Name);
                if (toProp == null)
                    into.Add(prop);
                else
                    Merge((JObject)toProp.Value, (JObject)prop.Value);
            }
        }

        internal static string NameOf<T>(Expression<Func<T>> property)
        {
            var member = GetMemberInfo(property);
            if (member.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                return null;

            var jsonProp = member.GetCustomAttribute<JsonPropertyAttribute>();
            return jsonProp.PropertyName;
        }

        /// <summary>
        /// Converts an expression into a <see cref="MemberInfo"/>.
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <returns>The member info.</returns>
        public static MemberInfo GetMemberInfo(Expression expression)
        {
            var lambda = (LambdaExpression)expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member;
        }
    }
}
