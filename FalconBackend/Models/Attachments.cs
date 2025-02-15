using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Attachments
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileType { get; set; }
        public float FileSize { get; set; }
        public string FilePath { get; set; }

        // Relationships
        public int MailId { get; set; }
        public Mail Mail { get; set; }
    }
}
