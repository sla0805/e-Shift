using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; 

namespace eShift.Models
{
    public class Assistant
    {
        [Key] 
        
        public int AssistantId { get; set; } 

        [Required(ErrorMessage = "Assistant Name is required.")]
        [StringLength(100, ErrorMessage = "Assistant Name cannot exceed 100 characters.")]
        [Display(Name = "Assistant Name")] 
        public string AssistantName { get; set; }

        [Required(ErrorMessage = "Assistant Phone is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Assistant Phone cannot exceed 20 characters.")]
        [Display(Name = "Assistant Phone")]
        public string AssistantPhone { get; set; }
       
    }
}
