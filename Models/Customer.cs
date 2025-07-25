using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace eShift.Models
{
    public class Customer
    {
        [Key] //primary key
        public int CustId { get; set; }

        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        public string CustName { get; set; }

        [Display(Name = "Customer Address")]
        public string CustAddress { get; set; }
        
        [Display(Name = "Customer Phone")]
        public string CustPhone { get; set; }

        [Display(Name = "Customer Email")]
        public string CustEmail { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime CustRegisterDate { get; set; }

      
    }
}

