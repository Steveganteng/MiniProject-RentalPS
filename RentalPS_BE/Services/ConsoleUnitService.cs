using Microsoft.EntityFrameworkCore;
using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Data;
using RentalPSMiniProject.Models;
using Microsoft.Extensions.Logging;

namespace RentalPS_BE.Services
{
    public class ConsoleUnitService : IConsoleUnitService
    {
        private readonly ILogger<ConsoleUnitService> _logger;
        private readonly IRepository<ConsoleUnit> _repository;
        private readonly RentalPSDBContext _context;

        public ConsoleUnitService(
            IRepository<ConsoleUnit> repository,
            RentalPSDBContext context,
            ILogger<ConsoleUnitService> logger)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }

        public async Task<List<ConsoleUnitResponse>> GetAllAsync()
        {
            var units = await _context.ConsoleUnits
                .AsNoTracking()
                .Include(unit => unit.ConsoleType)
                .Include(unit => unit.Room)
                .OrderBy(unit => unit.UnitCode)
                .ToListAsync();

            return units.Select(ToResponse).ToList();
        }

        public async Task<ConsoleUnitResponse?> GetByIdAsync(int id)
        {
            var unit = await _context.ConsoleUnits
                .AsNoTracking()
                .Include(item => item.ConsoleType)
                .Include(item => item.Room)
                .FirstOrDefaultAsync(item => item.Id == id);

            return unit == null ? null : ToResponse(unit);
        }

        public async Task<ConsoleUnitResponse> CreateAsync(ConsoleUnitRequest request)
        {
            var status = ParseAdminManagedStatus(request.StatusConsole);
            await ValidateReferencesAsync(request, requireAvailableRoom: status == StatusConsole.Available);
            await EnsureUnitCodeIsUniqueAsync(request.UnitCode, null);

            var unit = new ConsoleUnit
            {
                ConsoleTypeId = request.ConsoleTypeId,
                RoomId = request.RoomId,
                UnitCode = request.UnitCode.Trim(),
                StatusConsole = status
            };

            await _repository.AddAsync(unit);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console unit {ConsoleUnitId} ({UnitCode}) created with status {Status}.", unit.Id, unit.UnitCode, unit.StatusConsole);

            return (await GetByIdAsync(unit.Id))!;
        }

        public async Task<ConsoleUnitResponse?> UpdateAsync(int id, ConsoleUnitRequest request)
        {
            var unit = await _context.ConsoleUnits
                .FirstOrDefaultAsync(item => item.Id == id);

            if (unit == null)
            {
                return null;
            }

            var requestedStatus = ParseAdminManagedStatus(request.StatusConsole);
            await ValidateReferencesAsync(request, requireAvailableRoom: requestedStatus == StatusConsole.Available);
            await EnsureUnitCodeIsUniqueAsync(request.UnitCode, id);

            var relatedRentals = await _context.Rentals
                .Where(rental => rental.ConsoleUnitId == id)
                .ToListAsync();

            var hasOpenRental = relatedRentals.Any(rental =>
                (rental.StatusRental is StatusRental.Pending or StatusRental.Active));

            if (relatedRentals.Any(rental => rental.StatusRental == StatusRental.Active))
            {
                throw new ArgumentException(
                    "Console Unit sedang digunakan oleh rental Active. Selesaikan rental terlebih dahulu sebelum mengubah inventaris.");
            }

            if (hasOpenRental && (requestedStatus is StatusConsole.Maintenance or StatusConsole.Inactive))
            {
                throw new ArgumentException(
                    "Console Unit tidak dapat diubah menjadi Maintenance/Inactive karena masih memiliki rental Pending atau Active. Selesaikan atau batalkan rental terlebih dahulu.");
            }

            var hasRentalHistory = relatedRentals.Count > 0;
            var identityChanged =
                unit.ConsoleTypeId != request.ConsoleTypeId ||
                unit.RoomId != request.RoomId ||
                !string.Equals(unit.UnitCode, request.UnitCode.Trim(), StringComparison.Ordinal);

            if (hasRentalHistory && identityChanged)
            {
                throw new ArgumentException(
                    "ConsoleType, Room, dan UnitCode tidak dapat diubah setelah Console Unit memiliki histori rental. Buat unit baru untuk menjaga histori transaksi.");
            }

            unit.ConsoleTypeId = request.ConsoleTypeId;
            unit.RoomId = request.RoomId;
            unit.UnitCode = request.UnitCode.Trim();
            unit.StatusConsole = requestedStatus;

            _repository.Update(unit);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console unit {ConsoleUnitId} ({UnitCode}) updated. Status: {Status}.", unit.Id, unit.UnitCode, unit.StatusConsole);
            return await GetByIdAsync(id);
        }

        // Endpoint DELETE melakukan soft delete: histori ConsoleUnit tetap disimpan.
        public async Task<bool> DeactivateAsync(int id)
        {
            var unit = await _context.ConsoleUnits
                .FirstOrDefaultAsync(item => item.Id == id);

            if (unit == null)
            {
                return false;
            }

            var hasOpenRental = await _context.Rentals.AnyAsync(rental =>
                rental.ConsoleUnitId == id &&
                (rental.StatusRental == StatusRental.Pending ||
                 rental.StatusRental == StatusRental.Active));

            if (hasOpenRental)
            {
                throw new ArgumentException(
                    "Console Unit tidak dapat dinonaktifkan karena masih memiliki rental Pending atau Active.");
            }

            unit.StatusConsole = StatusConsole.Inactive;
            _repository.Update(unit);
            await _repository.SaveChangesAsync();
            _logger.LogInformation("Console unit {ConsoleUnitId} ({UnitCode}) deactivated.", unit.Id, unit.UnitCode);
            return true;
        }

        private async Task ValidateReferencesAsync(ConsoleUnitRequest request, bool requireAvailableRoom)
        {
            if (request.ConsoleTypeId <= 0)
            {
                throw new ArgumentException("ConsoleTypeId wajib lebih besar dari 0.");
            }

            if (string.IsNullOrWhiteSpace(request.UnitCode))
            {
                throw new ArgumentException("UnitCode wajib diisi.");
            }

            var consoleType = await _context.ConsoleTypes
                .FirstOrDefaultAsync(item => item.Id == request.ConsoleTypeId);

            if (consoleType == null || !consoleType.IsActive)
            {
                throw new ArgumentException("ConsoleType tidak ditemukan atau sedang tidak aktif.");
            }

            if (request.RoomId.HasValue)
            {
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(item => item.Id == request.RoomId.Value);

                if (room == null)
                {
                    throw new ArgumentException("RoomId tidak ditemukan.");
                }

                if (requireAvailableRoom && room.StatusRoom != StatusRoom.Available)
                {
                    throw new ArgumentException(
                        "Console Unit yang berstatus Available hanya dapat ditempatkan pada Room yang berstatus Available.");
                }
            }
        }

        private async Task EnsureUnitCodeIsUniqueAsync(string unitCode, int? excludedId)
        {
            var normalizedCode = unitCode.Trim();
            var duplicateExists = await _context.ConsoleUnits.AnyAsync(unit =>
                (!excludedId.HasValue || unit.Id != excludedId.Value) &&
                unit.UnitCode.ToLower() == normalizedCode.ToLower());

            if (duplicateExists)
            {
                throw new ArgumentException($"UnitCode '{normalizedCode}' sudah digunakan.");
            }
        }

        private static StatusConsole ParseAdminManagedStatus(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Enum.GetNames<StatusConsole>().Any(name =>
                    string.Equals(name, value.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("StatusConsole tidak valid.");
            }

            var status = Enum.Parse<StatusConsole>(value.Trim(), ignoreCase: true);
            if (status == StatusConsole.In_Use)
            {
                throw new ArgumentException(
                    "Status In_Use diatur otomatis ketika Admin mengubah StatusRental menjadi Active.");
            }

            return status;
        }

        private static ConsoleUnitResponse ToResponse(ConsoleUnit unit)
        {
            return new ConsoleUnitResponse
            {
                Id = unit.Id,
                ConsoleTypeId = unit.ConsoleTypeId,
                ConsoleTypeName = unit.ConsoleType?.Name ?? string.Empty,
                RoomId = unit.RoomId,
                RoomNumber = unit.Room?.RoomNumber,
                UnitCode = unit.UnitCode,
                StatusConsole = unit.StatusConsole.ToString()
            };
        }
    }
}
