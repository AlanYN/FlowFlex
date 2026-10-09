using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Document Operation Log Entity - Records every Univer MUTATION event for audit purposes
    /// </summary>
    [SugarTable("ff_document_operation_log")]
    public class DocumentOperationLog : EntityBaseCreateInfo
    {
        /// <summary>
        /// Associated document unit_id
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "unit_id")]
        public string UnitId { get; set; }

        /// <summary>
        /// Operator user ID (compatible with IDM user system)
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "operator_user_id")]
        public string OperatorUserId { get; set; }

        /// <summary>
        /// Operator user display name
        /// </summary>
        [StringLength(100)]
        [SugarColumn(ColumnName = "operator_user_name")]
        public string OperatorUserName { get; set; }

        /// <summary>
        /// Operation timestamp in milliseconds (from frontend Date.now())
        /// </summary>
        [SugarColumn(ColumnName = "op_timestamp")]
        public long OpTimestamp { get; set; }

        /// <summary>
        /// Univer MutationId (e.g. "sheet.mutation.set-range-values")
        /// </summary>
        [StringLength(200)]
        [SugarColumn(ColumnName = "mutation_id")]
        public string MutationId { get; set; }

        /// <summary>
        /// Mutation parameters JSON (JSONB).
        /// IsJson = true is required so SqlSugar sends the value as JSONB, not text.
        /// </summary>
        [SugarColumn(ColumnName = "params_json", ColumnDataType = "jsonb", IsJson = true)]
        public string ParamsJson { get; set; }

        /// <summary>
        /// Collaborative revision number at the time of this operation
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// Snapshot of cell values captured by the backend BEFORE this mutation was applied.
        /// Only populated for set-range-values mutations. Structure mirrors cellValue in
        /// ParamsJson: { "rowIndex": { "colIndex": "oldValue" } }, where null means the cell
        /// was empty. Populated by the service layer by reading the previous log entry.
        /// </summary>
        [SugarColumn(ColumnName = "old_values_json", ColumnDataType = "jsonb", IsJson = true, IsNullable = true)]
        public string OldValuesJson { get; set; }
    }
}
