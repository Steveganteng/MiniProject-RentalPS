using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Data;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class ConsoleUnitService : IConsoleUnitService
    {
        private readonly IRepository<ConsoleUnit> _repository;
        private readonly RentalPSDBContext _context;

        public ConsoleUnitService(IRepository<ConsoleUnit> repository, RentalPSDBContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<List<ConsoleUnitResponse>> GetAllAsync()
        {
            var units = await _repository.GetAllAsync();
            return units.Select(u => MapToResponse(u)).ToList();
        }

        public async Task<ConsoleUnitResponse?> GetByIdAsync(int id)
        {
            var unit = await _repository.GetByIdAsync(id);
            return unit != null ? MapToResponse(unit) : null;
        }

        public async Task<ConsoleUnitResponse> CreateAsync(ConsoleUnitRequest request)
        {
            var unit = new ConsoleUnit
            {
                ConsoleTypeId = request.ConsoleTypeId,
                RoomId = request.RoomId,
                UnitCode = request.UnitCode,
                StatusConsole = Enum.Parse<StatusConsole>(request.StatusConsole)
            };

            await _repository.AddAsync(unit);
            await _repository.SaveChangesAsync();

            return MapToResponse(unit);
        }

        public async Task<ConsoleUnitResponse?> UpdateAsync(int id, ConsoleUnitRequest request)
        {
            var unit = await _repository.GetByIdAsync(id);
            if (unit == null)
                return null;

            unit.ConsoleTypeId = request.ConsoleTypeId;
            unit.RoomId = request.RoomId;
            unit.UnitCode = request.UnitCode;
            unit.StatusConsole = Enum.Parse<StatusConsole>(request.StatusConsole);

            _repository.Update(unit);
            await _repository.SaveChangesAsync();

            return MapToResponse(unit);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var unit = await _repository.GetByIdAsync(id);
            if (unit == null)
                return false;

            _repository.Delete(unit);
            await _repository.SaveChangesAsync();

            return true;
        }

        private ConsoleUnitResponse MapToResponse(ConsoleUnit unit)
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