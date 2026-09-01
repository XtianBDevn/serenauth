namespace SerenAuth.Domain.Enums;

/// <summary>
/// Roles assigned to users within an organization. Granular permissions
/// are enforced via policies in the API layer; roles are deliberately
/// coarse to keep authorization decisions auditable.
/// </summary>
public enum Role
{
    /// <summary>Read-only across the organization. Cannot create or submit PAs.</summary>
    Viewer = 0,

    /// <summary>Can create and edit prior authorizations but not submit them.</summary>
    Intake = 1,

    /// <summary>Can create, edit, and submit prior authorizations.</summary>
    Clinician = 2,

    /// <summary>Full administrative access within their organization.</summary>
    Admin = 3
}
