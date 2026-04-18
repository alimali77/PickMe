using FluentValidation;
using PickMe.Application.Auth;

namespace PickMe.Application.Reservations;

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.ServiceType).IsInEnum().WithMessage("Hizmet türü seçiniz.");

        RuleFor(x => x.ReservationDateTimeUtc)
            .Must(BeInFutureWithMinLeadTime)
            .WithMessage(ValidationMessages.ReservationDateTooSoon());

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(ValidationMessages.ReservationAddressRequired)
            .MinimumLength(ValidationRules.AddressMin).WithMessage(ValidationMessages.ReservationAddressRequired)
            .MaximumLength(ValidationRules.AddressMax).WithMessage(ValidationMessages.ReservationAddressRequired);

        RuleFor(x => x.Lat)
            .InclusiveBetween(ValidationRules.LatMin, ValidationRules.LatMax)
            .WithMessage(ValidationMessages.ReservationOutsideTurkey);
        RuleFor(x => x.Lng)
            .InclusiveBetween(ValidationRules.LngMin, ValidationRules.LngMax)
            .WithMessage(ValidationMessages.ReservationOutsideTurkey);

        RuleFor(x => x.Note)
            .MaximumLength(ValidationRules.NoteMax).WithMessage(ValidationMessages.NoteTooLong)
            .When(x => !string.IsNullOrEmpty(x.Note));

        RuleFor(x => x.PlaceSelectedFromAutocomplete)
            .Equal(true).WithMessage(ValidationMessages.ReservationAddressRequired);
    }

    private static bool BeInFutureWithMinLeadTime(DateTime reservationUtc)
    {
        var now = DateTime.UtcNow;
        return reservationUtc >= now.AddMinutes(ValidationRules.ReservationMinMinutesAhead);
    }
}

public sealed class AssignDriverValidator : AbstractValidator<AssignDriverDto>
{
    public AssignDriverValidator()
    {
        RuleFor(x => x.DriverId).NotEqual(Guid.Empty).WithMessage("Şoför seçiniz.");
    }
}

public sealed class CancelReservationValidator : AbstractValidator<CancelReservationDto>
{
    public CancelReservationValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("İptal sebebi en fazla 1000 karakter olabilir.");
    }
}

public sealed class AdminCancelReservationValidator : AbstractValidator<CancelReservationDto>
{
    public AdminCancelReservationValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("İptal sebebi zorunludur.")
            .MaximumLength(1000).WithMessage("İptal sebebi en fazla 1000 karakter olabilir.");
    }
}

public sealed class RateReservationValidator : AbstractValidator<RateReservationDto>
{
    public RateReservationValidator()
    {
        RuleFor(x => x.Score)
            .InclusiveBetween(ValidationRules.RatingScoreMin, ValidationRules.RatingScoreMax)
            .WithMessage(ValidationMessages.RatingScoreRange);
        RuleFor(x => x.Comment)
            .MaximumLength(ValidationRules.CommentMax).WithMessage("Yorum en fazla 500 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}

public sealed class ContactFormValidator : AbstractValidator<ContactFormDto>
{
    public ContactFormValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.FirstNameRequired)
            .Length(ValidationRules.FirstNameMin, ValidationRules.FirstNameMax).WithMessage(ValidationMessages.FirstNameLength);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(ValidationMessages.PhoneRequired)
            .Matches(ValidationRules.PhoneRegex).WithMessage(ValidationMessages.PhoneFormat);
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Konu boş bırakılamaz.")
            .MaximumLength(ValidationRules.SubjectMax);
        RuleFor(x => x.Message)
            .NotEmpty()
            .MinimumLength(ValidationRules.MessageMin).WithMessage("Mesaj en az 10 karakter olmalıdır.")
            .MaximumLength(ValidationRules.MessageMax);
    }
}

public sealed record ContactFormDto(
    string FirstName,
    string Email,
    string Phone,
    string Subject,
    string Message);
