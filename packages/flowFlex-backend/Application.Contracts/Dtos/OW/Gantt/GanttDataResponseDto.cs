using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Gantt
{
    /// <summary>
    /// Gantt chart data response — contains case-level summary and all stage items
    /// </summary>
    public class GanttDataResponseDto
    {
        /// <summary>
        /// Case-level summary information
        /// </summary>
        public GanttCaseSummaryDto Summary { get; set; }

        /// <summary>
        /// All stage items ordered by stageOrder ascending
        /// </summary>
        public List<GanttStageItemDto> Stages { get; set; }
    }
}
