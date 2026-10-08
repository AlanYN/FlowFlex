using System.Text.Json;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;

namespace FlowFlex.Application.Services.Shared
{
    /// <summary>
    /// Effective runtime permission resolved from a Workflow's template + runtime fields.
    /// </summary>
    public record WorkflowEffectiveRuntime(
        ViewPermissionModeEnum ViewMode,
        List<string> ViewTeams,
        List<string> OperateTeams
    );

    /// <summary>
    /// Effective runtime permission resolved from a Stage's fields relative to its Workflow.
    /// </summary>
    public record StageEffectiveRuntime(
        ViewPermissionModeEnum ViewMode,
        List<string> ViewTeams,
        ViewPermissionModeEnum OperateMode,
        List<string> OperateTeams,
        List<string> RollBackTeams
    );

    /// <summary>
    /// Pure static helper that computes effective runtime permission objects.
    /// No DI, no DB access — accepts domain entities and returns value objects.
    /// </summary>
    public static class PermissionCalculator
    {
        /// <summary>
        /// Task 3.1 — Compute the effective runtime permission for a Workflow.
        /// When <see cref="Workflow.RuntimeUseSameAsTemplate"/> is <c>true</c>, the template
        /// fields (<see cref="Workflow.ViewPermissionMode"/>, <see cref="Workflow.ViewTeams"/>,
        /// <see cref="Workflow.OperateTeams"/>) are used.
        /// When <c>false</c>, the dedicated runtime fields are used instead.
        /// </summary>
        public static WorkflowEffectiveRuntime ComputeWorkflowEffectiveRuntime(Workflow workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            if (workflow.RuntimeUseSameAsTemplate)
            {
                return new WorkflowEffectiveRuntime(
                    ViewMode: workflow.ViewPermissionMode,
                    ViewTeams: DeserializeTeams(workflow.ViewTeams),
                    OperateTeams: DeserializeTeams(workflow.OperateTeams)
                );
            }
            else
            {
                return new WorkflowEffectiveRuntime(
                    ViewMode: workflow.RuntimeViewPermissionMode,
                    ViewTeams: DeserializeTeams(workflow.RuntimeViewTeams),
                    OperateTeams: DeserializeTeams(workflow.RuntimeOperateTeams)
                );
            }
        }

        /// <summary>
        /// Task 3.2 — Compute the effective runtime permission for a Stage.
        /// When <see cref="Stage.RuntimeUseSameAsWorkflow"/> is <c>true</c>, the stage inherits
        /// view mode and teams from <paramref name="workflowRuntime"/>.
        /// When <c>false</c>, stage-level fields are used and validated as subsets of the workflow runtime.
        /// </summary>
        /// <exception cref="CRMException">
        /// Thrown when stage teams are not a subset of the workflow runtime teams.
        /// </exception>
        public static StageEffectiveRuntime ComputeStageEffectiveRuntime(
            Stage stage,
            WorkflowEffectiveRuntime workflowRuntime)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            if (workflowRuntime == null)
                throw new ArgumentNullException(nameof(workflowRuntime));

            var rollBackTeams = DeserializeTeams(stage.RollBackTeams);

            if (stage.RuntimeUseSameAsWorkflow)
            {
                return new StageEffectiveRuntime(
                    ViewMode: workflowRuntime.ViewMode,
                    ViewTeams: workflowRuntime.ViewTeams,
                    OperateMode: workflowRuntime.ViewMode,
                    OperateTeams: workflowRuntime.OperateTeams,
                    RollBackTeams: rollBackTeams
                );
            }
            else
            {
                // View teams — must be ⊆ workflow runtime view teams
                var viewTeams = DeserializeTeams(stage.RuntimeViewTeams);
                if (!IsSubsetOf(viewTeams, workflowRuntime.ViewTeams))
                {
                    throw new CRMException(
                        ErrorCodeEnum.BusinessError,
                        "Stage Runtime view teams must be a subset of Workflow Runtime view teams.");
                }

                // Operate teams — resolve from flag then validate ⊆ workflow runtime operate teams
                List<string> operateTeams;
                if (stage.RuntimeUseSameTeamForOperate)
                {
                    operateTeams = viewTeams;
                }
                else
                {
                    operateTeams = DeserializeTeams(stage.RuntimeOperateTeams);
                }

                if (!IsSubsetOf(operateTeams, workflowRuntime.OperateTeams))
                {
                    throw new CRMException(
                        ErrorCodeEnum.BusinessError,
                        "Stage Runtime operate teams must be a subset of Workflow Runtime operate teams.");
                }

                return new StageEffectiveRuntime(
                    ViewMode: stage.RuntimeViewPermissionMode,
                    ViewTeams: viewTeams,
                    OperateMode: stage.RuntimeViewPermissionMode,
                    OperateTeams: operateTeams,
                    RollBackTeams: rollBackTeams
                );
            }
        }

        /// <summary>
        /// Task 3.3 — Determines whether <paramref name="teams"/> is a subset of <paramref name="maxTeams"/>.
        /// <list type="bullet">
        ///   <item><description><paramref name="teams"/> null or empty → always <c>true</c></description></item>
        ///   <item><description><paramref name="maxTeams"/> null or empty (and teams is non-empty) → <c>false</c></description></item>
        ///   <item><description>Otherwise: every element of <paramref name="teams"/> must exist in <paramref name="maxTeams"/> (case-sensitive)</description></item>
        /// </list>
        /// </summary>
        public static bool IsSubsetOf(List<string> teams, List<string> maxTeams)
        {
            if (teams == null || teams.Count == 0)
                return true;

            if (maxTeams == null || maxTeams.Count == 0)
                return false;

            var maxSet = new HashSet<string>(maxTeams, StringComparer.Ordinal);
            foreach (var team in teams)
            {
                if (!maxSet.Contains(team))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes a JSONB string column into a <see cref="List{string}"/>.
        /// Returns an empty list for null, whitespace, or unparseable input.
        /// </summary>
        /// <summary>
        /// Public accessor for DeserializeTeams, used by callers that need to clamp boundary violations.
        /// </summary>
        public static List<string> DeserializeTeamsPublic(string json) => DeserializeTeams(json);

        private static List<string> DeserializeTeams(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
