using System;
using System.Reflection;

namespace Utils.General
{
    /// <summary>
    /// Attribute to specify an ID property of a row type.
    /// Property type must be string and a row type must contain exactly one ID property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class StupidDbIdAttribute : Attribute
    {
        public static PropertyInfo FindIdProperty<T>()
        {
            var type = typeof(T);
            var idPropertyInfo = (PropertyInfo) null;
            foreach (var propertyInfo in type.GetProperties())
            {
                var idAttrFound = false;
                foreach (var attr in propertyInfo.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(StupidDbIdAttribute))
                    {
                        if (propertyInfo.PropertyType != typeof(string))
                        {
                            throw new Exception($"Type \"{type}\" contains an invalid ID property. ID must be of string");
                        }

                        idAttrFound = true;
                        break;
                    }
                }

                if (idAttrFound)
                {
                    if (idPropertyInfo != null)
                    {
                        throw new Exception($"Type \"{type}\" contains multiple ID properties");
                    }

                    idPropertyInfo = propertyInfo;
                }
            }

            if (idPropertyInfo == null)
            {
                throw new Exception($"Type \"{type}\" does not contain any ID properties");
            }

            return idPropertyInfo;
        }
    }
}