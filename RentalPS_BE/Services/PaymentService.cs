using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;
using RentalPS_BE.Exceptions;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Rental> _rentalRepository;
        private readonly IRepository<RentalRoom> _rentalRoomRepository;
        private readonly IRepository<RentalStick> _rentalStickRepository;
        private readonly IRentalPricingService _rentalPricingService;

        public PaymentService(
            IRepository<Payment> paymentRepository,
            IRepository<Rental> rentalRepository,
            IRepository<RentalRoom> rentalRoomRepository,
            IRepository<RentalStick> rentalStickRepository,
            IRentalPricingService rentalPricingService,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _rentalRepository = rentalRepository;
            _rentalRoomRepository = rentalRoomRepository;
            _rentalStickRepository = rentalStickRepository;
            _rentalPricingService = rentalPricingService;
            _logger = logger;
        }

        public async Task<List<PaymentResponse>> GetAllAsync(int userId, bool isAdmin)
        {
            var payments = await _paymentRepository.GetAllAsync();

            if (!isAdmin)
            {
                var rentals = await _rentalRepository.GetAllAsync();
                var customerRentalIds = rentals
                    .Where(r => r.UserId == userId)
                    .Select(r => r.Id)
                    .ToHashSet();

                payments = payments
                    .Where(p => customerRentalIds.Contains(p.RentalId))
                    .ToList();
            }

            return payments
                .OrderByDescending(p => p.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<PaymentResponse?> GetByIdAsync(
            int id,
            int userId,
            bool isAdmin)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return null;
            }

            if (!isAdmin)
            {
                var rental = await _rentalRepository.GetByIdAsync(payment.RentalId);
                if (rental == null || rental.UserId != userId)
                {
                    return null;
                }
            }

            return ToResponse(payment);
        }

        public async Task<List<PaymentResponse>?> GetByRentalIdAsync(
            int rentalId,
            int userId,
            bool isAdmin)
        {
            var rental = await _rentalRepository.GetByIdAsync(rentalId);
            if (rental == null || (!isAdmin && rental.UserId != userId))
            {
                return null;
            }

            var payments = await _paymentRepository.GetAllAsync();

            return payments
                .Where(p => p.RentalId == rentalId)
                .OrderByDescending(p => p.Id)
                .Select(ToResponse)
                .ToList();
        }

        public async Task<PaymentResponse?> CreateAsync(
            int userId,
            PaymentRequest request)
        {
            if (request.RentalId <= 0)
            {
                throw new ArgumentException("RentalId harus lebih besar dari 0.");
            }

            if (request.Amount <= 0)
            {
                throw new InvalidPaymentAmountException("Amount harus lebih besar dari 0.");
            }

            if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
            {
                throw new ArgumentException("PaymentMethod tidak valid.");
            }

            var rental = await _rentalRepository.GetByIdAsync(request.RentalId);
            if (rental == null || rental.UserId != userId)
            {
                // Jangan membuka informasi apakah rental milik pengguna lain tersedia.
                return null;
            }

            if (rental.StatusRental == StatusRental.Cancelled)
            {
                throw new ArgumentException(
                    "Pembayaran tidak dapat dibuat untuk rental yang sudah dibatalkan.");
            }

            if (rental.DurationHour <= 0 || rental.ConsoleRate < 0)
            {
                throw new InvalidRentalDurationException("Data durasi atau tarif konsol rental belum valid.");
            }

            var rentalTotal = await SynchronizeRentalTotalAsync(rental);
            if (rentalTotal <= 0)
            {
                throw new InvalidPaymentAmountException(
                    "Total rental belum valid atau belum memiliki nilai pembayaran.");
            }

            var payments = await _paymentRepository.GetAllAsync();

            // Pembayaran Pending dan Success sama-sama mengalokasikan sisa tagihan.
            var reservedAmount = payments
                .Where(p => p.RentalId == rental.Id &&
                            (p.PaymentStatus == PaymentStatus.Pending ||
                             p.PaymentStatus == PaymentStatus.Success))
                .Sum(p => p.Amount);

            var outstandingAmount = rentalTotal - reservedAmount;
            if (outstandingAmount <= 0)
            {
                throw new InvalidPaymentAmountException(
                    "Rental sudah lunas atau tidak memiliki sisa tagihan.");
            }

            if (request.Amount > outstandingAmount)
            {
                throw new InvalidPaymentAmountException(
                    $"Jumlah pembayaran melebihi sisa tagihan. Sisa tagihan: {outstandingAmount:N2}.");
            }

            var payment = new Payment
            {
                RentalId = rental.Id,
                PaymentCode = GeneratePaymentCode(),
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                PaidAt = null
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} ({PaymentCode}) created for rental {RentalId}. Amount: {Amount}, Method: {PaymentMethod}, Status: {PaymentStatus}.", payment.Id, payment.PaymentCode, payment.RentalId, payment.Amount, payment.PaymentMethod, payment.PaymentStatus);

            return ToResponse(payment);
        }

        public async Task<PaymentResponse?> UpdateStatusAsync(
            int id,
            PaymentStatusRequest request)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(PaymentStatus), request.PaymentStatus))
            {
                throw new ArgumentException("PaymentStatus tidak valid.");
            }

            var previousStatus = payment.PaymentStatus;

            if (payment.PaymentStatus != PaymentStatus.Pending)
            {
                throw new ArgumentException(
                    "Status pembayaran hanya dapat diubah ketika masih Pending.");
            }

            if (request.PaymentStatus == PaymentStatus.Pending)
            {
                throw new ArgumentException(
                    "Status tujuan harus Success atau Failed, bukan Pending.");
            }

            if (request.PaymentStatus == PaymentStatus.Success)
            {
                var rental = await _rentalRepository.GetByIdAsync(payment.RentalId);
                if (rental == null)
                {
                    throw new NotFoundException(
                        "Rental yang terhubung dengan pembayaran tidak ditemukan.");
                }

                if (rental.StatusRental == StatusRental.Cancelled)
                {
                    throw new ArgumentException(
                        "Pembayaran untuk rental yang sudah dibatalkan tidak dapat dikonfirmasi.");
                }

                var rentalTotal = await SynchronizeRentalTotalAsync(rental);
                var payments = await _paymentRepository.GetAllAsync();

                var otherReservedAmount = payments
                    .Where(p => p.RentalId == rental.Id &&
                                p.Id != payment.Id &&
                                (p.PaymentStatus == PaymentStatus.Pending ||
                                 p.PaymentStatus == PaymentStatus.Success))
                    .Sum(p => p.Amount);

                if (otherReservedAmount + payment.Amount > rentalTotal)
                {
                    throw new InvalidPaymentAmountException(
                        "Pembayaran tidak dapat dikonfirmasi karena jumlahnya melebihi total tagihan rental.");
                }

                payment.PaidAt = DateTime.UtcNow;
            }
            else
            {
                payment.PaidAt = null;
            }

            payment.PaymentStatus = request.PaymentStatus;
            _paymentRepository.Update(payment);
            await _paymentRepository.SaveChangesAsync();
            _logger.LogInformation("Payment {PaymentId} for rental {RentalId} changed from {PreviousStatus} to {NewStatus}.", payment.Id, payment.RentalId, previousStatus, payment.PaymentStatus);

            return ToResponse(payment);
        }

        private async Task<decimal> SynchronizeRentalTotalAsync(Rental rental)
        {
            var rentalRooms = (await _rentalRoomRepository.GetAllAsync())
                .Where(room => room.RentalId == rental.Id)
                .ToList();
            var rentalSticks = (await _rentalStickRepository.GetAllAsync())
                .Where(stick => stick.RentalId == rental.Id)
                .ToList();

            // Sinkronkan subtotal komponen lama sebelum menghitung tagihan Payment.
            foreach (var room in rentalRooms)
            {
                room.SubTotal = _rentalPricingService.CalculateRoomSubtotal(
                    room.AdditionalHourlyRate, room.Name, rental);
                _rentalRoomRepository.Update(room);
            }

            foreach (var stick in rentalSticks)
            {
                stick.SubTotal = _rentalPricingService.CalculateStickSubtotal(
                    stick.HourlyRate, rental);
                _rentalStickRepository.Update(stick);
            }

            rental.TotalAmount = _rentalPricingService.CalculateTotal(
                rental, rentalRooms, rentalSticks);

            // Payment, detail, dan Rental menggunakan DbContext scoped yang sama.
            // Perubahan subtotal dan TotalAmount ikut tersimpan pada SaveChanges Payment.
            _rentalRepository.Update(rental);
            return rental.TotalAmount;
        }

        private static string GeneratePaymentCode()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
        }

        private static PaymentResponse ToResponse(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                RentalId = payment.RentalId,
                PaymentCode = payment.PaymentCode,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                PaidAt = payment.PaidAt
            };
        }
    }
}
