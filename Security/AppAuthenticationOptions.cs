namespace PetHelp.AdminOnboarding.Security;

public sealed class AppAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Mode { get; set; } = "Auth0";
    public string DevBypassEmail { get; set; } = "dev-admin@local.pethelp";
    public string DevBypassName { get; set; } = "Administrador local";
}
