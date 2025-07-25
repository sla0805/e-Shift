using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace eShift.Models
{
    public class Vehicle
    {
        
        public int VehicleId { get; set; }
        
        [Required(ErrorMessage = "Vehicle Model is required.")]
        [StringLength(100, ErrorMessage = "Vehicle Model cannot exceed 100 characters.")]
        [Display(Name = "Vehicle Model")]
        public string VehicleModel { get; set; }

        [Required(ErrorMessage = "Vehicle License Number is required.")]
        [StringLength(50, ErrorMessage = "Vehicle License Number cannot exceed 50 characters.")]
        [Display(Name = "Vehicle License Number")]
        public string VehicleLicensenum { get; set; }

        [Required(ErrorMessage = "Vehicle Type is required.")]
        [StringLength(50, ErrorMessage = "Vehicle Type cannot exceed 50 characters.")]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; }

        [Required(ErrorMessage = "Capacity in Kg is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Capacity must be a positive value.")] 
        [Display(Name = "Capacity (Kg)")]
        public float CapacityKg { get; set; }

        
    }
}
