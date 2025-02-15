using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class FavoriteTag
    {
        public int Id { get; set; }

        // Relationships
        public int TagId { get; set; }
        public Tag Tag { get; set; }

        public int MailAccountId { get; set; }
        public MailAccount MailAccount { get; set; }
    }
}
