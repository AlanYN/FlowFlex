using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models;

/// <summary>
/// Represents a paginated model for data transfer
/// </summary>
/// <typeparam name="T">The type of data in the page</typeparam>
public class PageModelDto<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the PageModelDto class
    /// </summary>
    /// <param name="pageIndex">The current page number</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="data">The list of items for the current page</param>
    /// <param name="total">The total number of items across all pages</param>
    public PageModelDto(int pageIndex, int pageSize, List<T> data, int total)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        Total = total;
        Data = data;
    }

    /// <summary>
    /// Gets the total number of pages
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

    public int PageCount => TotalPage;

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the total number of items across all pages
    /// </summary>
    public int Total { get; set; }

    public int DataCount => Total;

    /// <summary>
    /// Gets or sets the list of items for the current page
    /// </summary>
    public List<T> Data { get; set; }

    public void Deconstruct(out List<T> datas, out int total)
    {
        datas = Data;
        total = Total;
    }
}
