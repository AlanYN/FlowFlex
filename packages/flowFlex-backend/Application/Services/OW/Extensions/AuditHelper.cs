using System;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Application.Services.OW.Extensions
{
    /// <summary>
    /// Helper to apply audit info using OperatorContextService
    /// </summary>
    public static class AuditHelper
    {
        public static void ApplyCreateAudit(EntityBaseCreateInfo entity, IOperatorContextService operatorContext)
        {
            if (entity == null || operatorContext == null) return;
            var name = operatorContext.GetOperatorDisplayName();
            var id = operatorContext.GetOperatorId();
            entity.CreateBy = name;
            entity.ModifyBy = name;
            entity.CreateUserId = id;
            entity.ModifyUserId = id;
            if (entity.CreateDate == default) entity.CreateDate = DateTimeOffset.Now;
            if (entity.ModifyDate == default) entity.ModifyDate = DateTimeOffset.Now;
        }

        public static void ApplyModifyAudit(EntityBaseCreateInfo entity, IOperatorContextService operatorContext)
        {
            if (entity == null || operatorContext == null) return;
            entity.ModifyBy = operatorContext.GetOperatorDisplayName();
            entity.ModifyUserId = operatorContext.GetOperatorId();
            entity.ModifyDate = DateTimeOffset.Now;
        }
    }
}

