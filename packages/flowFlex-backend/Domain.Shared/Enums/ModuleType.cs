namespace FlowFlex.Domain.Shared.Enums;

public static class ModuleType
{
    public const int None = 0;

    public const int Activity = 1;

    public const int Contact = 2;

    public const int Company = 3;

    public const int Deal = 4;

    public const int Product = 5;

    public const int LineItem = 6;

    public const int Contract = 7;

    public const int Task = 8;

    public const int Note = 9;

    public const int OperationLog = 10;

    public static string GetName(int moduleType)
    {
        return moduleType switch
        {
            None => "None",
            Contact => "Contact",
            Company => "Company",
            Deal => "Deal",
            Product => "Product",
            LineItem => "Line Item",
            Contract => "Contract",
            _ => "Unknown"
        };
    }
}
