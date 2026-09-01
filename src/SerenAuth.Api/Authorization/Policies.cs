namespace SerenAuth.Api.Authorization;

/// <summary>
/// Named authorization policies. Centralized so resolvers don't pass
/// raw role strings around.
/// </summary>
public static class Policies
{
    public const string RequireOrgScope = "RequireOrgScope";
    public const string RequirePaRead = "RequirePaRead";
    public const string RequirePaWrite = "RequirePaWrite";
    public const string RequirePaSubmit = "RequirePaSubmit";
    public const string RequireAdmin = "RequireAdmin";
}
