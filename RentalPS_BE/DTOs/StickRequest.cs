using RentalPSMiniProject.Models;
using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class StickRequest
    {
        [Required(ErrorMessage="StickTypeId wajib diisi!")]
        [Range(1, int.MaxValue)]
        public int StickTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string StickCode { get; set; } = string.Empty;

        [EnumDataType(typeof(StatusStick))]
        public StatusStick StatusStick { get; set; } = StatusStick.Available;
    }
}
