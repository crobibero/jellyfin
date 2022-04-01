using System;
using System.Collections.Generic;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Static extensions for the <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Determines whether the value is contained in the source collection.
        /// </summary>
        /// <param name="source">An instance of the <see cref="IEnumerable{String}"/> interface.</param>
        /// <param name="value">The value to look for in the collection.</param>
        /// <param name="stringComparison">The string comparison.</param>
        /// <returns>A value indicating whether the value is contained in the collection.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public static bool Contains(this IEnumerable<string> source, ReadOnlySpan<char> value, StringComparison stringComparison)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is IList<string> list)
            {
                int len = list.Count;
                for (int i = 0; i < len; i++)
                {
                    if (value.Equals(list[i], stringComparison))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (string element in source)
            {
                if (value.Equals(element, stringComparison))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the index of the first item matching an expression in an enumerable.
        /// </summary>
        /// <param name="items">The enumerable to search.</param>
        /// <param name="predicate">The expression to test the items against.</param>
        /// <typeparam name="T">The type if item.</typeparam>
        /// <returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(predicate);

            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        /// <summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
        /// <param name="items">The enumerable to search.</param>
        /// <param name="item">The item to find.</param>
        /// <typeparam name="T">The type if item.</typeparam>
        /// <returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item)
        {
            return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i));
        }
    }
}
