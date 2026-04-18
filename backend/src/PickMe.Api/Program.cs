using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PickMe.Api.Common;
using PickMe.Application;
using PickMe.Application.Abstractions;
using PickMe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
builder.Configuration.AddEnvironmentVariables();

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET ayarlanmamış.");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "pickme-api";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "pickme-web";

// ---------- Services ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Swagger UI + OpenAPI JSON (NSwag CLI bu şemayı okuyup shared/api-types.ts üretir)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Pick Me API",
        Version = "v1",
        Description = "Pick Me — Şoför & Vale rezervasyon platformu REST API. Auth için Authorize butonuna `Bearer <token>` giriniz.",
    });

    // Nested tip adı çakışmalarını engelle (birden fazla controller'da SetActiveRequest
    // nested record'u var — tam tip adıyla ayırt et).
    c.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);

    // JWT Bearer scheme tanımı — Swagger UI'da "Authorize" butonu gelir.
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "JWT Bearer token — `Bearer <token>` formatında giriniz.",
    });
    // AddSecurityRequirement Microsoft.OpenApi 2.x'te sorunlu (Swashbuckle 10 ile uyumsuz
    // çağrı imzası). Kullanıcı "Authorize" butonu ile tokenı tanımlar, UI auto-apply yapar.
});

var corsOrigins = (builder.Configuration["CORS_ORIGINS"]
    ?? Environment.GetEnvironmentVariable("CORS_ORIGINS")
    ?? "https://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
        };
    });

builder.Services.AddAuthorization();

// ---------- Build ----------
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});

// Swagger her ortamda açık — anon olduğu için sadece şema bilgisi verir,
// prod'da endpoint'leri reverse-proxy ile IP kısıtlayıp kapatabilirsiniz.
app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pick Me API v1");
        c.DocumentTitle = "Pick Me API";
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

// Hangfire dashboard — admin-only (JWT rol kontrolü)
var hangfireEnabled = bool.Parse(builder.Configuration["HANGFIRE_ENABLED"] ?? "true");
if (hangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AdminOnlyHangfireFilter()],
        AppPath = "/admin",
        DashboardTitle = "Pick Me — Job Queue",
        StatsPollingInterval = 5_000,
    });
}

// Startup: migration + initial admin seed
await PickMe.Infrastructure.Persistence.DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
