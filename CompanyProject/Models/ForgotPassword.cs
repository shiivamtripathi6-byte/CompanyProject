using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace CompanyProject.Models
{
    public class ForgotPassword
    {
        public int ForgetId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; }
        public string Email { get; set; }
        public string TokenId { get; set; }

        public bool Status { get; set; }

        public DateTime Date { get; set; }
    }
}