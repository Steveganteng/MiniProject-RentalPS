using RentalPS_BE.DTOs;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Models;

namespace RentalPS_BE.Services
{
    public class StickService : IStickService
    {
        private readonly IRepository<Stick> _stickRepository;
        private readonly IRepository<StickType> _stickTypeRepository;

        public StickService(
            IRepository<Stick> stickRepository,
            IRepository<StickType> stickTypeRepository)
        {
            _stickRepository = stickRepository;
            _stickTypeRepository = stickTypeRepository;
        }

        public async Task<List<StickResponse>> GetAllAsync()
        {
            var sticks = await _stickRepository.GetAllAsync();

            return sticks
                .Select(ToResponse)
                .ToList();
        }

        public async Task<StickResponse?> GetByIdAsync(int id)
        {
            var stick = await _stickRepository.GetByIdAsync(id);

            return stick == null
                ? null
                : ToResponse(stick);
        }

        public async Task<StickResponse> CreateAsync(StickRequest request)
        {
            var stickType = await _stickTypeRepository.GetByIdAsync(request.StickTypeId);

            if (stickType == null)
            {
                throw new ArgumentException("StickTypeId tidak ditemukan.");
            }

            if (!stickType.IsActive)
            {
                throw new ArgumentException("Jenis stik sedang tidak aktif.");
            }

            var stick = new Stick
            {
                StickTypeId = request.StickTypeId,
                StickCode = request.StickCode.Trim(),
                StatusStick = request.StatusStick
            };

            await _stickRepository.AddAsync(stick);
            await _stickRepository.SaveChangesAsync();

            return ToResponse(stick);
        }

        public async Task<StickResponse?> UpdateAsync(int id, StickRequest request)
        {
            var stick = await _stickRepository.GetByIdAsync(id);

            if (stick == null)
            {
                return null;
            }

            var stickType = await _stickTypeRepository.GetByIdAsync(request.StickTypeId);

            if (stickType == null)
            {
                throw new ArgumentException("StickTypeId tidak ditemukan.");
            }

            if (!stickType.IsActive)
            {
                throw new ArgumentException("Jenis stik sedang tidak aktif.");
            }

            stick.StickTypeId = request.StickTypeId;
            stick.StickCode = request.StickCode.Trim();
            stick.StatusStick = request.StatusStick;

            _stickRepository.Update(stick);
            await _stickRepository.SaveChangesAsync();

            return ToResponse(stick);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var stick = await _stickRepository.GetByIdAsync(id);

            if (stick == null)
            {
                return false;
            }

            stick.StatusStick = StatusStick.Inactive;

            _stickRepository.Update(stick);
            await _stickRepository.SaveChangesAsync();

            return true;
        }

        private static StickResponse ToResponse(Stick stick)
        {
            return new StickResponse
            {
                Id = stick.Id,
                StickTypeId = stick.StickTypeId,
                StickCode = stick.StickCode,
                StatusStick = stick.StatusStick
            };
        }
    }
}
