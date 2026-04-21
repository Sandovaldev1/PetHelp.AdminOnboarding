using System.Security.Claims;

namespace PetHelp.AdminOnboarding.Security;

public static class UserEmailResolver
{
    public static string? GetEmail(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("email")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("upn")?.Value;

        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }

    public static string? GetNormalizedEmail(ClaimsPrincipal? user)
    {
        var email = GetEmail(user);
        return email is null ? null : email.ToUpperInvariant();
    }
}
