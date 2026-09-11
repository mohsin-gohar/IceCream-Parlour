using System.Collections.Generic;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Extension methods for collection operations.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Checks if a collection is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <returns>True if null or empty, false otherwise</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? collection)
        {
            if (collection == null)
                return true;

            using (var enumerator = collection.GetEnumerator())
            {
                return !enumerator.MoveNext();
            }
        }

        /// <summary>
        /// Checks if a collection has any items.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <returns>True if collection has items, false otherwise</returns>
        public static bool HasAny<T>(this IEnumerable<T>? collection)
        {
            return !collection.IsNullOrEmpty();
        }

        /// <summary>
        /// Safely gets the first item from a collection.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to retrieve from</param>
        /// <returns>The first item or default value if collection is empty</returns>
        public static T? SafeFirstOrDefault<T>(this IEnumerable<T>? collection) where T : class
        {
            if (collection == null)
                return null;

            return collection.FirstOrDefault();
        }

        /// <summary>
        /// Batches a collection into groups of specified size.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection</typeparam>
        /// <param name="collection">The collection to batch</param>
        /// <param name="batchSize">The size of each batch</param>
        /// <returns>An enumerable of batches</returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            if (batchSize <= 0)
                throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

            var batch = new List<T>(batchSize);

            foreach (var item in collection)
            {
                batch.Add(item);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
                yield return batch;
        }
    }
}
