using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SerenAuth.Application.Behaviors;

namespace SerenAuth.Application;

/// <summary>
/// Hooks MediatR + FluentValidation into the host's DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddSerenAuthApplication(this IServiceCollection services)
    {
        var asm = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(asm));
        services.AddValidatorsFromAssembly(asm, includeInternalTypes: true);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
