using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace eShift.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(100, ErrorMessage = "Product Name cannot exceed 100 characters.")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

       
        [Required(ErrorMessage = "Product Type is required.")]
        [StringLength(50, ErrorMessage = "Product Type cannot exceed 50 characters.")]
        [Display(Name = "Product Type")]
        public string ProductType { get; set; }

        
    }

}
