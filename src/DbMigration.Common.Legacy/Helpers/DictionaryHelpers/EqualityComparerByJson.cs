using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace DbMigration.Common.Legacy.Helpers.DictionaryHelpers
{
    public class EqualityComparerByJson<T> : IEqualityComparer<T> where T : class
    {
        private readonly PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

        public bool Equals(T x, T y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            //Use your favourite serializer.
            var xHashCode = JsonSerializer.Serialize(x).GetHashCode();
            var yHashCode = JsonSerializer.Serialize(y).GetHashCode();
            return xHashCode == yHashCode;
        }
        public int GetHashCode(T obj)
        {
            var hashCode = 0;
            foreach (var propertyInfo in _properties)
            {
                //Property is collection  Equal can catch differences.
                if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.PropertyType != typeof(string)) continue;
                //Property is object (class) Equal can catch differences.
                if (!propertyInfo.PropertyType.IsValueType && propertyInfo.PropertyType != typeof(string)) continue;
                var propertyValue = propertyInfo.GetValue(obj, null);
                hashCode += propertyValue == null ? 0 : propertyValue.GetHashCode();
            }
            return hashCode;
        }
    }

}
