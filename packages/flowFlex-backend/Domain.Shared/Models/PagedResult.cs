using System.Collections.Generic;
using System;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Paged result model
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Data items
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Page index (from 1)
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// Has previous page
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Has next page
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;
    }
}