using Microsoft.AspNetCore.Mvc;

namespace eShift.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}
