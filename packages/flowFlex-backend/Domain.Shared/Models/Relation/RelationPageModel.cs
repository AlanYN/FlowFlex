using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Relation
{
    /// <summary>
    /// Relation pagination structure
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RelationPageModel<T> where T : class
    {
        public RelationPageModel(int pageIndex, int pageSize, List<T> data, int count)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            Total = count;
            Data = data;
        }

        /// <summary>
        /// Total pages
        /// </summary>
        public int TotalPage
        {
            get
            {
                if (Total > 0)
                {
                    return Total % PageSize == 0 ? Total / PageSize : Total / PageSize + 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int Total { get; set; }

        public List<T> Data { get; set; }
    }
}
