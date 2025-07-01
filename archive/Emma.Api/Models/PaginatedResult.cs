using System.Collections.Generic;

namespace Emma.Api.Models
{
    /// <summary>
    /// Represents a paginated result set with metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the result set.</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Gets or sets the items in the current page.
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling(TotalCount / (double)PageSize) : 0;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Creates a new instance of <see cref="PaginatedResult{T}"/>.
        /// </summary>
        public PaginatedResult() { }

        /// <summary>
        /// Creates a new instance of <see cref="PaginatedResult{T}"/> with the specified items and pagination info.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        public PaginatedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }
}
