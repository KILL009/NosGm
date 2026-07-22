using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ChickenAPI.DAL.Extensions
{
    public static class DtoExtensions
    {
        public static object GetKey<T>(this T obj) where T : IDto => DtoKeyHelper<T>.KeyProperty.GetValue(obj);

        private class DtoKeyHelper<T>
        {
            public static PropertyInfo KeyProperty { get; }

            static DtoKeyHelper()
            {

                PropertyInfo keyProperty = null;
                foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.GetCustomAttribute<KeyAttribute>() == null)
                    {
                        continue;
                    }

                    if (keyProperty != null)
                    {
                        throw new ArgumentException("You can't have multiple KeyAttribute in your object");
                    }

                    keyProperty = property;
                }

                KeyProperty = keyProperty ?? throw new ArgumentException("Dto should at least contain one");
             
            }
        }
    }
}