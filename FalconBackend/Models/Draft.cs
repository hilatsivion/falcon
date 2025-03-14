using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Draft : Mail
    {
        public DateTime TimeCreated { get; set; }
        public bool IsSent { get; set; }
    }
}
