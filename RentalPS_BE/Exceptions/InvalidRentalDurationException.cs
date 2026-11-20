namespace RentalPS_BE.Exceptions;

/// <summary>Digunakan ketika rentang waktu atau durasi rental tidak valid.</summary>
public sealed class InvalidRentalDurationException : Exception
{
    public InvalidRentalDurationException(string message) : base(message) { }
}
