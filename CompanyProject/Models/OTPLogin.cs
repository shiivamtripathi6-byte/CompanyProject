using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CompanyProject.Models
{
    public class OTPLogin
    {
        public string Input { get; set; }
        public string OTP { get; set; }
        public int UserId { get; set; }
    }
}