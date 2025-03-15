using FalconBackend.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Analytics
{
    [Key]
    [ForeignKey("AppUser")]
    [MaxLength(255)]
    [EmailAddress]
    public string AppUserEmail { get; set; }

    // Time Tracking
    public float TimeSpentToday { get; set; } = 0;
    public float AvgTimeSpentDaily { get; set; } = 0;
    public float TimeSpentThisWeek { get; set; } = 0;
    public float AvgTimeSpentWeekly { get; set; } = 0;
    public float TotalTimeSpent { get; set; } = 0; 
    public int TotalDaysTracked { get; set; } = 0; 

    // Email Tracking
    public int EmailsReceivedWeekly { get; set; } = 0;
    public int EmailsSentWeekly { get; set; } = 0;
    public int SpamEmailsWeekly { get; set; } = 0;
    public int ReadEmailsWeekly { get; set; } = 0;
    public float AvgEmailsPerDay { get; set; } = 0;
    public float AvgEmailsPerWeek { get; set; } = 0;

    // Streak Tracking
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;

    // Tracking Resets
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow;
    public bool IsActiveToday { get; set; } = false;

    // Timestamp
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public AppUser AppUser { get; set; }
}
