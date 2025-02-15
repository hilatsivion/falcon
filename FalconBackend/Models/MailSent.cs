using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class MailSent : Mail
    {
        public int Id { get; set; } // Primary key

        public DateTime TimeSent { get; set; }
    }
}
