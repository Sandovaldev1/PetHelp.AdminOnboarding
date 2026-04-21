using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PetHelp.AdminOnboarding.Models;
using PetHelp.AdminOnboarding.Security;
using PetHelp.Domain.Entities;
using PetHelp.Infrastructure.Persistence;
using PetHelp.Infrastructure.Storage;

namespace PetHelp.AdminOnboarding.Services;

public sealed class OnboardingAdminService : IOnboardingAdminService
{
    private readonly AppDbContext _db;
    private readonly IBlobStorage _blobStorage;

    public OnboardingAdminService(AppDbContext db, IBlobStorage blobStorage)
    {
        _db = db;
        _blobStorage = blobStorage;
    }

    public async Task<IReadOnlyList<OnboardingSubmissionListItem>> GetSubmissionsAsync(OnboardingStatus? status, CancellationToken ct = default)
    {
        var query = _db.ShelterOnboardingSubmissions.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.Updated)
            .ThenByDescending(x => x.Created)
            .Take(200)
            .Select(x => new OnboardingSubmissionListItem(
                x.Id,
                x.OrganizationName,
                x.ContactName,
                x.ContactEmail,
                x.ContactPhone,
                x.Status,
                x.StaffNotes,
                x.Created,
                x.Updated))
            .ToListAsync(ct);
    }

    public async Task<OnboardingSubmissionDetail?> GetSubmissionDetailAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _db.ShelterOnboardingSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == submissionId, ct);

        if (submission is null)
        {
            return null;
        }

        var shelterProfile = await _db.ShelterProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OwnerUserId == submission.ApplicantUserId, ct);

        var attachments = await _db.MediaAssets.AsNoTracking()
            .Where(x => x.OwnerType == MediaOwnerType.ShelterOnboardingSubmission && x.OwnerId == submission.Id)
            .OrderBy(x => x.Created)
            .ToListAsync(ct);

        var history = await _db.OnboardingReviewDecisions.AsNoTracking()
            .Where(x => x.SubmissionId == submission.Id)
            .OrderByDescending(x => x.DecidedAt)
            .Select(x => new OnboardingDecisionHistoryItem(
                x.Id,
                x.Decision,
                x.DecidedByUserId,
                x.DecidedAt,
                x.Notes))
            .ToListAsync(ct);

        return new OnboardingSubmissionDetail(
            submission.Id,
            submission.ApplicantUserId,
            submission.OrganizationName,
            submission.ContactName,
            submission.ContactEmail,
            submission.ContactPhone,
            submission.AddressLine,
            submission.City,
            submission.Department,
            submission.Status,
            submission.StaffNotes,
            submission.Created,
            submission.Updated,
            shelterProfile?.Id,
            shelterProfile?.IsActive ?? false,
            attachments.Select(MapAttachment).ToList(),
            history);
    }

    public async Task DecideAsync(Guid submissionId, bool approve, string? notes, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var actorId = AdminIdentityResolver.GetDecisionActorId(user);
        var normalizedNotes = NormalizeNotes(notes);
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var submission = await _db.ShelterOnboardingSubmissions
            .FirstOrDefaultAsync(x => x.Id == submissionId, ct)
            ?? throw new InvalidOperationException("No encontramos la solicitud indicada.");

        submission.Status = approve ? OnboardingStatus.Approved : OnboardingStatus.Rejected;
        submission.StaffNotes = normalizedNotes;
        submission.Updated = now;

        if (approve)
        {
            var shelterProfile = await _db.ShelterProfiles
                .FirstOrDefaultAsync(x => x.OwnerUserId == submission.ApplicantUserId, ct);

            if (shelterProfile is null)
            {
                shelterProfile = new ShelterProfile
                {
                    OwnerUserId = submission.ApplicantUserId,
                    OrganizationName = submission.OrganizationName,
                    ContactName = submission.ContactName,
                    ContactEmail = submission.ContactEmail,
                    ContactPhone = submission.ContactPhone,
                    AddressLine = submission.AddressLine,
                    City = submission.City,
                    Department = submission.Department,
                    IsActive = true,
                    Created = now,
                    Updated = now
                };

                _db.ShelterProfiles.Add(shelterProfile);
            }
            else
            {
                shelterProfile.OrganizationName = submission.OrganizationName;
                shelterProfile.ContactName = submission.ContactName;
                shelterProfile.ContactEmail = submission.ContactEmail;
                shelterProfile.ContactPhone = submission.ContactPhone;
                shelterProfile.AddressLine = submission.AddressLine;
                shelterProfile.City = submission.City;
                shelterProfile.Department = submission.Department;
                shelterProfile.IsActive = true;
                shelterProfile.Updated = now;
            }
        }

        _db.OnboardingReviewDecisions.Add(new OnboardingReviewDecision
        {
            SubmissionId = submission.Id,
            DecidedByUserId = actorId,
            DecidedAt = now,
            Decision = approve ? OnboardingDecision.Approved : OnboardingDecision.Rejected,
            Notes = normalizedNotes
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private OnboardingAttachmentItem MapAttachment(MediaAsset asset)
    {
        var readUrl = BuildReadUrl(asset);
        var fileName = Path.GetFileName(asset.RelativePath);
        var contentType = asset.ContentType?.Trim().ToLowerInvariant() ?? "application/octet-stream";
        var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        return new OnboardingAttachmentItem(
            asset.Id,
            ResolveAttachmentLabel(asset.Kind),
            ResolveAttachmentCategory(asset.Kind),
            string.IsNullOrWhiteSpace(fileName) ? "archivo" : fileName,
            contentType,
            readUrl,
            asset.Kind,
            asset.Created,
            isImage,
            isPdf);
    }

    private string BuildReadUrl(MediaAsset asset)
    {
        try
        {
            return asset.IsPublic
                ? _blobStorage.BuildPublicUrl(asset.RelativePath)
                : _blobStorage.BuildPrivateReadUrl(asset.RelativePath, TimeSpan.FromMinutes(30));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ResolveAttachmentLabel(MediaKind kind) => kind switch
    {
        MediaKind.Receipt => "RUC",
        MediaKind.Construction => "Documento del representante",
        MediaKind.Other => "Foto del refugio",
        MediaKind.PetBefore => "Adjunto",
        MediaKind.PetAfter => "Adjunto",
        _ => "Adjunto"
    };

    private static string ResolveAttachmentCategory(MediaKind kind) => kind switch
    {
        MediaKind.Receipt => "Documentos tributarios",
        MediaKind.Construction => "Identidad del representante",
        MediaKind.Other => "Fotos del refugio",
        _ => "Otros"
    };

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        return notes.Trim();
    }
}
