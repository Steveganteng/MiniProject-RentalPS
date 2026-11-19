using RentalPSMiniProject.Models;
using System.ComponentModel.DataAnnotations;

namespace RentalPS_BE.DTOs
{
    public class StickResponse
    {
        public int Id { get; set; }
        public int StickTypeId { get; set; }
        public string StickCode { get; set; } = string.Empty;
        public StatusStick StatusStick { get; set; }
    }
}
