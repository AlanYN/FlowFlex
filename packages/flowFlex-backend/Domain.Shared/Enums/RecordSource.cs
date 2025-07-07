namespace FlowFlex.Domain.Shared.Enums;

public enum RecordSource
{
    None = 0,
    OnBorading = 1,
    RateEngine = 2,
    Bnp = 3,
    CreditSafe = 4,
    Experian = 5,
    Wise = 6,
    WorkFlow = 7,
    Pass = 8,
    Unis = 9,
    TMS = 10,
    IdentityHub = 11,
    Location = 12,
    DI = 13,
    ThirdParty = 99 // Generic third-party interface
}
