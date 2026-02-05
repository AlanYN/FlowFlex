using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Const;

public static class AuthSchemes
{
    public const string ApiKey = "wfe.apikey";

    public const string Identification = "wfe.identify";

    public const string PassIdentification = "pass.identify";

    public const string IdentityClient = "identity.client";

    public const string ItemIamIdentification = "item.iam.identify";
    public const string ItemIamClientIdentification = "item.iam.identify.client";
}

public static class AuthPolicies
{
    public const string ItemExternalPolicy = "crm.external.item.pub";
    public const string ApsExternalPolicy = "crm.external.aps.pub";
    public const string WiseExternalPolicy = "crm.external.wise.pub";
    public const string OnBoardingExternalPolicy = "crm.external.onboarding.pub";
    public const string RateEnginePolicy = "crm.external.rateengine.pub";
}

public static class ScopeNames
{
    public const string DiPublic = "crm_di_public";
    public const string WmsPublic = "crm_wms_public";
    public const string PassPublic = "crm_pass_public";
    public const string ApsPublic = "crm_aps_public";
    public const string WisePublic = "crm_wise_public";
    public const string OnBoardingPublic = "crm_onboarding_public";
    public const string RateEnginePublic = "crm_rate_public";
    public const string TmsPublic = "crm_tms_public";
    public const string Public = "crm_vrm_public";
    // For testing and integration use, long-term non-expiring
    public const string TestPublic = "crm_test_public";

    public readonly static List<string> ItemExternalScopes = [TmsPublic, DiPublic, WmsPublic, PassPublic, Public, TestPublic];
}
