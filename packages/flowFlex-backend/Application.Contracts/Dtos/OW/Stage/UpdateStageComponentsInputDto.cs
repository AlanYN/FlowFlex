using System.Collections.Generic;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Update Stage components input DTO
    /// </summary>
    public class UpdateStageComponentsInputDto
    {
        /// <summary>
        /// Components list
        /// </summary>
        public List<StageComponent> Components { get; set; } = new List<StageComponent>();
    }
}