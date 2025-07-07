namespace FlowFlex.Domain.Shared.Const;

public class ErrorMessages
{
    public const string ValidFieldName =
        "internal_name Must start with a digit or lowercase letter\r\n" +
        "Can contain a combination of digits, lowercase letters, and underscores\r\n" +
        "Can consist of only digits or lowercase letters\r\n" +
        "Cannot consist of only underscores\r\n" +
        "All letters must be lowercase";
}
