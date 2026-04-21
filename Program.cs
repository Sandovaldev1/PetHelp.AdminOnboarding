using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using PetHelp.AdminOnboarding.Components;
using PetHelp.AdminOnboarding.Security;
using PetHelp.AdminOnboarding.Services;
using PetHelp.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
var appAuthentication = builder.Configuration.GetSection(AppAuthenticationOptions.SectionName).Get<AppAuthenticationOptions>() ?? new();
var useDevelopmentBypass = builder.Environment.IsDevelopment()
    && appAuthentication.Mode.Equals("DevelopmentBypass", StringComparison.OrdinalIgnoreCase);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddPetHelpInfrastructure(builder.Configuration);
builder.Services.AddScoped<IOnboardingAdminService, OnboardingAdminService>();
builder.Services.Configure<AdminAccessOptions>(builder.Configuration.GetSection(AdminAccessOptions.SectionName));
builder.Services.Configure<AppAuthenticationOptions>(builder.Configuration.GetSection(AppAuthenticationOptions.SectionName));

if (useDevelopmentBypass)
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
            options.DefaultAuthenticateScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = DevelopmentBypassAuthenticationHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, DevelopmentBypassAuthenticationHandler>(
            DevelopmentBypassAuthenticationHandler.SchemeName,
            _ => { });
}
else
{
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            var auth0 = builder.Configuration.GetSection("Auth0");
            options.Authority = NormalizeAuth0Authority(auth0["Domain"]);
            options.ClientId = auth0["ClientId"] ?? string.Empty;
            options.ClientSecret = auth0["ClientSecret"] ?? string.Empty;
            options.CallbackPath = auth0["CallbackPath"] ?? "/signin-oidc";
            options.SignedOutCallbackPath = auth0["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            var configuredScopes = auth0.GetSection("Scopes").Get<string[]>() ?? [];
            foreach (var scope in configuredScopes.Where(scope => !string.IsNullOrWhiteSpace(scope)))
            {
                if (!options.Scope.Contains(scope, StringComparer.Ordinal))
                {
                    options.Scope.Add(scope);
                }
            }

            var audience = auth0["Audience"];
            if (!string.IsNullOrWhiteSpace(audience))
            {
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("audience", audience);
                        return Task.CompletedTask;
                    }
                };
            }
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminAccessPolicies.Admin, policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new AdminAccessRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, AdminAccessRequirementHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (useDevelopmentBypass)
{
    app.MapGet("/auth/login", (string? returnUrl) =>
    {
        return Results.LocalRedirect(NormalizeLocalReturnUrl(returnUrl));
    }).AllowAnonymous();

    app.MapGet("/auth/logout", (string? returnUrl) =>
    {
        return Results.LocalRedirect(NormalizeLocalReturnUrl(returnUrl));
    }).AllowAnonymous();
}
else
{
    app.MapGet("/auth/login", (HttpContext httpContext, string? returnUrl) =>
    {
        var redirectUri = NormalizeLocalReturnUrl(returnUrl);
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return Results.LocalRedirect(redirectUri);
        }

        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [OpenIdConnectDefaults.AuthenticationScheme]);
    }).AllowAnonymous();

    app.MapGet("/auth/logout", (HttpContext httpContext, string? returnUrl) =>
    {
        var redirectUri = NormalizeLocalReturnUrl(returnUrl);
        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = redirectUri },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }).AllowAnonymous();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string NormalizeAuth0Authority(string? domainOrAuthority)
{
    if (string.IsNullOrWhiteSpace(domainOrAuthority))
    {
        throw new InvalidOperationException("Auth0:Domain configuration is required.");
    }

    var authority = domainOrAuthority.Trim();
    if (!authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        authority = authority.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? authority.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase)
            : $"https://{authority}";
    }

    return authority.TrimEnd('/');
}

static string NormalizeLocalReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/";
    }

    var trimmed = returnUrl.Trim();
    if (!trimmed.StartsWith("/", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
    {
        return "/";
    }

    return trimmed;
}
