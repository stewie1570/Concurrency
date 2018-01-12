using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Concurrency
{
    public class ConcurrentTask
    {
        public static async Task WhenAll(IEnumerable<Func<Task>> tasks, int maxConcurrency)
        {
            IEnumerator<Func<Task>> taskEnumerator = tasks.GetEnumerator();
            await Task.WhenAll(Enumerable
                .Range(start: 1, count: maxConcurrency)
                .Select(i => RunInSeries(taskEnumerator)));
        }

        public static async Task RunInSeries(IEnumerator<Func<Task>> enumerator)
        {
            if (enumerator.MoveNext())
            {
                await enumerator.Current();
                await RunInSeries(enumerator);
            }
        }
    }
}
