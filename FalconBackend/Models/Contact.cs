﻿namespace FalconBackend.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public DateTime? LastContactDate { get; set; }
        public bool IsFavorite { get; set; }

        // Relationships
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }

}