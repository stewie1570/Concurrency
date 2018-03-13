using System.Collections.Generic;
using System.Linq;

namespace Concurrency.Core
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> array, T newNode)
        {
            return array.Concat(new T[] { newNode });
        }
    }
}
