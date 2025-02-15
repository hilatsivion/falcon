using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Reply
    {
        public int Id { get; set; }
        public int RepliedToMailId { get; set; }
        public int ReplyChainPosition { get; set; }

        // Relationships
        public Mail Mail { get; set; }
    }

}
