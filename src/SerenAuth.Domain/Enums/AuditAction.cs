namespace SerenAuth.Domain.Enums;

/// <summary>
/// Every sensitive operation emits an <c>AuditEvent</c> with one of these
/// actions. Names are stable strings — never reuse a value for a different
/// meaning; add a new entry instead.
/// </summary>
public enum AuditAction
{
    LOGIN = 0,
    CREATE_PA = 1,
    UPDATE_PA = 2,
    SUBMIT_PA = 3,
    VIEW_PA = 4
}
