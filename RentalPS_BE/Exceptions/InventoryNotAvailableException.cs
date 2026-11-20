namespace RentalPS_BE.Exceptions;

/// <summary>Digunakan ketika inventaris tidak tersedia atau jadwalnya bentrok.</summary>
public sealed class InventoryNotAvailableException : Exception
{
    public InventoryNotAvailableException(string message) : base(message) { }
}
