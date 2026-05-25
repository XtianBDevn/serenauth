namespace SerenAuth.Domain.Enums;

/// <summary>
/// Every sensitive operation emits an <c>AuditEvent</c> with one of these
/// actions. Names are stable strings — never reuse a value for a different
/// meaning; add a new entry instead. The underscored UPPER_CASE names are
/// the persisted on-disk identifiers, so CA1707 is intentionally suppressed.
/// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
public enum AuditAction
{
    LOGIN = 0,
    CREATE_PA = 1,
    UPDATE_PA = 2,
    SUBMIT_PA = 3,
    VIEW_PA = 4,
    DECIDE_PA = 5,
    WITHDRAW_PA = 6,
    CHANGE_PASSWORD = 7
}
#pragma warning restore CA1707
