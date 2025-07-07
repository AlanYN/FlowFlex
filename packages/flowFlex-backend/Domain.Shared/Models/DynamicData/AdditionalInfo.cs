namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class AdditionalInfo
{
    public ComputedModel Computed { get; set; }
}

public class ComputedModel
{
    public ComputedType ComputedType { get; set; }

    /// <summary>
    /// Parameters for computed properties
    /// </summary>
    public ArgumentModel[] ComputedArguments { get; set; }
}

public class ArgumentModel
{
    public ArgumentType Type { get; set; }

    public string Value { get; set; }
}

public enum ComputedType
{
    /// <summary>
    /// Addition
    /// </summary>
    Addition = 1,

    /// <summary>
    /// Subtraction
    /// </summary>
    Subtraction = 2,

    /// <summary>
    /// Multiplication
    /// </summary>
    Multiplication = 3,

    /// <summary>
    /// Division
    /// </summary>
    Division = 4
}

public enum ArgumentType
{
    /// <summary>
    /// Property
    /// </summary>
    Property,

    /// <summary>
    /// Constant
    /// </summary>
    Const
}
