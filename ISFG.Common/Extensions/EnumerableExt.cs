using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.Common.Extensions
{
    public static class EnumerableExt
    {
        #region Static Methods

        public static void ForEach<T>(this IEnumerable<T> input, Action<T> action)
        {
            if (input != null)
                foreach (var item in input)
                    action(item);
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> input, Func<T, Task> action)
        {
            if (input != null)
                foreach (var item in input)
                    await action(item);
        }

        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;

                var subFolder = childSelector(next);
                if (subFolder == null)
                    continue;

                foreach (var child in subFolder)
                    stack.Push(child);
            }
        }

        #endregion
    }
}