using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace eShift.Models
{
    public class DeliveryViewModel
    {
        // Job-related fields
        public int CustId { get; set; }
        public string PickupLocation { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ScheduledDate { get; set; }

        // Load-related fields
        public int ProductId { get; set; } 
        public int ProductQuantity { get; set; }
        public int ProductContainer { get; set; }
        public float LoadWeightKg { get; set; }
        public string Comment { get; set; }

        // Dropdown list for products
        public List<SelectListItem> ProductList { get; set; }
    }
}