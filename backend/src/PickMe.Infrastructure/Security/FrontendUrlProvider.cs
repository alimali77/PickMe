using Microsoft.Extensions.Configuration;
using PickMe.Application.Auth;

namespace PickMe.Infrastructure.Security;

public sealed class FrontendUrlProvider(IConfiguration config) : IFrontendUrlProvider
{
    private readonly string _baseUrl = (config["FRONTEND_BASE_URL"]
        ?? Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
        ?? "https://localhost:5173").TrimEnd('/');

    public string VerifyEmailUrl(string token) => $"{_baseUrl}/eposta-dogrula?token={Uri.EscapeDataString(token)}";

    public string ResetPasswordUrl(string token) => $"{_baseUrl}/sifre-sifirla?token={Uri.EscapeDataString(token)}";
}
