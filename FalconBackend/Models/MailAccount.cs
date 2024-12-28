namespace FalconBackend.Models
{
    public class MailAccount
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public string EmailAddress { get; set; }
        public DateTime LastMailSync { get; set; }
        public bool IsDefault { get; set; }
        public MailProvider Provider { get; set; }

        public enum MailProvider
        {
            Gmail,
            Outlook
        }

        // Relationships
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public ICollection<FavoriteTag> FavoriteTags { get; set; }
    }

}
