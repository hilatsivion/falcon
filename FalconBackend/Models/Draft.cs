using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Draft : Mail
    {
        public DateTime TimeCreated { get; set; }
        public bool IsSent { get; set; }
    }
}
