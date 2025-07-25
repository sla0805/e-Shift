using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace eShift.Models
{
    public class TransportAssignment
    {
        [Key] // Primary Key
        public int TransportId { get; set; }

        [Required(ErrorMessage = "Job is required.")]
        [Display(Name = "Job")]
        public int JobId { get; set; }

        [Required(ErrorMessage = "Driver is required.")]
        [Display(Name = "Driver")]
        public int DriverId { get; set; }

        [Required(ErrorMessage = "Assistant is required.")]
        [Display(Name = "Assistant")]
        public int AssistantId { get; set; }

        [Required(ErrorMessage = "Vehicle is required.")]
        [Display(Name = "Vehicle")]
        public int VehicleId { get; set; }

        // Navigation properties
        [ForeignKey("JobId")]
        public Job? Job { get; set; }

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        [ForeignKey("AssistantId")]
        public Assistant? Assistant { get; set; }

        [ForeignKey("VehicleId")]
        public Vehicle? Vehicle { get; set; }
    }
}
