namespace FalconBackend.Models
{
    public abstract class Mail
    {
        public int MailId { get; set; }
        public string Body { get; set; }
        public string Subject { get; set; }
        public bool IsFavorite { get; set; }

        // Relationships
        public ICollection<Recipient> Recipients { get; set; }
        public ICollection<Attachments> Attachments { get; set; }
    }
}
