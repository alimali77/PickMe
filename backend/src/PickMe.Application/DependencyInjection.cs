using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PickMe.Application.Auth;
using PickMe.Application.Public;
using PickMe.Application.Reservations;

namespace PickMe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterCustomerValidator>();

        // Admin cancel validator ayrı bir concrete sınıf olduğu için doğrudan register ediyoruz
        // (aynı DTO için 2 IValidator<T> olamaz — DI'da tekil AdminCancelReservationValidator olarak kaydolur).
        services.AddScoped<AdminCancelReservationValidator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPublicService, PublicService>();
        return services;
    }
}
