namespace RentalPS_BE.Exceptions
{
    public class InventoryNotAvailableException : Exception
    {
        public InventoryNotAvailableException(string inventoryType, string inventoryCode)
            : base($"{inventoryType} dengan kode {inventoryCode} sedang tidak tersedia.")
        {
        }
    }
}
