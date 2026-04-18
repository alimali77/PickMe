namespace PickMe.Domain.Common;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string from, string to, string action)
        : base(
            "reservation.invalid_transition",
            $"Geçersiz durum geçişi: {action} işlemi {from} durumunda yapılamaz (hedef: {to}).")
    {
    }
}
