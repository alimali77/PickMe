using System.Security.Claims;
using PickMe.Application.Abstractions;

namespace PickMe.Api.Common;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor = accessor;

    private ClaimsPrincipal? P => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = P?.FindFirstValue(ClaimTypes.NameIdentifier) ?? P?.FindFirstValue("sub");
            return Guid.TryParse(id, out var g) ? g : null;
        }
    }

    public string? Email => P?.FindFirstValue(ClaimTypes.Email) ?? P?.FindFirstValue("email");
    public string? Role => P?.FindFirstValue(ClaimTypes.Role) ?? P?.FindFirstValue("role");
    public bool IsAuthenticated => P?.Identity?.IsAuthenticated ?? false;
}
