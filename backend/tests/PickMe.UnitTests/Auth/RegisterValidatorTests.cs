using FluentAssertions;
using PickMe.Application.Auth;
using Xunit;

namespace PickMe.UnitTests.Auth;

public class RegisterValidatorTests
{
    private static readonly RegisterCustomerValidator V = new();

    private static RegisterCustomerDto Valid() => new(
        "Ali", "Yılmaz", "ali@pickme.tr", "05551234567",
        "Strong1Pass", "Strong1Pass", true);

    [Fact]
    public void Valid_Input_Passes()
    {
        V.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ThisIsAFirstNameThatIsWayLongerThanFiftyCharactersDefinitely")]
    public void First_Name_Length_Is_Enforced(string first)
    {
        var r = V.Validate(Valid() with { FirstName = first });
        r.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("@pickme.tr")]
    public void Email_Format_Is_Enforced(string email)
    {
        var r = V.Validate(Valid() with { Email = email });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorMessage == ValidationMessages.EmailFormat);
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("+905551234")]
    [InlineData("abcdefghij")]
    [InlineData("05551")]
    public void Phone_Format_Is_Enforced(string phone)
    {
        var r = V.Validate(Valid() with { Phone = phone });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorMessage == ValidationMessages.PhoneFormat);
    }

    [Theory]
    [InlineData("05551234567")]
    [InlineData("+905551234567")]
    [InlineData("5551234567")]
    public void Valid_Turkish_Mobile_Passes(string phone)
    {
        V.Validate(Valid() with { Phone = phone }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]       // 7 chars
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")]  // no digit
    public void Password_Complexity_Enforced(string password)
    {
        var r = V.Validate(Valid() with { Password = password, PasswordConfirm = password });
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Passwords_Must_Match()
    {
        var r = V.Validate(Valid() with { PasswordConfirm = "Different1Pass" });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorMessage == ValidationMessages.PasswordConfirmMismatch);
    }

    [Fact]
    public void Kvkk_Must_Be_Accepted()
    {
        var r = V.Validate(Valid() with { KvkkAccepted = false });
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorMessage == ValidationMessages.KvkkRequired);
    }
}
