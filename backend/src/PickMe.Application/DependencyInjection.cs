using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PickMe.Application.Auth;

namespace PickMe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterCustomerValidator>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
