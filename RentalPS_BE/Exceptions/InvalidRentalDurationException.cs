namespace RentalPS_BE.Exceptions
{
    public class InvalidRentalDurationException : Exception
    {
        public InvalidRentalDurationException(decimal durationHours)
            : base($"Durasi rental ({durationHours}) harus lebih besar dari 0.")
        {
        }
    }
}
