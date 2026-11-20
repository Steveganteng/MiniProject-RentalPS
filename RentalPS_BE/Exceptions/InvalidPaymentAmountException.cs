namespace RentalPS_BE.Exceptions;

/// <summary>Digunakan ketika nominal pembayaran tidak valid atau melebihi tagihan.</summary>
public sealed class InvalidPaymentAmountException : Exception
{
    public InvalidPaymentAmountException(string message) : base(message) { }
}
