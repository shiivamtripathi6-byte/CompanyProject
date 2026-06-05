using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace CompanyProject.Models
{
    public class AddUser
    {
        public int UserId { get; set; }
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        [Display(Name="Mobile Number ")]
        [Required(ErrorMessage = "Mobile is required")]
        [MaxLength(10, ErrorMessage = "Mobile cannot exceed 10 digits")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit mobile number starting with 6-9.")]
        public string Mobile { get; set; }

        [Display(Name = "City")]
        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s]).{8,}", ErrorMessage = "The password must be at least 8 characters long, and must include one upper case character, one lower case character, one numerical character, and one symbol")]
        public string Password { get; set; }
        public bool Status { get; set; }
        public DateTime DateOFRegistration { get; set; }
        public string Role { get; set; }
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-z][a-z0-9._%+-]*@[a-z0-9.-]+\.[a-z]{2,}$", ErrorMessage = "Enter valid email. Email must start with a lowercase letter and contain @ and .")]
        public string Email { get; set; }
        public AddUser()
        {
            Role = "User";
            Status = true;
        }
    }
}