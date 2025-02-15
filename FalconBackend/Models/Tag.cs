using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FalconBackend.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string TagName { get; set; }

        // Relationships
        public ICollection<FavoriteTag> FavoriteTags { get; set; }
    }
}
