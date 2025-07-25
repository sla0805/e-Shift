using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace eShift.Models
{
    public class Load
    {
        public int LoadId { get; set; }

        [Required(ErrorMessage = "Job is required.")]
        [Display(Name = "Job")]
        public int JobId { get; set; }
        [ForeignKey("JobId")]

        [Required(ErrorMessage = "Product is required.")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]

        [Required(ErrorMessage = "Product quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int ProductQuantity { get; set; }

        [Required(ErrorMessage = "Container quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int ProductContainer { get; set; }

        [Required(ErrorMessage = "Load Weight is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Load Weight must be greater than 0.")]
        [Display(Name = "Load Weight (kg)")]
        public float LoadWeightKg { get; set; }

        [StringLength(500)]    
        public string? Comment { get; set; }


        public Job? Job { get; set; }
        public Product? Product { get; set; }
    }
}