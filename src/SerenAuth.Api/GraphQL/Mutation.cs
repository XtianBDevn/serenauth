using HotChocolate;
using HotChocolate.Authorization;
using MediatR;
using SerenAuth.Api.Authorization;
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

public sealed record SubmitPriorAuthorizationInput(string Id);

public sealed class Mutation
{
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

    [Authorize(Policy = Policies.RequirePaSubmit)]
    public Task<PriorAuthorizationDto> SubmitPriorAuthorization(
        [Service] IMediator mediator,
        SubmitPriorAuthorizationInput input,
        CancellationToken ct = default)
        => mediator.Send(new SubmitPriorAuthorizationCommand(input.Id), ct);
}
