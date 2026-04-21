using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PetHelp.AdminOnboarding.Security;

public sealed class DevelopmentBypassAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevelopmentBypass";

    private readonly IOptions<AppAuthenticationOptions> _appAuthentication;

    public DevelopmentBypassAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AppAuthenticationOptions> appAuthentication)
        : base(options, logger, encoder)
    {
        _appAuthentication = appAuthentication;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var email = string.IsNullOrWhiteSpace(_appAuthentication.Value.DevBypassEmail)
            ? "dev-admin@local.pethelp"
            : _appAuthentication.Value.DevBypassEmail.Trim();
        var name = string.IsNullOrWhiteSpace(_appAuthentication.Value.DevBypassName)
            ? email
            : _appAuthentication.Value.DevBypassName.Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, $"dev-bypass|{email}"),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new("email", email),
            new("sub", $"dev-bypass|{email}")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
