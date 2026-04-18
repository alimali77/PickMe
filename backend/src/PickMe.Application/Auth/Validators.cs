using FluentValidation;

namespace PickMe.Application.Auth;

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerDto>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.FirstNameRequired)
            .Length(ValidationRules.FirstNameMin, ValidationRules.FirstNameMax).WithMessage(ValidationMessages.FirstNameLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(ValidationMessages.LastNameRequired)
            .Length(ValidationRules.LastNameMin, ValidationRules.LastNameMax).WithMessage(ValidationMessages.LastNameLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(ValidationRules.EmailMax).WithMessage(ValidationMessages.EmailFormat)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(ValidationMessages.PhoneRequired)
            .Matches(ValidationRules.PhoneRegex).WithMessage(ValidationMessages.PhoneFormat);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MinimumLength(ValidationRules.PasswordMin).WithMessage(ValidationMessages.PasswordLength)
            .MaximumLength(ValidationRules.PasswordMax).WithMessage(ValidationMessages.PasswordComplexity)
            .Must(p => ValidationRules.UppercaseRegex.IsMatch(p)
                       && ValidationRules.LowercaseRegex.IsMatch(p)
                       && ValidationRules.DigitRegex.IsMatch(p))
            .WithMessage(ValidationMessages.PasswordComplexity);

        RuleFor(x => x.PasswordConfirm)
            .Equal(x => x.Password).WithMessage(ValidationMessages.PasswordConfirmMismatch);

        RuleFor(x => x.KvkkAccepted)
            .Equal(true).WithMessage(ValidationMessages.KvkkRequired);
    }
}

public sealed class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
        RuleFor(x => x.Password).NotEmpty().WithMessage(ValidationMessages.PasswordRequired);
    }
}

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .Matches(ValidationRules.EmailRegex).WithMessage(ValidationMessages.EmailFormat);
    }
}

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password)
            .MinimumLength(ValidationRules.PasswordMin).WithMessage(ValidationMessages.PasswordLength)
            .MaximumLength(ValidationRules.PasswordMax).WithMessage(ValidationMessages.PasswordComplexity)
            .Must(p => ValidationRules.UppercaseRegex.IsMatch(p)
                       && ValidationRules.LowercaseRegex.IsMatch(p)
                       && ValidationRules.DigitRegex.IsMatch(p))
            .WithMessage(ValidationMessages.PasswordComplexity);
        RuleFor(x => x.PasswordConfirm).Equal(x => x.Password).WithMessage(ValidationMessages.PasswordConfirmMismatch);
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(ValidationMessages.PasswordRequired);
        RuleFor(x => x.NewPassword)
            .MinimumLength(ValidationRules.PasswordMin).WithMessage(ValidationMessages.PasswordLength)
            .MaximumLength(ValidationRules.PasswordMax).WithMessage(ValidationMessages.PasswordComplexity)
            .Must(p => ValidationRules.UppercaseRegex.IsMatch(p)
                       && ValidationRules.LowercaseRegex.IsMatch(p)
                       && ValidationRules.DigitRegex.IsMatch(p))
            .WithMessage(ValidationMessages.PasswordComplexity);
        RuleFor(x => x.NewPasswordConfirm).Equal(x => x.NewPassword).WithMessage(ValidationMessages.PasswordConfirmMismatch);
    }
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .Length(ValidationRules.FirstNameMin, ValidationRules.FirstNameMax).WithMessage(ValidationMessages.FirstNameLength);
        RuleFor(x => x.LastName)
            .Length(ValidationRules.LastNameMin, ValidationRules.LastNameMax).WithMessage(ValidationMessages.LastNameLength);
        RuleFor(x => x.Phone)
            .Matches(ValidationRules.PhoneRegex).WithMessage(ValidationMessages.PhoneFormat);
    }
}
