using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace eShift.Models
{
    public class Driver
    {
        [Key] 
        public int DriverId { get; set; } 

        [Required(ErrorMessage = "Driver Name is required.")]
        [StringLength(100, ErrorMessage = "Driver Name cannot exceed 100 characters.")]
        [Display(Name = "Driver Name")]
        public string DriverName { get; set; } 

        [Required(ErrorMessage = "Driver License Number is required.")]
        [StringLength(50, ErrorMessage = "License Number cannot exceed 50 characters.")]
        [Display(Name = "Driver License Number")]
        public string DriverLicensenum { get; set; } 

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Driver Phone cannot exceed 20 characters.")]
        [Display(Name = "Driver Phone")]
        public string DriverPhone { get; set; }

        
    }
}