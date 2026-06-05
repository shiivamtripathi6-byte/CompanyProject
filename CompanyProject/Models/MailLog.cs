using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CompanyProject.Models
{
    public class MailLog
    {
        public int MailId { get; set; }

        public string To { get; set; }

        public int UserId { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public DateTime DateTime { get; set; }
    }
}