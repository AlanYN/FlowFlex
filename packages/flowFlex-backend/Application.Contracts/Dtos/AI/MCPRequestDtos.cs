using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.AI;

public class StoreContextRequest
{
    [Required]
    public string ContextId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GraphQueryRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;
}

public class MCPWorkflowGenerationRequest
{
    [Required]
    public string Description { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public List<string> Requirements { get; set; } = new();
    public string Industry { get; set; } = string.Empty;
    public string ProcessType { get; set; } = string.Empty;
    public bool IncludeApprovals { get; set; } = true;
    public bool IncludeNotifications { get; set; } = true;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class MCPQuestionnaireGenerationRequest
{
    [Required]
    public string Purpose { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public string Context { get; set; } = string.Empty;
    public int EstimatedQuestions { get; set; } = 10;
    public bool IncludeValidation { get; set; } = true;
    public string Complexity { get; set; } = "Medium";
}

public class MCPChecklistGenerationRequest
{
    [Required]
    public string ProcessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public List<string> RequiredSteps { get; set; } = new();
    public string Context { get; set; } = string.Empty;
    public bool IncludeDependencies { get; set; } = true;
    public bool IncludeEstimates { get; set; } = true;
}

public class MCPServiceStatus
{
    public bool IsAvailable { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public string MemoryProvider { get; set; } = string.Empty;
    public bool EnablePersistence { get; set; }
    public DateTime LastHealthCheck { get; set; }
}
