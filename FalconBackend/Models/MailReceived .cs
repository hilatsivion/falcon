namespace FalconBackend.Models
{
    public class MailReceived : Mail
    {
        public int Id { get; set; } // Primary key

        public DateTime TimeReceived { get; set; }
        public bool IsRead { get; set; }
        public string Sender { get; set; }
    }
}
