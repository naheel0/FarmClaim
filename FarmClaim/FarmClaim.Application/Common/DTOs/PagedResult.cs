using System;
using System.Collections.Generic;

namespace FarmClaim.Application.Common.DTOs
{
    /// <summary>
    /// Standardized paginated response following Microsoft REST API guidelines
    /// </summary>
    public record PagedResult<T>
    {
        /// <summary>
        /// Collection of items for current page
        /// </summary>
        public IReadOnlyList<T> Items { get; init; } = new List<T>().AsReadOnly();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; init; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; init; }

        /// <summary>
        /// Indicates whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indicates whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// First item's index (0-based) for pagination calculations
        /// </summary>
        public int FirstItemIndex => (PageNumber - 1) * PageSize;

        /// <summary>
        /// Last item's index for pagination calculations
        /// </summary>
        public int LastItemIndex => Math.Min(FirstItemIndex + PageSize - 1, TotalCount - 1);

        /// <summary>
        /// Check if requested page number is valid
        /// </summary>
        public bool IsValidPage => PageNumber >= 1 && PageNumber <= TotalPages;
    }
}