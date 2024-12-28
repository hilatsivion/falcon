namespace FalconBackend.Models
{
    public class Analytics
    {
        public int Id { get; set; }
        public float TimeSpentToday { get; set; }
        public float AvgTimeSpentDaily { get; set; }
        public float TimeSpentThisWeek { get; set; }
        public float AvgTimeSpentWeekly { get; set; }
        public int EmailsReceivedWeekly { get; set; }
        public int SpamEmailsWeekly { get; set; }

        // Relationships
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
    }
}
