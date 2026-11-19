namespace RentalPS_BE.Exceptions
{
    public class InvalidPaymentAmountException : Exception
    {
        public InvalidPaymentAmountException(decimal amount, decimal remainingAmount)
            : base($"Jumlah pembayaran ({amount}) melebihi sisa tagihan ({remainingAmount}).")
        {
        }
    }
}
