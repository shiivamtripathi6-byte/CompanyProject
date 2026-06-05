using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace CompanyProject.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public string UserName { get; set; }
        public string BookName { get; set; }
        public string DateOfBooking { get; set; }
        public string DateOfReturning { get; set; }
        public bool Status { get; set; }
    }
}