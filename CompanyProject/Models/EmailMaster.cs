using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CompanyProject.Models
{
    public class EmailMaster
    {
        public int Id { get; set; }
        public string Type { get; set; }
         [AllowHtml]
        public string Message { get; set; }
    }
}