using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eShift.Models
{
    public class Job
    {
        [Key]
        public int JobId { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer")]
        public int? CustId { get; set; } // Foreign key to Customer

        [Required(ErrorMessage = "Pickup Location is required.")]
        [StringLength(200, ErrorMessage = "Pickup Location cannot exceed 200 characters.")]
        [Display(Name = "Pickup Location")]
        public string PickupLocation { get; set; }

        [Required(ErrorMessage = "Delivery Location is required.")]
        [StringLength(200, ErrorMessage = "Delivery Location cannot exceed 200 characters.")]
        [Display(Name = "Delivery Location")]
        public string DeliveryLocation { get; set; }

        [Required(ErrorMessage = "Order Date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Scheduled Date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Scheduled Date")]
        public DateTime ScheduledDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Delivery Date")]
        public DateTime? DeliveryDate { get; set; } // Nullable, so not required by default

        [Required(ErrorMessage = "Job Status is required.")]
        [StringLength(50, ErrorMessage = "Job Status cannot exceed 50 characters.")]
        [Display(Name = "Job Status")]
        public string JobStatus { get; set; }

        // Navigation properties
        [ForeignKey("CustId")]
        public Customer? Customer { get; set; } // Link to the Customer entity
        public ICollection<Load> Loads { get; set; } = new List<Load>();

    }
}