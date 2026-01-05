using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Stage component model
    /// </summary>
    public class StageComponent
    {
        /// <summary>
        /// Component key (fields, checklist, questionnaires, files, quickLinks)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Key { get; set; }

        /// <summary>
        /// Component display order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the component is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Component configuration (JSON)
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Static fields configuration list (for fields component)
        /// Contains field ID, isRequired, and order
        /// </summary>
        public List<StaticFieldConfig> StaticFields { get; set; } = new List<StaticFieldConfig>();

        /// <summary>
        /// Selected checklist IDs (for checklist component)
        /// </summary>
        public List<long> ChecklistIds { get; set; } = new List<long>();

        /// <summary>
        /// Selected questionnaire IDs (for questionnaire component)
        /// </summary>
        public List<long> QuestionnaireIds { get; set; } = new List<long>();

        /// <summary>
        /// Names of selected checklists (for checklist component)
        /// </summary>
        public List<string> ChecklistNames { get; set; } = new List<string>();

        /// <summary>
        /// Names of selected questionnaires (for questionnaire component)
        /// </summary>
        public List<string> QuestionnaireNames { get; set; } = new List<string>();

        /// <summary>
        /// Selected quick link IDs (for quickLinks component)
        /// </summary>
        public List<long> QuickLinkIds { get; set; } = new List<long>();

        /// <summary>
        /// Names of selected quick links (for quickLinks component)
        /// </summary>
        public List<string> QuickLinkNames { get; set; } = new List<string>();

        /// <summary>
        /// Customer Portal Access level for this component
        /// Defines how this component is visible and accessible in the customer portal
        /// </summary>
        public PortalPermissionEnum CustomerPortalAccess { get; set; } = PortalPermissionEnum.Viewable;
    }
}