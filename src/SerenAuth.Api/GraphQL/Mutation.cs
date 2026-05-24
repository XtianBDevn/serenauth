using HotChocolate;
using HotChocolate.Authorization;
using MediatR;
using SerenAuth.Api.Authorization;
using SerenAuth.Application.Auth;
using SerenAuth.Application.Dtos;
using SerenAuth.Application.PriorAuthorizations;

namespace SerenAuth.Api.GraphQL;

public sealed record CreatePriorAuthorizationInput(
    string PatientId,
    string ProviderId,
    string ProcedureCpt,
    string DiagnosisIcd10,
    string Payer,
    double AiConfidence);

public sealed record UpdatePriorAuthorizationInput(
    string Id,
    string ProcedureCpt,
    string DiagnosisIcd10,
    string Payer,
    double AiConfidence);

public sealed record SubmitPriorAuthorizationInput(string Id);

public sealed record WithdrawPriorAuthorizationInput(string Id);

public sealed record DecidePriorAuthorizationInput(string Id, PaDecision Decision);

public sealed record LoginInput(string Email, string Password);

public sealed class Mutation
{
    /// <summary>
    /// Public mutation — exchanges credentials for a JWT. Deliberately
    /// not behind any policy; rate limiting still applies per-IP.
    /// </summary>
    public Task<LoginResultDto> Login(
        [Service] IMediator mediator,
        LoginInput input,
        CancellationToken ct = default)
        => mediator.Send(new LoginCommand(input.Email, input.Password), ct);

    [Authorize(Policy = Policies.RequirePaWrite)]
    public Task<PriorAuthorizationDto> CreatePriorAuthorization(
        [Service] IMediator mediator,
        CreatePriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new CreatePriorAuthorizationCommand(
            input.PatientId,
            input.ProviderId,
            input.ProcedureCpt,
            input.DiagnosisIcd10,
            input.Payer,
            input.AiConfidence), ct);

    /// <summary>
    /// Edits the clinical fields of a draft PA. Same policy as Create
    /// (RequirePaWrite) — intake/clinician/admin — because editing a
    /// draft is the natural sibling of creating one. The Draft-only
    /// invariant is enforced by the domain, not by the policy.
    /// </summary>
    [Authorize(Policy = Policies.RequirePaWrite)]
    public Task<PriorAuthorizationDto> UpdatePriorAuthorization(
        [Service] IMediator mediator,
        UpdatePriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new UpdatePriorAuthorizationCommand(
            input.Id,
            input.ProcedureCpt,
            input.DiagnosisIcd10,
            input.Payer,
            input.AiConfidence), ct);

    [Authorize(Policy = Policies.RequirePaSubmit)]
    public Task<PriorAuthorizationDto> SubmitPriorAuthorization(
        [Service] IMediator mediator,
        SubmitPriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new SubmitPriorAuthorizationCommand(input.Id), ct);

    /// <summary>
    /// Withdraws a Pending PA — clinic-side "take it back" before the
    /// payer responds. Same policy as submit: the roles that can push
    /// a PA out are the ones who can pull it back.
    /// </summary>
    [Authorize(Policy = Policies.RequirePaSubmit)]
    public Task<PriorAuthorizationDto> WithdrawPriorAuthorization(
        [Service] IMediator mediator,
        WithdrawPriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new WithdrawPriorAuthorizationCommand(input.Id), ct);

    /// <summary>
    /// Records the payer's decision on a Pending PA. Admin-only — this
    /// closes the PA lifecycle, so the policy is intentionally tighter
    /// than write/submit.
    /// </summary>
    [Authorize(Policy = Policies.RequireAdmin)]
    public Task<PriorAuthorizationDto> DecidePriorAuthorization(
        [Service] IMediator mediator,
        DecidePriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new DecidePriorAuthorizationCommand(input.Id, input.Decision), ct);
}
