using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.Item
{
    /// <summary>
    /// Preferences Currency options for transaction details
    /// </summary>
    public enum PreferencesCurrencyEnum
    {
        [Description("US Dollar (USD) $")]
        USD = 1, // US Dollar ($)
        [Description("Euro (EUR) А")]
        EUR = 2, // Euro (А)
        [Description("Japanese Yen (JPY) гд")]
        JPY = 3, // Japanese Yen (гд)
        [Description("British Pound (GBP) бъ")]
        GBP = 4, // British Pound (бъ)
        [Description("Australian Dollar (AUD) A$")]
        AUD = 5, // Australian Dollar (A$)
        [Description("Canadian Dollar (CAD) C$")]
        CAD = 6, // Canadian Dollar (C$)
        [Description("Swiss Franc (CHF) CHF")]
        CHF = 7, // Swiss Franc (CHF)
        [Description("Chinese Yuan (CNY) гд")]
        CNY = 8, // Chinese Yuan (гд)
        [Description("Hong Kong Dollar (HKD) HK$")]
        HKD = 9, // Hong Kong Dollar (HK$)
        [Description("Indian Rupee (INR) ?")]
        INR = 10, // Indian Rupee (?)
        [Description("Russian Ruble (RUB) ?")]
        RUB = 11, // Russian Ruble (?)
        [Description("Brazilian Real (BRL) R$")]
        BRL = 12, // Brazilian Real (R$)
        [Description("South African Rand (ZAR) R")]
        ZAR = 13, // South African Rand (R)
        [Description("Singapore Dollar (SGD) S$")]
        SGD = 14, // Singapore Dollar (S$)
        [Description("South Korean Won (KRW) ?")]
        KRW = 15, // South Korean Won (?)
        [Description("Mexican Peso (MXN) Mex$")]
        MXN = 16, // Mexican Peso (Mex$)
        [Description("New Zealand Dollar (NZD) NZ$")]
        NZD = 17, // New Zealand Dollar (NZ$)
        [Description("Philippine Peso (PHP) ?")]
        PHP = 18, // Philippine Peso (?)
        [Description("United Arab Emirates Dirham (AED) ?.?")]
        AED = 19
    }
}
