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

    // Current Time Tracking
    public float TimeSpentToday { get; set; } = 0;
    public float TimeSpentThisWeek { get; set; } = 0;

    // Previous Period Time Tracking 
    public float TimeSpentYesterday { get; set; } = 0;
    public float TimeSpentLastWeek { get; set; } = 0;

    // Averages & Totals
    public float AvgTimeSpentDaily { get; set; } = 0;
    public float AvgTimeSpentWeekly { get; set; } = 0;
    public float TotalTimeSpent { get; set; } = 0;
    public int TotalDaysTracked { get; set; } = 0;

    // Current Email Tracking
    public int EmailsReceivedWeekly { get; set; } = 0;
    public int EmailsSentWeekly { get; set; } = 0;
    public int SpamEmailsWeekly { get; set; } = 0; 
    public int ReadEmailsWeekly { get; set; } = 0;
    public int DeletedEmailsWeekly { get; set; } = 0;

    // Previous Period Email Tracking 
    public int EmailsReceivedLastWeek { get; set; } = 0;
    public int EmailsSentLastWeek { get; set; } = 0;
    public int SpamEmailsLastWeek { get; set; } = 0;
    public int ReadEmailsLastWeek { get; set; } = 0;
    public int DeletedEmailsLastWeek { get; set; } = 0;

    // Averages
    public float AvgEmailsPerDay { get; set; } = 0;
    public float AvgEmailsPerWeek { get; set; } = 0;

    // Streak Tracking
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;

    // Tracking Resets
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow; 
    public DateTime LastDailyReset { get; set; } = DateTime.UtcNow.Date; 
    public DateTime LastWeeklyReset { get; set; } = DateTime.UtcNow.Date; 
    public bool IsActiveToday { get; set; } = false;

    // Timestamp
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public AppUser AppUser { get; set; }
}
