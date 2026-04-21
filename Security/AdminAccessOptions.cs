namespace PetHelp.AdminOnboarding.Security;

public sealed class AdminAccessOptions
{
    public const string SectionName = "AdminAccess";

    public string[] AllowedEmails { get; set; } = Array.Empty<string>();
}
