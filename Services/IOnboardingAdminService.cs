using System.Security.Claims;
using PetHelp.AdminOnboarding.Models;
using PetHelp.Domain.Entities;

namespace PetHelp.AdminOnboarding.Services;

public interface IOnboardingAdminService
{
    Task<IReadOnlyList<OnboardingSubmissionListItem>> GetSubmissionsAsync(OnboardingStatus? status, CancellationToken ct = default);
    Task<OnboardingSubmissionDetail?> GetSubmissionDetailAsync(Guid submissionId, CancellationToken ct = default);
    Task DecideAsync(Guid submissionId, bool approve, string? notes, ClaimsPrincipal user, CancellationToken ct = default);
}
