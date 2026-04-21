using PetHelp.Domain.Entities;

namespace PetHelp.AdminOnboarding.Models;

public sealed record OnboardingSubmissionListItem(
    Guid SubmissionId,
    string OrganizationName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    OnboardingStatus Status,
    string? StaffNotes,
    DateTimeOffset Created,
    DateTimeOffset Updated);

public sealed record OnboardingSubmissionDetail(
    Guid SubmissionId,
    string ApplicantUserId,
    string OrganizationName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string AddressLine,
    string City,
    string Department,
    OnboardingStatus Status,
    string? StaffNotes,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    Guid? ShelterProfileId,
    bool ShelterProfileIsActive,
    IReadOnlyList<OnboardingAttachmentItem> Attachments,
    IReadOnlyList<OnboardingDecisionHistoryItem> DecisionHistory);

public sealed record OnboardingAttachmentItem(
    Guid Id,
    string Label,
    string Category,
    string FileName,
    string ContentType,
    string ReadUrl,
    MediaKind Kind,
    DateTimeOffset Created,
    bool IsImage,
    bool IsPdf);

public sealed record OnboardingDecisionHistoryItem(
    Guid Id,
    OnboardingDecision Decision,
    string DecidedByUserId,
    DateTimeOffset DecidedAt,
    string? Notes);
