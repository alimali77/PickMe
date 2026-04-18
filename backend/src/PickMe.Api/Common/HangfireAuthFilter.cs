using Hangfire.Dashboard;
using PickMe.Domain;

namespace PickMe.Api.Common;

/// <summary>
/// Hangfire dashboard'a sadece rolü Admin olan JWT sahipleri erişebilir.
/// Prod'da ayrıca IIS/Nginx seviyesinde IP kısıtlaması önerilir.
/// </summary>
public sealed class AdminOnlyHangfireFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User?.Identity?.IsAuthenticated != true) return false;
        return httpContext.User.IsInRole(nameof(UserRole.Admin));
    }
}
