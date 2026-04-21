using System.Security.Claims;

namespace PetHelp.AdminOnboarding.Security;

public static class AdminIdentityResolver
{
    public static string GetDecisionActorId(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("Debes iniciar sesion para aprobar o rechazar solicitudes.");
        }

        var candidates = new[]
        {
            UserEmailResolver.GetEmail(user),
            user.FindFirst("sub")?.Value,
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            user.Identity?.Name
        };

        foreach (var candidate in candidates
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(value => value!.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (candidate.Length <= 128)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo resolver el identificador del administrador autenticado.");
    }
}
