using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace CompanyProject.Models
{
    public class BookMaster
    {
        public int BookId { get; set; }
        [Display(Name = "Enter your Book Name")]
        [Required(ErrorMessage = "Book Name is required")]
        public string BookName { get; set; }
        [Display(Name = "Enter your Quantity")]
        [Required(ErrorMessage = "Quantity is required")]
        public int Quantity { get; set; }
        public bool Status { get; set; }
        public DataTable Rows { get; set; }   
    }
}