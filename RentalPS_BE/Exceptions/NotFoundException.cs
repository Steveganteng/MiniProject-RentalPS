namespace RentalPS_BE.Exceptions;

/// <summary>Digunakan ketika resource yang dibutuhkan tidak ditemukan.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
