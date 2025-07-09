// This file is now moved to Domain.Shared.Models.ApiResponse
// Keep this file for backward compatibility, but use the one in Domain.Shared
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.WebApi.Model.Response
{
    /// <summary>
    /// API response model (legacy location - use FlowFlex.Domain.Shared.Models.ApiResponse instead)
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T> : FlowFlex.Domain.Shared.Models.ApiResponse<T>
    {
    }
}
