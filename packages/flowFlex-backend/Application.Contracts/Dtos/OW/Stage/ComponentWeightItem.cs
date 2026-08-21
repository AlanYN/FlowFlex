using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Represents the weight configuration for a single Component instance within a Stage.
    /// Serialized to/from ff_stage.component_weights (JSONB).
    /// Sum of all weights in a Stage must equal 100 when weights are configured.
    /// </summary>
    public class ComponentWeightItem
    {
        /// <summary>
        /// Component type.
        /// Valid values: "fields" | "checklist" | "questionnaire" | "files" | "quickLink"
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Instance identifier.
        /// - fields: fixed value "fields"
        /// - checklist / questionnaire / files / quickLink: string-ified snowflake long ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the component instance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Weight value 0–100 (integer).
        /// Sum of all weights in a Stage must equal 100 when the list is non-empty.
        /// </summary>
        [Range(0, 100)]
        public int Weight { get; set; }
    }
}
