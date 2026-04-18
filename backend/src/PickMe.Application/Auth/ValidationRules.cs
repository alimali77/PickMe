using System.Text.RegularExpressions;

namespace PickMe.Application.Auth;

/// <summary>
/// /shared/validation-rules.json tablosunun birebir kopyası.
/// Frontend Zod ile aynı limitleri kullanır; Faz 6'da drift testi bu sabitleri shared JSON ile karşılaştırır.
/// </summary>
public static class ValidationRules
{
    public const int FirstNameMin = 2;
    public const int FirstNameMax = 50;
    public const int LastNameMin = 2;
    public const int LastNameMax = 50;
    public const int EmailMax = 256;
    public const int PasswordMin = 8;
    public const int PasswordMax = 128;
    public const int NoteMax = 500;
    public const int AddressMin = 5;
    public const int AddressMax = 300;
    public const int CommentMax = 500;
    public const int SubjectMax = 120;
    public const int MessageMin = 10;
    public const int MessageMax = 2000;

    public const double LatMin = 35.8, LatMax = 42.2;
    public const double LngMin = 25.6, LngMax = 44.9;

    public const int LoginMaxFailedAttempts = 5;
    public const int LoginLockoutMinutes = 15;
    public const int EmailVerificationTokenHours = 24;
    public const int PasswordResetTokenMinutes = 60;

    public const int ReservationMinMinutesAhead = 30;
    public const int ReservationStepMinutes = 15;

    public const int RatingScoreMin = 1;
    public const int RatingScoreMax = 5;
    public const int RatingEditWindowHours = 24;

    public static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    public static readonly Regex PhoneRegex = new(@"^(\+90|0)?5[0-9]{9}$", RegexOptions.Compiled);
    public static readonly Regex UppercaseRegex = new("[A-Z]", RegexOptions.Compiled);
    public static readonly Regex LowercaseRegex = new("[a-z]", RegexOptions.Compiled);
    public static readonly Regex DigitRegex = new(@"\d", RegexOptions.Compiled);
}

public static class ValidationMessages
{
    public const string FirstNameRequired = "Ad boş bırakılamaz.";
    public const string FirstNameLength = "Ad 2-50 karakter arasında olmalıdır.";
    public const string LastNameRequired = "Soyad boş bırakılamaz.";
    public const string LastNameLength = "Soyad 2-50 karakter arasında olmalıdır.";
    public const string EmailRequired = "E-posta boş bırakılamaz.";
    public const string EmailFormat = "Geçerli bir e-posta adresi giriniz.";
    public const string PhoneRequired = "Telefon numarası boş bırakılamaz.";
    public const string PhoneFormat = "Geçerli bir Türkiye cep telefonu numarası giriniz (ör: 05551234567).";
    public const string PasswordRequired = "Şifre boş bırakılamaz.";
    public const string PasswordLength = "Şifre en az 8 karakter olmalıdır.";
    public const string PasswordComplexity = "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.";
    public const string PasswordConfirmMismatch = "Şifreler eşleşmiyor.";
    public const string KvkkRequired = "Devam etmek için KVKK metnini onaylamanız gerekir.";
    public const string LoginInvalid = "E-posta veya şifre hatalı.";
    public const string AccountLocked = "Hesabınız 15 dakika kilitlendi, lütfen daha sonra tekrar deneyin.";
    public const string EmailNotVerified = "Lütfen önce e-posta adresinizi doğrulayın.";
    public const string EmailAlreadyRegistered = "Bu e-posta adresi zaten kayıtlı.";
    public const string InvalidOrExpiredToken = "Geçersiz veya süresi dolmuş bağlantı.";
    public const string GenericSuccessNeutral = "İşleminiz alındı. Mevcutsa e-postanıza bilgilendirme yapılacaktır.";
    public const string MustChangePassword = "Devam etmeden önce şifrenizi değiştirmelisiniz.";
    public const string NotAuthenticated = "Bu işlem için giriş yapmış olmanız gerekir.";
    public const string AccountInactive = "Hesabınız devre dışı. Lütfen yönetimle iletişime geçiniz.";
}
