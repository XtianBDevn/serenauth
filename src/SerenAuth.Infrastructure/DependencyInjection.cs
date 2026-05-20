using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SerenAuth.Application.Abstractions;
using SerenAuth.Domain.Abstractions;
using SerenAuth.Infrastructure.Mongo;
using SerenAuth.Infrastructure.Options;
using SerenAuth.Infrastructure.Security;

namespace SerenAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSerenAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SeedingOptions>(configuration.GetSection(SeedingOptions.SectionName));

        services.AddSingleton<MongoContext>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPriorAuthorizationRepository, PriorAuthorizationRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditPublisher, AuditPublisher>();

        services.AddHostedService<MongoIndexInitializer>();
        services.AddHostedService<MongoSeeder>();

        return services;
    }
}
