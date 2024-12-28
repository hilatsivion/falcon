namespace FalconBackend.Models
{
    public class MailSent : Mail
    {
        public int Id { get; set; } // Primary key

        public DateTime TimeSent { get; set; }
    }
}
