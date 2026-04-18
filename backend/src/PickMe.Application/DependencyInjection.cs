using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PickMe.Application.AdminManagement;
using PickMe.Application.Auth;
using PickMe.Application.Public;
using PickMe.Application.Reservations;

namespace PickMe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterCustomerValidator>();

        // Admin cancel validator aynı DTO için ikinci validator olduğu için
        // manuel scoped olarak kaydediyoruz (IValidator<CancelReservationDto>'da çakışmaması için).
        services.AddScoped<AdminCancelReservationValidator>();

        // Auth + public + rezervasyon
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<IPublicService, PublicService>();

        // Admin yönetim modülleri (Faz 5)
        services.AddScoped<IDriverManagementService, DriverManagementService>();
        services.AddScoped<IRecipientsService, RecipientsService>();
        services.AddScoped<IFaqManagementService, FaqManagementService>();
        services.AddScoped<IContactMessagesService, ContactMessagesService>();
        services.AddScoped<ICustomerAdminService, CustomerAdminService>();
        services.AddScoped<IRatingAdminService, RatingAdminService>();
        services.AddScoped<IAdminUsersService, AdminUsersService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();

        return services;
    }
}
