using FlowFlex.Domain.Entities.Base;
using Serilog;
using SqlSugar;

namespace FlowFlex.WebApi.Extensions
{
    /// <summary>
    /// Database encryption and decryption extensions
    /// </summary>
    public static class DataBaseExtensions
    {
        private const string STR_CRM = "CRM";
        private const string STR_ConfigAction = "_configAction";

        private static readonly List<string> IgnoreDbAopSetValueHandlerControllers =
        [
            "Unis.CRM.WebApi.Controllers.BNPTransformController",
            "Unis.CRM.WebApi.Controllers.TransformController",
            "Unis.CRM.WebApi.Controllers.Operation.OperationController",
            "Unis.CRM.WebApi.Controllers.zBNPTransformController",
            "Unis.CRM.WebApi.Controllers.zTransformController",
        ];

       
    }
}
