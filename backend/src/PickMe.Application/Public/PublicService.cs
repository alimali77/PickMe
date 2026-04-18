using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PickMe.Application.Abstractions;
using PickMe.Application.Common;
using PickMe.Application.Reservations;
using PickMe.Domain.Entities;

namespace PickMe.Application.Public;

public sealed record FaqDto(Guid Id, string Question, string Answer, int DisplayOrder);

public interface IPublicService
{
    Task<Result<IReadOnlyList<FaqDto>>> ListFaqsAsync(CancellationToken ct);
    Task<Result<Unit>> SubmitContactAsync(ContactFormDto dto, CancellationToken ct);
}

public sealed class PublicService(
    IApplicationDbContext db,
    IValidator<ContactFormDto> contactValidator) : IPublicService
{
    private readonly IApplicationDbContext _db = db;
    private readonly IValidator<ContactFormDto> _contactValidator = contactValidator;

    public async Task<Result<IReadOnlyList<FaqDto>>> ListFaqsAsync(CancellationToken ct)
    {
        var faqs = await _db.Faqs.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.CreatedAtUtc)
            .Select(f => new FaqDto(f.Id, f.Question, f.Answer, f.DisplayOrder))
            .ToListAsync(ct);
        return Result<IReadOnlyList<FaqDto>>.Ok(faqs);
    }

    public async Task<Result<Unit>> SubmitContactAsync(ContactFormDto dto, CancellationToken ct)
    {
        var validation = await _contactValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            var dict = validation.Errors
                .GroupBy(e => string.IsNullOrEmpty(e.PropertyName) ? "_" : char.ToLowerInvariant(e.PropertyName[0]) + e.PropertyName[1..])
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
            return Result<Unit>.Fail("validation", "Doğrulama hatası.", dict);
        }

        var entity = ContactMessage.Create(Guid.NewGuid(), dto.FirstName, dto.Email, dto.Phone, dto.Subject, dto.Message);
        _db.ContactMessages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Unit>.Ok(Unit.Value);
    }
}
