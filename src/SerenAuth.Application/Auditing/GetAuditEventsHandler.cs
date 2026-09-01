using MediatR;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Abstractions;

namespace SerenAuth.Application.Auditing;

/// <summary>
/// Reads audit events for the caller's organization. Deliberately does
/// NOT emit an audit event for the read itself — that would inflate the
/// log and (when combined with any read-while-listing pattern) risk a
/// feedback loop. If a "who read the audit log" trail is needed later,
/// add it via middleware, not via this handler.
/// </summary>
public sealed class GetAuditEventsHandler(
    IAuditEventRepository repo,
    ICurrentUser currentUser)
    : IRequestHandler<GetAuditEventsQuery, IReadOnlyList<AuditEventDto>>
{
    public async Task<IReadOnlyList<AuditEventDto>> Handle(
        GetAuditEventsQuery request,
        CancellationToken cancellationToken)
    {
        var events = await repo.ListByOrganizationAsync(
            currentUser.OrganizationId,
            request.Action,
            request.Since,
            request.Limit,
            cancellationToken);

        return events.Select(AuditEventDto.FromEntity).ToList();
    }
}
