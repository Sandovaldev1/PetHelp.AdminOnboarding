using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PetHelp.AdminOnboarding.Security;

public static class AdminAccessPolicies
{
    public const string Admin = "AdminOnly";
}

public sealed class AdminAccessRequirement : IAuthorizationRequirement;

public sealed class AdminAccessRequirementHandler : AuthorizationHandler<AdminAccessRequirement>
{
    private readonly IOptions<AdminAccessOptions> _options;

    public AdminAccessRequirementHandler(IOptions<AdminAccessOptions> options)
    {
        _options = options;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var allowedEmails = _options.Value.AllowedEmails
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);

        if (allowedEmails.Count == 0)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var normalizedEmail = UserEmailResolver.GetNormalizedEmail(context.User);
        if (!string.IsNullOrWhiteSpace(normalizedEmail) && allowedEmails.Contains(normalizedEmail))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
