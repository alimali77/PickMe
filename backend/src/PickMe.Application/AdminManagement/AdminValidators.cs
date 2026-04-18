using FluentValidation;
using PickMe.Application.Auth;

namespace PickMe.Application.AdminManagement;

public sealed class CreateDriverValidator : AbstractValidator<CreateDriverDto>
{
    public CreateDriverValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.FirstNameRequired)
            .Length(ValidationRules.FirstNameMin, ValidationRules.FirstNameMax).WithMessage(ValidationMessages.FirstNameLength);
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(ValidationMessages.LastNameRequired)
            .Length(ValidationRules.LastNameMin, ValidationRules.LastNameMax).WithMessage(ValidationMessages.LastNameLength);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(ValidationMessages.PhoneRequired)
            .Matches(ValidationRules.PhoneRegex).WithMessage(ValidationMessages.PhoneFormat);
        RuleFor(x => x.InitialPassword!)
            .MinimumLength(ValidationRules.PasswordMin).WithMessage(ValidationMessages.PasswordLength)
            .Must(p => ValidationRules.UppercaseRegex.IsMatch(p)
                       && ValidationRules.LowercaseRegex.IsMatch(p)
                       && ValidationRules.DigitRegex.IsMatch(p))
            .WithMessage(ValidationMessages.PasswordComplexity)
            .When(x => !string.IsNullOrEmpty(x.InitialPassword));
    }
}

public sealed class UpdateDriverValidator : AbstractValidator<UpdateDriverDto>
{
    public UpdateDriverValidator()
    {
        RuleFor(x => x.FirstName)
            .Length(ValidationRules.FirstNameMin, ValidationRules.FirstNameMax).WithMessage(ValidationMessages.FirstNameLength);
        RuleFor(x => x.LastName)
            .Length(ValidationRules.LastNameMin, ValidationRules.LastNameMax).WithMessage(ValidationMessages.LastNameLength);
        RuleFor(x => x.Phone)
            .Matches(ValidationRules.PhoneRegex).WithMessage(ValidationMessages.PhoneFormat);
    }
}

public sealed class CreateRecipientValidator : AbstractValidator<CreateRecipientDto>
{
    public CreateRecipientValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
    }
}

public sealed class CreateFaqValidator : AbstractValidator<CreateFaqDto>
{
    public CreateFaqValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateFaqValidator : AbstractValidator<UpdateFaqDto>
{
    public UpdateFaqValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateAdminValidator : AbstractValidator<CreateAdminDto>
{
    public CreateAdminValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
        RuleFor(x => x.Password)
            .MinimumLength(ValidationRules.PasswordMin).WithMessage(ValidationMessages.PasswordLength)
            .Must(p => ValidationRules.UppercaseRegex.IsMatch(p)
                       && ValidationRules.LowercaseRegex.IsMatch(p)
                       && ValidationRules.DigitRegex.IsMatch(p))
            .WithMessage(ValidationMessages.PasswordComplexity);
    }
}

public sealed class UpdateAdminValidator : AbstractValidator<UpdateAdminDto>
{
    public UpdateAdminValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
    }
}

public sealed class FlagRatingValidator : AbstractValidator<FlagRatingDto>
{
    public FlagRatingValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
